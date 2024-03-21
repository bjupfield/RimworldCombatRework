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
            List<ThingDef> myGuns = DefDatabase<ThingDef>.AllDefs.ToList();
            Verse.Log.Warning("WE ARE IN THE STATIC MANAGER CLASS");
            myGuns.RemoveAll(thing =>
            {
                return thing.weaponTags == null;
            });
            myGuns.RemoveAll(thing =>
            {
                return thing.weaponTags.Count == 0;
            });
            //foreach (Shield_Armor_Damage damage in myDamages)
            //{
            //    ThingDef myGun = myGuns.Find(gun =>
            //    {
            //        return gun.defName == damage.defName;
            //    });
            //    if (myGun != null)
            //    {
            //        allDamages.Add(myGun.defName, damage);
            //        Verse.Log.Warning("Is it mE!?!?!?" + DefDatabase<ThingDef>.GetNamed(myGun.Verbs[0].defaultProjectile.defName).projectile.GetDamageAmount(1).ToString());
            //        //myGun.Verbs.Find(x => { return typeof(x) == typeof(ThingDef)});
            //        Verse.Log.Warning("The Damage is currently: " + DefDatabase<ThingDef>.GetNamed(myGun.Verbs[0].defaultProjectile.defName).projectile.GetDamageAmount(1).ToString());
            //        StringBuilder mine = new StringBuilder("yes");
            //        ThingDef b = DefDatabase<ThingDef>.GetNamed(myGun.Verbs[0].defaultProjectile.defName);
            //        //DefDatabase<ThingDef>.GetNamed(myGun.Verbs[0].defaultProjectile.defName).projectile.GetDamageAmount(5f, mine);
            //        b.projectile.GetDamageAmount(5f, mine);
            //        Verse.Log.Warning("The Damage is currently: " + b.projectile.GetDamageAmount(1).ToString());
            //        //Verse.Log.Warning("The Damage is currently: " + DefDatabase<ThingDef>.GetNamed(myGun.Verbs[0].defaultProjectile.defName).projectile.GetDamageAmount(1).ToString());
            //        allDamages[myGun.defName].baseDamage  = DefDatabase<ThingDef>.GetNamed(myGun.Verbs[0].defaultProjectile.defName).projectile.GetDamageAmount(1);
            //        Verse.Log.Warning("Yes!?!?!?");
            //    }
            //}
            String bulletString =  myGuns.Find(t =>
            {
                return t.defName == "Gun_Revolver";
            }).Verbs[0].defaultProjectile.defName;

            myGuns.Find(t =>
            {
                return t.defName == "Gun_Revolver";
            }).Verbs[0].defaultProjectile.defName = "Bullet_EMPLauncher";

            Verse.Log.Warning("HEY THIS IS: " + myGuns.Find(t =>
            {
                return t.defName == "Gun_Revolver";
            }).Verbs[0].defaultProjectile.defName);

            myGuns.Find(t =>
            {
                return t.defName == "Gun_Revolver";
            }).defName = "Emp_Emp";

            List <ThingDef> bullets = DefDatabase<ThingDef>.AllDefsListForReading.ListFullCopy();


            bullets.RemoveAll(t =>
            {
                if (t.projectile != null && t.projectile.damageDef != null) Verse.Log.Warning("DamageAmountBase: "+t.projectile.GetDamageAmount(1));
                return (t.projectile != null && t.projectile.damageDef != null);
            });

            bullets.Find(t =>
            {
                return t.defName == bulletString;
            });
            Predicate<ThingDef> isWeapon = (ThingDef td) => td.equipmentType == EquipmentType.Primary && !td.weaponTags.NullOrEmpty();
            List<ThingStuffPair> weapons = ThingStuffPair.AllWith(isWeapon);
            foreach(ThingStuffPair t in weapons)
            {
                if(t.stuff != null && t.thing != null)
                Verse.Log.Warning("Thing: " + t.thing.defName + " || Stuff: " + t.stuff.defName);
            }
            ThingDef mine = new ThingDef { defName = "hey" };
            //ProjectileProperties min3 = new ProjectileProperties { dama };
            ThingStuffPair mine2 = new ThingStuffPair { thing = mine };

            //Verse.Log.Warning("Gun Revolvers damage: " + bullets.Find(t =>
            //{
            //    return t.defName == bulletString;
            //}).defName);

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
            Verse.Log.Warning("Info Amount: " + info.Amount);//reveals all projectiles at least hold this information... but armor damage is just pure damage as it is a thing...

            Verse.Log.Warning("Thing Being Damaged: " + objectDamaged.def.defName);
        }
        public static void adjustedApplyArmor(ref float damageAmount, ref Verse.DamageInfo damageInfo, Verse.Thing armorPiece, Pawn targetPawn, RimWorld.StatDef armorStatDef, out bool metalArmor)
        {
            float armorRating = armorPiece.GetStatValue(armorStatDef);
            if (armorPiece != null)
            {
                metalArmor = armorPiece.def.apparel.useDeflectMetalEffect || (armorPiece.Stuff != null && armorPiece.Stuff.IsMetal);
                //below needs to be my logic that changes how the armor damage works
                //struct aDamPen{float damPercent, int armorDam};
                //where damPercent is just the current damageAmount / baseWeapon damage... which will give us both the amount
                //of damage at the start based on the weapon multiplier and will subsequently give the amount of damage that has gotten through
                //pieces of armor
                //like struct aDamPen = pullArmorDamage(damageInfo.weapon /*I think this will always not be null inside a target with armor */, damageAmount);
                //so yeah this just returns a struct of this type which we than do this with
                //float f = aDamPen.armorDam * damPercent;
                //armorthing.TakeDamage(new Damageinfo(damage.Def, f));
                //also the original uses a random value for the damage amount to armor, might want to use this
            }
            else//if armorthing is null this means its just hitting the pawn... which means we dont need to do the armor damage stuff
            {
                metalArmor = targetPawn.RaceProps.IsMechanoid;
            }
            //the following logic just does a couple of random chance things
            //generates a random number and than checks if the value is below
            //half of the armorrating - armorpenetration, which means the weapon does no damage
            //if its above half of this than it halfs the damage amount
            //the genMath.RoundRandom stuff just randomly chooses either flooring or ceiling
            //the number
            //also changes damage to blunt if sharp if it is below the penetration value
            float num = Mathf.Max(armorRating - damageInfo.ArmorPenetrationInt,0f);
            float value = Rand.Value;
            float num2 = num * 0.5f;
            float num3 = num;
            if (value < num2)
            {
                damageAmount = 0f;
            }
            else if (value < num3)
            {
                damageAmount = GenMath.RoundRandom(damageAmount / 2f);
                if (damageInfo.Def.armorCategory == DamageArmorCategoryDefOf.Sharp)
                {
                    damageInfo.Def = DamageDefOf.Blunt;
                }
            }
            //okay weirdly damageInfo.Amount cannot be adjusted... so we have to reference the damage amount
        }
    }


}
