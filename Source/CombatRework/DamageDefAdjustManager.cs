using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;
using static UnityEngine.Scripting.GarbageCollector;

namespace CombatRework
{
    public static class DamageDefAdjustManager
    {
        private static int SillyLittleCount = 0;

        public static int onLoad()
        {
            EvilHasBeenCommited();
            return 0;
        }
        public static float retrieveBaseDamage(ref Verse.DamageInfo damageInfo)
        {
            if (damageInfo.Weapon != null && damageInfo.Weapon.Verbs.Count == SillyLittleCount + 1)//make sure its a weapon and then make sure its been adjusted by my defs
            {
                //means weapon is gun
                if (damageInfo.Weapon.Verbs[0].verbClass == typeof(Verb_Shoot))
                {
                    //means its the bullet the gun shot
                    if(damageInfo.Def == DamageDefOf.Bullet) return damageInfo.Weapon.Verbs[0].defaultProjectile.projectile.GetDamageAmount(1);
                    else
                    {
                        //at least with base weapons the damage of the different parts of the weapon are all the same... a weapon has a damage of 9pr sec, not a stock damage of 9pr sec, a barrel damage of 30pr sec, if this is a problem with some weapons the logic would have to change a bit
                        return damageInfo.Weapon.tools[0].power;
                    }
                }
                //means verb is melee weapon
                if (damageInfo.Weapon.Verbs[0].verbClass == typeof(Verb_MechCluster))//this is the stupid verbclass that we are using in our fake verbs, melee weapons have no verbs so if we are accessing our defs than it must have a verb and we are using this useless verb as our verbproperties verbclass
                {
                    DamageDef myDef = damageInfo.Def;
                    //have to search through this to find the right base damage, because the melee weapons actually do have different damages for their parts... the handle has 2 damage while the blade has 10 damage
                    return damageInfo.Weapon.tools.Find(t =>
                    {
                        return t.capacities.Exists(b =>
                        {//two searches because thankfully weapons can have two capacities per tool, wonderful
                            return b.defName == myDef.defName;
                        });
                    }).power;
                }
                //means weapon is grenade
                if (damageInfo.Weapon.Verbs[0].verbClass == typeof(Verb_LaunchProjectile))
                {
                    if (damageInfo.Def == DamageDefOf.Bomb) return damageInfo.Weapon.Verbs[0].defaultProjectile.projectile.GetDamageAmount(1);
                }
                
            }
                return 0;
        }
        public static float retrieveArmorDamage(ref Verse.DamageInfo damageInfo, float amount, float baseDamage)
        {
            if (amount != 0 && baseDamage != 0) return (float)damageInfo.Weapon.Verbs[SillyLittleCount].burstShotCount * (amount / baseDamage);
            return 0;
        }
        public static float retrieveShieldDamage(ref Verse.DamageInfo damageInfo)//energy if amount not found - base amount
        {
            if (damageInfo.Weapon != null && damageInfo.Weapon.Verbs[SillyLittleCount] != null) return ((float)damageInfo.Weapon.Verbs[SillyLittleCount].sprayWidth * (damageInfo.Amount / damageInfo.Weapon.Verbs[SillyLittleCount].defaultProjectile.projectile.GetDamageAmount(1))) / 100;
            return 0f;
        }
        public static float retrieveArmorPercent(ref RimWorld.Apparel armorPiece)//taking a percentage, just testing for now will floor and ceiling it in someway
        {
            //tested it and it seemed fast enough... 20,000 iterations takes 4 microseconds, will be far less in iterations when combat occurs obviously
            float b = (float)System.Math.Sqrt(System.Math.Max(System.Math.Min((float)(armorPiece.HitPoints - (armorPiece.MaxHitPoints / 5)) / (float)(armorPiece.MaxHitPoints * .6f), 1f), 0));
            return b > 1.0f ? 1.0f : b;
        }
        public static bool adjustedApplyArmor(ref float damageAmount, ref Verse.DamageDef def, float armorPenetration, Verse.Thing armorPiece, Pawn targetPawn, float armorRating, float armorPercent = 1.0f, float baseDamage = 0, float armorDamage = 0)
        {
            bool metalArmor = false;
            if (armorPiece != null)
            {
                
                metalArmor = armorPiece.def.apparel.useDeflectMetalEffect || (armorPiece.Stuff != null && armorPiece.Stuff.IsMetal);
                //below needs to be my logic that changes how the armor damage works
                if (baseDamage != 0)
                {
                    float f = armorDamage * (damageAmount / baseDamage) * armorPercent;
                    //above will give us the armordamage, which is saved to verb 2s burstshotcount int
                    //multiplied by the current percentage of the damageAmount in comparision to the base damage amount...
                    //the current percentage makes it where if the weapon has modifiers on it those modifiers will be reflected in the armordamage
                    //or if the weapons damage has already been diminished by osme armor that diminishment will also be reflected by the percentage
                    armorPiece.TakeDamage(new DamageInfo(def, f));
                }
                else//revert to old logic weapon doesn't have an extra verb
                {
                    float f = damageAmount * 0.25f;
                    armorPiece.TakeDamage(new DamageInfo(def, f));//okay annoyingly I just noticed that damageinfo is not passed to getpostarmordamage...
                    //unfortunately Ill just be lazy and pass armordamage to that in a new very slightly adjusted class...
                    //whatever
                }
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
            float num = Mathf.Max((armorRating * armorPercent) - armorPenetration,0f);
            if(num != 0)//bullet damage is only efffected if the penetration is below the armorrating
            {//this is like the base rimworld logic but I've just put this extra check because why not?
                //randValue is always positive
                float value = Rand.Value;
                float num2 = num * 0.5f;
                float num3 = num;//this might seem weird to do... but it seems to work well in the compiled Il, it doesnt use more local variables than it would otherwise
                //but im no computer scientist
                if (value < num2)
                {
                    damageAmount = 0f;
                }
                else
                {
                    //okay what Ive changed here in comparison to the normal rimworld logic is
                    //the damage is always divided by 2f but if the random value is less than num
                    //the dammage is divided by 4f I don't know it just seems reasonable... it encourages more penetration in comparison to gear
                    //instead of just more bullet
                    if (def.armorCategory == DamageArmorCategoryDefOf.Sharp)
                    {
                        def = DamageDefOf.Blunt;
                    }
                    if (value < num3)
                    {
                        damageAmount = GenMath.RoundRandom(damageAmount / 4f);
                    }
                    else
                    {
                        damageAmount = GenMath.RoundRandom(damageAmount / 2f);
                    }
                }

            }
            return metalArmor;
        }
        public static float adjustedGetPostArmorDamage(Pawn pawn, float amount, ref DamageInfo damageInfo, out bool deflectedByMetalArmor, out bool diminishedByMetalArmor)
        {
            deflectedByMetalArmor = false;
            diminishedByMetalArmor = false;
            if (damageInfo.Def.armorCategory == null)
            {
                return amount;
            }
            DamageDef holder = damageInfo.Def;//im doing this in here, because it makes more sense than the logic he has set up... like why not do it this way... sure your passing more... but its as a reference... and you don't need to create the object if theres no armor
            StatDef armorRatingStat = holder.armorCategory.armorRatingStat;
            //might as well pull these things up here if we have to adjust this function anyway
            float armorPen = damageInfo.ArmorPenetrationInt;
            float armorDamage = 0;
            float baseDamage = 0;
            if (damageInfo.Weapon != null)//check if weapon has been changed by our patches, if not use old logic
            {
                baseDamage = retrieveBaseDamage(ref damageInfo);
                Verse.Log.Warning("This is the baseDamage: " + baseDamage);
                armorDamage = retrieveArmorDamage(ref damageInfo, amount, baseDamage);
            }
            else
            {
                Verse.Log.Warning("DamageInfo" + damageInfo);
            }
            if (pawn.apparel != null)
            {
                List<Apparel> wornApparel = pawn.apparel.WornApparel;
                for (int num = wornApparel.Count - 1; num >= 0; num--)
                {
                    Apparel apparel = wornApparel[num];
                    if (apparel.def.apparel.CoversBodyPart(damageInfo.HitPart))
                    {
                        float num2 = amount;
                        bool metalArmor;
                        float armorPercent = retrieveArmorPercent(ref apparel);
                        if (armorDamage == 0) metalArmor = adjustedApplyArmor(ref amount, ref holder, armorPen, apparel, pawn, apparel.GetStatValue(armorRatingStat), armorPercent);//if weapon has patch pass else dont
                        else metalArmor = adjustedApplyArmor(ref amount, ref holder, armorPen, apparel, pawn, apparel.GetStatValue(armorRatingStat), armorPercent, baseDamage, armorDamage);
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
            bool metalArmor2 = adjustedApplyArmor(ref amount, ref holder, armorPen, null, pawn, pawn.GetStatValue(armorRatingStat));//skin doesnt take armordamage obviously
            if (amount < 0.001f)
            {
                deflectedByMetalArmor = metalArmor2;
                return 0f;
            }
            if (amount < num3 && metalArmor2)
            {
                diminishedByMetalArmor = true;
            }
            damageInfo.Def = holder;
            return amount;
        }
        public static void instatiateVerb(ref VerbProperties verb)
        {
            verb.verbClass = typeof(Verb_MechCluster);
            //it must have this otherwise a constructor that creates it for something fails
            //the constructor is called every time a pawn equips osmething and it creates the base class of of the Verbtype,
            //it does this for the items verbtracker which handles all the verbs affecting it...
        }
        public static void EvilHasBeenCommited()
        {
            //okay scratch all of what was previously here...
            //heres my notes on what this evil function is doing
            //this function changes the the weapons stats here and adds the extra verb property inside this function as defined by our new defs
            //this allows the creation of one def instead of 2 patch replaces and a patchadd to change the weapons, allowing easier implementation of
            //modded weapon changes
            //I really did this because I find it funny that I can, but also because it makes the logic better
            //first off it makes it where I don't have to explain how to patch in rimworld, just how to create a def
            //second and more importantly, it prevents the need of a search function if there are more than one verbproperites on the weapons
            //which I think will probably be a problem when multiple mods are taken into account, because I imagine they might do the same thing as this
            //to adjust weapons without creating horrible library searches

            //it does this through the use of c# reflections primarily
            List<ThingDef> myGuns = DefDatabase<ThingDef>.AllDefs.ToList();
            List<Lucids_Damage> adjustments = DefDatabase<Lucids_Damage>.AllDefs.ToList();
            myGuns.RemoveAll(thing =>
            {
                return thing.weaponTags == null || thing.equipmentType != EquipmentType.Primary;
            });
            ThingDef revolver = myGuns.Find(t =>
            {
                return t.defName == "Gun_Revolver";
            });

            if(revolver != null)
            {
                Type pProperties = revolver.Verbs[0].defaultProjectile.projectile.GetType();
                FieldInfo revolverDamage = pProperties.GetField("damageAmountBase", BindingFlags.NonPublic | BindingFlags.Instance);
                Verse.Log.Warning("Initial Value: " + revolverDamage.GetValue(revolver.Verbs[0].defaultProjectile.projectile));
                revolverDamage.SetValue(revolver.Verbs[0].defaultProjectile.projectile, 3);
                Verse.Log.Warning("New Value: " + revolverDamage.GetValue(revolver.Verbs[0].defaultProjectile.projectile));
            }
            else
            {
                Verse.Log.Warning("Couldnt find revolver");
            }
            foreach (ThingDef t in myGuns)
            {
                if (t.Verbs.Count > SillyLittleCount) SillyLittleCount = t.Verbs.Count;
            }
            ConstructorInfo myConst = typeof(VerbProperties).GetConstructor(Type.EmptyTypes);

            VerbProperties verb = (VerbProperties)myConst.Invoke(null);
            instatiateVerb(ref verb);//doing this before because it is a large assignment...

            foreach (Lucids_Damage t in adjustments)
            {
                ThingDef foundWeapon = myGuns.Find(b =>
                {
                    return b.defName == t.defName;
                });
                if (foundWeapon != null)
                {
                    Type pProperties = foundWeapon.Verbs[0].defaultProjectile.projectile.GetType();
                    if (t.baseDamage != -1)
                    {
                        FieldInfo baseDamage = pProperties.GetField("damageAmountBase", BindingFlags.NonPublic | BindingFlags.Instance);
                        baseDamage.SetValue(foundWeapon.Verbs[0].defaultProjectile.projectile, t.baseDamage);
                    }
                    if (t.baseDamage != -1)
                    {
                        FieldInfo armorPen = pProperties.GetField("armorPenetrationBase", BindingFlags.NonPublic | BindingFlags.Instance);
                        float pen = (float)armorPen.GetValue(foundWeapon.Verbs[0].defaultProjectile.projectile);
                        Verse.Log.Warning("I imagine this won't be logged because ArmorPen doesn't exist but anyways here is the value if it exist: " + pen);
                        armorPen.SetValue(foundWeapon.Verbs[0].defaultProjectile.projectile, t.armorPen);
                    }
                    verb.burstShotCount = t.armorDamage == -1 ? 0 : t.armorDamage;
                    verb.sprayWidth = t.shieldDamage == -1 ? 0 : t.shieldDamage;

                    Type tDef = foundWeapon.GetType();
                    FieldInfo verbs = tDef.GetField("verbs", BindingFlags.NonPublic | BindingFlags.Instance);
                    List<VerbProperties> adjustVerbs = (List<VerbProperties>)verbs.GetValue(foundWeapon);

                    Verse.Log.Warning("Revolvers VerbProperty defaultProjectile: " + adjustVerbs[0].defaultProjectile.defName);

                    for (int i = adjustVerbs.Count; i < SillyLittleCount; i++)//this adds null verbs to every weapon untill the all weapons have the same amount of verbs so we can access shielddamage and armor damage without conducting a search
                    {
                        adjustVerbs.Add(verb);
                        Verse.Log.Warning("Its DOING IT");
                    }
                    adjustVerbs.Add(verb);
                    Verse.Log.Warning("Is it firing in here?");

                }
                else
                {
                    Verse.Log.Error("The CombatRework Lucids_Damage: " + t.defName + " does not have a matching weapon. \nThis Error could be caused by having a submod active for a mod you no longer use.\n If you are making a submod your defName matches no weapon. Consider documentation.");
                }
            }

            
            

            
        }
    }
}
