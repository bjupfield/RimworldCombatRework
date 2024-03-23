using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            //myGuns.RemoveAll(thing =>
            //{
            //    return thing.weaponTags == null;
            //});
            //myGuns.RemoveAll(thing =>
            //{
            //    return thing.weaponTags.Count == 0;
            //});
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
            //String bulletString =  myGuns.Find(t =>
            //{
            //    return t.defName == "Gun_Revolver";
            //}).Verbs[0].defaultProjectile.defName;

            //myGuns.Find(t =>
            //{
            //    return t.defName == "Gun_Revolver";
            //}).Verbs[0].defaultProjectile.defName = "Bullet_EMPLauncher";
            //the above stuff shows that I cna just adjust the values of the stored 

            //Verse.Log.Warning("HEY THIS IS: " + myGuns.Find(t =>
            //{
            //    return t.defName == "Gun_Revolver";
            //}).Verbs[0].defaultProjectile.defName);


            //uint verb2count = 0;
            //foreach(ThingDef t in myGuns) 
            //{
            //    if (t.Verbs.Count > 1) verb2count++;
            //}

            //Verse.Log.Warning("Weapons with Multiple Verbs: " + verb2count);

            return 0;
        }
        public static float retrieveBaseDamage(ref Verse.DamageInfo damageInfo)
        {
            if (damageInfo.Weapon != null && damageInfo.Weapon.Verbs[0] != null) return damageInfo.Weapon.Verbs[0].defaultProjectile.projectile.GetDamageAmount(1);
            return 0;
        }
        public static float retrieveArmorDamage(ref Verse.DamageInfo damageInfo, float amount, float baseDamage)
        {
            if (amount != 0) return (float)damageInfo.Weapon.Verbs[SillyLittleCount].burstShotCount * (amount / baseDamage);
            return 0;
        }
        public static float retrieveShieldDamage(ref Verse.DamageInfo damageInfo)//energy if amount not found - base amount
        {
            if (damageInfo.Weapon != null && damageInfo.Weapon.Verbs[SillyLittleCount] != null) return ((float)damageInfo.Weapon.Verbs[SillyLittleCount].sprayWidth * (damageInfo.Amount / damageInfo.Weapon.Verbs[SillyLittleCount].defaultProjectile.projectile.GetDamageAmount(1))) / 100;
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
        public static void okayLolHaveToAssignAllValuesManually(ref VerbProperties verb)
        {
            //assign null values to every other value so it doesnt crash
            verb.category = VerbCategory.Misc;
            verb.verbClass = typeof(Verb);
            verb.label = "";
            verb.untranslatedLabel = "";
            verb.isPrimary = true;
            verb.violent = true;
            verb.minRange = 0;
            verb.range = 0;
            verb.rangeStat = StatDefOf.RangedWeapon_Cooldown;
            verb.noiseRadius = 0;
            verb.ticksBetweenBurstShots = 0;
            verb.hasStandardCommand = false;
            verb.targetable = true;
            verb.nonInterruptingSelfCast = false;
            verb.targetParams = new TargetingParameters();
            verb.requireLineOfSight = true;
            verb.mustCastOnOpenGround = false;
            verb.forceNormalTimeSpeed = true;
            verb.onlyManualCast = false;
            verb.stopBurstWithoutLos = true;
            verb.surpriseAttack = new SurpriseAttackProps();
            verb.commonality = -1f;
            verb.minIntelligence = new Intelligence();
            verb.consumeFuelPerBurst = 0;
            verb.consumeFuelPerShot = 0;
            verb.stunTargetOnCastStart = false;
            verb.invalidTargetPawn = "";
            verb.warmupTime = 0;
            verb.defaultCooldownTime = 0;
            verb.commandIcon = "";
            verb.soundCast = SoundDefOf.Ambient_AltitudeWind;
            verb.soundCastTail = SoundDefOf.Ambient_AltitudeWind;
            verb.soundAiming = SoundDefOf.Ambient_AltitudeWind;
            verb.muzzleFlashScale = 0;
            verb.impactMote = ThingDefOf.Cow;
            verb.impactFleck = FleckDefOf.AirPuff;
            verb.drawAimPie = false;
            verb.warmupEffecter = EffecterDefOf.AcidSpray_Directional;
            verb.drawHighlightWithLineOfSight = false;
            verb.aimingLineMote = ThingDefOf.Cow;
            verb.aimingLineMoteFixedLength = 0;
            verb.aimingChargeMote = ThingDefOf.Cow;
            verb.aimingChargeMoteOffset = 0f;
            verb.linkedBodyPartsGroup = BodyPartGroupDefOf.FullHead;
            verb.ensureLinkedBodyPartsGroupAlwaysUsable = false;
            verb.meleeDamageDef = DamageDefOf.Flame;
            verb.meleeDamageBaseAmount = 1;
            verb.meleeArmorPenetrationBase = -1f;
            verb.ai_IsWeapon = true;
            verb.ai_IsBuildingDestroyer = false;
            verb.ai_AvoidFriendlyFireRadius = 0;
            verb.ai_RangedAlawaysShootGroundBelowTarget = false;
            verb.ai_IsDoorDestroyer = false;
            verb.ai_ProjectileLaunchingIgnoresMeleeThreats = false;
            verb.ai_TargetHasRangedAttackScoreOffset = 0;
            verb.defaultProjectile = ThingDefOf.Cow;
            //need to do the weird reflection constructors on these as they are private...
            Type verbType = verb.GetType();
            FieldInfo missRadius = verbType.GetField("forcedMissRadius", BindingFlags.NonPublic | BindingFlags.Instance);
            missRadius.SetValue(verb, 0);


            FieldInfo forcedMissRadiusClassic = verbType.GetField("forcedMissRadiusClassicMortars", BindingFlags.NonPublic | BindingFlags.Instance);
            forcedMissRadiusClassic.SetValue(verb, -1f);

            verb.forcedMissEvenDispersal = false;

            FieldInfo MortarIS = verbType.GetField("isMortar", BindingFlags.NonPublic | BindingFlags.Instance);
            MortarIS.SetValue(verb, false);

            verb.accuracyTouch = 1f;
            verb.accuracyShort = 1f;
            verb.accuracyMedium = 1f;
            verb.accuracyLong = 1f;
            verb.canGoWild = true;
            verb.beamDamageDef = DamageDefOf.Bomb;
            verb.beamWidth = 1f;
            verb.beamMaxDeviation = 0;
            verb.beamGroundFleckDef = FleckDefOf.AirPuff;
            verb.beamEndEffecterDef = EffecterDefOf.AcidSpray_Directional;
            verb.beamMoteDef = ThingDefOf.Cow;
            verb.beamFleckChancePerTick = 0f;
            verb.beamCurvature = 0f;
            verb.beamChanceToStartFire = 0f;
            verb.beamChanceToAttachFire = 0;
            verb.beamStartOffset = 0;
            verb.beamFullWidthRange = 0;
            verb.beamLineFleckDef = FleckDefOf.AirPuff;
            verb.beamLineFleckChanceCurve = new SimpleCurve();
            verb.beamFireSizeRange = FloatRange.ZeroToOne;
            verb.soundCastBeam = SoundDefOf.Ambient_AltitudeWind;
            verb.beamTargetsGround = false;
            verb.sprayArching = 0;
            verb.sprayNumExtraCells = 0;
            verb.sprayThicknessCells = 0;
            verb.sprayEffecterDef = EffecterDefOf.AcidSpray_Directional;
            verb.spawnDef = ThingDefOf.Cow;
            verb.colonyWideTaleDef = TaleDefOf.AteRawHumanlikeMeat;
            verb.affectedCellCount = 0;
            verb.bodypartTagTarget = BodyPartTagDefOf.BloodFiltrationKidney;
            verb.rangedFireRulepack = RulePackDefOf.ArtDescriptionRoot_HasTale;
            verb.soundLanding = SoundDefOf.Ambient_AltitudeWind;
            verb.flightEffecterDef = EffecterDefOf.AcidSpray_Directional;
            verb.flyWithCarriedThing = true;
            verb.workModeDef = MechWorkModeDefOf.Work;
            Verse.Log.Warning("Do all these trigger");
            foreach (var field in verbType.GetFields())
            {
                Verse.Log.Warning(field.GetValue(verb) + "is initilized");
            }
            Verse.Log.Warning("Yes They do");


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
            okayLolHaveToAssignAllValuesManually(ref verb);//doing this before because it is a large assignment...

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
                    verb.affectedCellCount = t.shieldDamage == -1 ? 0 : t.shieldDamage;

                    Type tDef = foundWeapon.GetType();
                    FieldInfo verbs = tDef.GetField("verbs", BindingFlags.NonPublic | BindingFlags.Instance);
                    List<VerbProperties> adjustVerbs = (List<VerbProperties>)verbs.GetValue(foundWeapon);

                    Verse.Log.Warning("Revolvers VerbProperty defaultProjectile: " + adjustVerbs[0].defaultProjectile.defName);

                    for (int i = adjustVerbs.Count; i < SillyLittleCount; i++)//this adds null verbs to every weapon untill the all weapons have the same amount of verbs so we can access shielddamage and armor damage without conducting a search
                    {
                        adjustVerbs.Add(verb);
                    }
                    //adjustVerbs[0] = verb;
                    adjustVerbs.Add(verb);
                    Verse.Log.Warning("Is it firing in here?");

                }
                else
                {
                    Verse.Log.Error("The CombatRework Lucids_Damage: " + t.defName + " does not have a matching weapon. \nThis Error could be caused by having a submod active for a mod you no longer use.\n If you are making a submod your defName matches no weapon. Consider documentation.");
                }
            }

            //testing code straight from chatgpt to see If I can just do this
            //ConstructorInfo ctor = typeof(VerbProperties).GetConstructor(Type.EmptyTypes);

            //VerbProperties myVerb = (VerbProperties)ctor.Invoke(null);
            //myVerb.burstShotCount = 2;
            //myVerb.affectedCellCount = 3;
            //Verse.Log.Warning("In EvilHAsBeenCommited");
            //Verse.Log.Warning("Okay the thing Constructed BurstShot is: " + myVerb.burstShotCount);
            if (revolver != null)
            {
                //revolver.Verbs.Add(myVerb);
            }
            //okay this above code actually shows that the verbproperties struct can be constructed with this...
            //the info on this is c# reflection, look up the documentation from microsoft if interested
            
            

            
        }
    }
}
