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
        private static int SillyLittleCount = 0;

        public static int onLoad()
        {
            EvilHasBeenCommited();
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

            //myGuns.Find(t =>
            //{
            //    return t.defName == "Gun_Revolver";
            //}).Verbs[0].defaultProjectile.defName = "Bullet_EMPLauncher";
            //the above stuff shows that I cna just adjust the values of the stored 

            Verse.Log.Warning("HEY THIS IS: " + myGuns.Find(t =>
            {
                return t.defName == "Gun_Revolver";
            }).Verbs[0].defaultProjectile.defName);


            uint verb2count = 0;
            foreach(ThingDef t in myGuns) 
            {
                if (t.Verbs.Count > 1) verb2count++;
            }

            Verse.Log.Warning("Weapons with Mulitple Verbs: " + verb2count);

            return 0;
        }
        public static float retrieveBaseDamage(ref Verse.DamageInfo damageInfo)
        {
            if (damageInfo.Weapon != null && damageInfo.Weapon.Verbs[SillyLittleCount] != null) return damageInfo.Weapon.Verbs[SillyLittleCount].defaultProjectile.projectile.GetDamageAmount(1);
            return 0;
        }
        public static float retrieveArmorDamage(ref Verse.DamageInfo damageInfo, float amount, float baseDamage)
        {
            if (amount != 0) return (float)damageInfo.Weapon.Verbs[SillyLittleCount].burstShotCount * (amount / baseDamage);
            return 0;
        }
        public static float retrieveShieldDamage(ref Verse.DamageInfo damageInfo)//energy if amount not found - base amount
        {
            if (damageInfo.Weapon != null && damageInfo.Weapon.Verbs[SillyLittleCount] != null) return ((float)damageInfo.Weapon.Verbs[SillyLittleCount].affectedCellCount * (damageInfo.Amount / damageInfo.Weapon.Verbs[SillyLittleCount].defaultProjectile.projectile.GetDamageAmount(1))) / 100;
            return 0f;
        }
        public static bool adjustedApplyArmor(ref float damageAmount, ref Verse.DamageDef def, float armorPenetration, Verse.Thing armorPiece, Pawn targetPawn, float armorRating, float baseDamage = 0, float armorDamage = 0)
        {
            bool metalArmor = false;
            if (armorPiece != null)
            {
                
                metalArmor = armorPiece.def.apparel.useDeflectMetalEffect || (armorPiece.Stuff != null && armorPiece.Stuff.IsMetal);
                //below needs to be my logic that changes how the armor damage works
                if (baseDamage != 0)
                {
                    float f = armorDamage * (damageAmount / baseDamage);
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
            float num = Mathf.Max(armorRating - armorPenetration,0f);
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
                armorDamage = retrieveArmorDamage(ref damageInfo, amount, baseDamage);
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
                        if (armorDamage == 0) metalArmor = adjustedApplyArmor(ref amount, ref holder, armorPen, apparel, pawn, apparel.GetStatValue(armorRatingStat));//if weapon has patch pass else dont
                        else metalArmor = adjustedApplyArmor(ref amount, ref holder, armorPen, apparel, pawn, apparel.GetStatValue(armorRatingStat), baseDamage, armorDamage);
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
            bool metalArmor2;
            if (armorDamage == 0) metalArmor2 = adjustedApplyArmor(ref amount, ref holder, armorPen, null, pawn, pawn.GetStatValue(armorRatingStat));//if weapon has patch pass else dont
            else metalArmor2 = adjustedApplyArmor(ref amount, ref holder, armorPen, null, pawn, pawn.GetStatValue(armorRatingStat), baseDamage, armorDamage);
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
        public static void EvilHasBeenCommited()
        {
            List<ThingDef> myGuns = DefDatabase<ThingDef>.AllDefs.ToList();
            myGuns.RemoveAll(thing =>
            {
                return thing.weaponTags == null || thing.equipmentType != EquipmentType.Primary;
            });
            
            foreach (ThingDef t in myGuns)
            {
                if (t.Verbs.Count > SillyLittleCount)
                {
                    Verse.Log.Warning("This is Increasing Silly " + t.defName);
                    SillyLittleCount = t.Verbs.Count;
                }
            }
            List<Lucids_Damage> myAdjustments = DefDatabase<Lucids_Damage>.AllDefs.ToList();
            foreach (Lucids_Damage t in myAdjustments)
            {
                Verse.Log.Warning("This is the Lucid: " + t.defName);
                ThingDef gunnery = myGuns.Find(b =>
                {
                    if (b.defName == t.defName) return true;
                    return false;
                });
                if(gunnery != null)
                {
                    int evilCount = gunnery.Verbs.Count;
                    for (int i = evilCount; i < SillyLittleCount; i++)
                    {
                        gunnery.Verbs.Add(null);
                    }
                    Verse.Log.Warning("SillyCount: " + SillyLittleCount);
                    gunnery.Verbs.Add(new VerbProperties{ defaultProjectile = gunnery.Verbs[0].defaultProjectile, affectedCellCount = t.shieldDamage, burstShotCount = t.armorDamage });
                    Verse.Log.Warning("The Revolvers VerbCount: " + gunnery.Verbs.Count);
                    //okay we cannot actually change the basedamage or the armorpenetration in here, that will have to be done in patches sadly...
                }

            }
        }
    }
}
