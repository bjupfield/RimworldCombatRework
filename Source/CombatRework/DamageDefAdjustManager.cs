using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatRework
{
    public static class DamageDefAdjustManager
    {
        private static Dictionary<String, Shield_Armor_Damage> allDamages = new Dictionary<string, Shield_Armor_Damage>();


        public static int onLoad()
        {
            allDamages = new Dictionary<String, Shield_Armor_Damage>();
            List<Shield_Armor_Damage> myDamages = DefDatabase<Shield_Armor_Damage>.AllDefsListForReading.ListFullCopy();
            List<ThingDef> myGuns = DefDatabase<ThingDef>.AllDefsListForReading.ListFullCopy();
            Verse.Log.Warning("WE ARE IN THE STATIC MANAGER CLASS");
            myGuns.RemoveAll(thing =>
            {
                return thing.weaponTags == null;
            });
            myGuns.RemoveAll(thing =>
            {
                return thing.weaponTags.Count == 0;
            });
            foreach (Shield_Armor_Damage damage in myDamages)
            {
                ThingDef myGun = myGuns.Find(gun =>
                {
                    return gun.defName == damage.defName;
                });
                if (myGun != null)
                {
                    allDamages.Add(myGun.defName, damage);
                    Verse.Log.Warning("Is it mE!?!?!?" + DefDatabase<ThingDef>.GetNamed(myGun.Verbs[0].defaultProjectile.defName).projectile.GetDamageAmount(1).ToString());
                    //myGun.Verbs.Find(x => { return typeof(x) == typeof(ThingDef)});
                    Verse.Log.Warning("The Damage is currently: " + DefDatabase<ThingDef>.GetNamed(myGun.Verbs[0].defaultProjectile.defName).projectile.GetDamageAmount(1).ToString());
                    StringBuilder mine = new StringBuilder("yes");
                    ThingDef b = DefDatabase<ThingDef>.GetNamed(myGun.Verbs[0].defaultProjectile.defName);
                    //DefDatabase<ThingDef>.GetNamed(myGun.Verbs[0].defaultProjectile.defName).projectile.GetDamageAmount(5f, mine);
                    b.projectile.GetDamageAmount(5f, mine);
                    Verse.Log.Warning("The Damage is currently: " + b.projectile.GetDamageAmount(1).ToString());
                    //Verse.Log.Warning("The Damage is currently: " + DefDatabase<ThingDef>.GetNamed(myGun.Verbs[0].defaultProjectile.defName).projectile.GetDamageAmount(1).ToString());
                    allDamages[myGun.defName].baseDamage  = DefDatabase<ThingDef>.GetNamed(myGun.Verbs[0].defaultProjectile.defName).projectile.GetDamageAmount(1);
                    Verse.Log.Warning("Yes!?!?!?");
                }
            }

            Verse.Log.Warning("Final Count" + allDamages.Count.ToString());

            return 0;
        }
        public static float pullArmorDamage(string DefName, float damage)
        {
            if(allDamages.ContainsKey(DefName))
            {
                Shield_Armor_Damage b = allDamages[DefName];
                Verse.Log.Warning("THIS WEPAONS DAMAGE IS: " + b.armorDamage + " || BaseDamage: " + b.baseDamage + " || damage: " + damage);
                return b.armorDamage * (damage / b.baseDamage);
            }
            return 0;
        }
        public static float pullShieldDamage(string DefName, float damage)
        {
            if (allDamages.ContainsKey(DefName))
            {
                Shield_Armor_Damage b = allDamages[DefName];
                return b.shieldDamage * (damage / b.baseDamage);
            }
            return 0;
        }

        public static float GetPostArmorDamage(Pawn pawn, float amount, float armorPenetration, BodyPartRecord part, ref DamageDef damageDef, out bool deflectedByMetalArmor, out bool diminishedByMetalArmor, string projectileName)//alright just copying this because i need more info in the applyarmor function and this is the best way
        {
            float armorDamage = pullArmorDamage(projectileName, amount);
            deflectedByMetalArmor = false;
            diminishedByMetalArmor = false;
            if (damageDef.armorCategory == null)
            {
                return amount;
            }
            StatDef armorRatingStat = damageDef.armorCategory.armorRatingStat;
            if (pawn.apparel != null)
            {
                List<Apparel> wornApparel = pawn.apparel.WornApparel;
                for (int num = wornApparel.Count - 1; num >= 0; num--)
                {
                    Apparel apparel = wornApparel[num];
                    if (apparel.def.apparel.CoversBodyPart(part))
                    {
                        float num2 = amount;
                        ApplyArmor(ref amount, armorPenetration, apparel.GetStatValue(armorRatingStat), apparel, ref damageDef, pawn, out var metalArmor, armorDamage);
                        if (amount < 0.001f)
                        {
                            deflectedByMetalArmor = metalArmor;
                            return 0f;
                        }
                        if (amount < num2 && metalArmor)
                        {
                            diminishedByMetalArmor = true;
                        }
                    }
                }
            }
            float num3 = amount;
            ApplyArmor(ref amount, armorPenetration, pawn.GetStatValue(armorRatingStat), null, ref damageDef, pawn, out var metalArmor2, armorDamage);
            if (amount < 0.001f)
            {
                deflectedByMetalArmor = metalArmor2;
                return 0f;
            }
            if (amount < num3 && metalArmor2)
            {
                diminishedByMetalArmor = true;
            }
            return amount;
        }
        private static void ApplyArmor(ref float damAmount, float armorPenetration, float armorRating, Thing armorThing, ref DamageDef damageDef, Pawn pawn, out bool metalArmor, float armorDamage)
        {
            float b = 1f;
            if (armorThing != null)
            {
                metalArmor = armorThing.def.apparel.useDeflectMetalEffect || (armorThing.Stuff != null && armorThing.Stuff.IsMetal);
            }
            else
            {
                metalArmor = pawn.RaceProps.IsMechanoid;
            }
            if (armorThing != null)
            {
                b = Mathf.Max((float)armorThing.HitPoints / (float)armorThing.MaxHitPoints, .1f);
                float myArmorDamage = b * Mathf.Min(armorRating, .9f) * armorDamage;
                Verse.Log.Warning("This is the B Value: " + b + " || This is the ArmorRating Value: " + armorRating + " || This is the ArmorthingHitpoints: " + armorThing.HitPoints + " || This is the ArmorThingMax: " + armorThing.MaxHitPoints);
                Verse.Log.Warning("Calculated Armor Damage: " + myArmorDamage);
                armorThing.TakeDamage(new DamageInfo(damageDef, GenMath.RoundRandom(myArmorDamage)));
            }
            float num = Mathf.Max(armorRating - armorPenetration, 0f);
            float value = Rand.Value;
            float num2 = num * 0.5f;
            float num3 = num;
            if (value < num2)
            {
                damAmount = 0f;
            }
            else if (value < num3)
            {
                damAmount = GenMath.RoundRandom(damAmount / 2f);
                if (damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp)
                {
                    damageDef = DamageDefOf.Blunt;
                }
            }
        }
        public static void PrintMyShit(Verse.Hediff_Injury myInjury)
        {
            //if(myInjury.source == null)
            //{
            //    Verse.Log.Warning("Its null thats kinda weird");
            //}
            //else
            //{
            //    Verse.Log.Warning("This is the Weapon Maybe: " + myInjury.source.defName);
            //}
            //if (myInjury.sourceHediffDef == null)
            //{
            //    Verse.Log.Warning("The sourceHediffDef is null");
            //}
            //else
            //{
            //    Verse.Log.Warning("The SourceHeddiffName is: " + myInjury.sourceHediffDef.defName);
            //}
            //if (myInjury.def == null)
            //{
            //    Verse.Log.Warning("The injury def is null");
            //}
            //else
            //{
            //    Verse.Log.Warning("The injury def is: " + myInjury.def.defName);
            //}
            //if(myInjury.Severity == null)
            //{
            //    Verse.Log.Warning("Severity is null");
            //}
            //else
            //{
            //    Verse.Log.Warning("Severity is: " + myInjury.Severity);
            //}
            //myInjury.Severity = 2;
        }
        public static void Printer(Verse.ThingDef source)
        {
            //if (source == null)
            //{
            //    Verse.Log.Warning("This thing is null");
            //}
            //else Verse.Log.Warning(source.defName);
        }
        public static void MyPrinter(ref Verse.DamageDef damageDef)
        {
            //Verse.Log.Warning("The ApplyArmor IS null");
            //if (damageDef.defName == null)
            //{
            //    Verse.Log.Warning("The ApplyArmor IS null");
            //}
            //else Verse.Log.Warning("damagedef thing is: " + damageDef);
            //if (damageDef.defaultDamage == null)
            //{
            //    Verse.Log.Warning("Default Damage Null");
            //}
            //else
            //{
            //    Verse.Log.Warning("Default Damage: " + (int)damageDef.defaultDamage);
            //}
            //Verse.Log.Warning("Default DamagePen: " + damageDef.defaultArmorPenetration);
            Verse.Log.Warning("DamageDef is: " + damageDef.defName);
        }
        public static void MyPrinted(Verse.Pawn pawn)
        {
            if (pawn == null)
            {
                Verse.Log.Warning("Pawn is Null");
            }
            else Verse.Log.Warning("Pawn is: " + pawn.Name);
        }
        public static void Damaging(ref Verse.DamageInfo info, Thing objectDamaged)
        {
            if (info.Weapon == null)
            {
                Verse.Log.Warning("Info is Null");
            }
            else Verse.Log.Warning("Pawn is: " + info.Weapon);
            Verse.Log.Warning("This is something?: " + info.Weapon);
            Verse.Log.Warning("Info Amount: " + info.Amount);

            Verse.Log.Warning("Thing Being Damaged: " + objectDamaged.def.defName);
        }
    }


}
