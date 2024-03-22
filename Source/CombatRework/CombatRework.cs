using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using CombatRework;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatRework
{
    [StaticConstructorOnStartup]
    [HarmonyDebug]
    public static class CombatRework
    {
        static CombatRework()
        {
            Harmony harmony = new Harmony("rimworld.mod.Pelican.CombatRework");
            Harmony.DEBUG = true;
            Verse.Log.Warning("HEY");
            Verse.Log.Warning("HEY2: " + nameof(ArmorUtility.GetPostArmorDamage));
            harmony.PatchAll();
            DamageDefAdjustManager.onLoad();
        }
    }
}
[HarmonyPatch(typeof(DamageWorker_AddInjury))]
[HarmonyPatch("ApplyDamageToPart")]
[HarmonyPatch()]
public static class DamageWorker_AddInjury_ApplyDamage_Patch
{
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
    {
        var lineList = new List<CodeInstruction>(lines);

        bool found = false;
        int replacePoint = 0;
        while (!found && replacePoint < lineList.Count)
        {
            replacePoint++;
            if (lineList[replacePoint].ToString().Contains("GetPostArmorDamage")) found = true;
        }

        replacePoint -= 12; //this is the line ldraga.s dinfo, we are removing the logic to create and store damagedef because we are doing that more efficiently in our new function because we have to pass a reference to the damageinfo

        List<CodeInstruction> myInstructs = new List<CodeInstruction>();

        //load pawn onto stack
        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg_2, null));
        //load num onto stack
        myInstructs.Add(new CodeInstruction(OpCodes.Ldloc_0, null));
        //load damageinfo onto stack as reference or pointer
        myInstructs.Add(new CodeInstruction(OpCodes.Ldarga, 1));
        //load out bool deflectbyMetalARmor onto stack
        myInstructs.Add(new CodeInstruction(OpCodes.Ldloca_S, 1));
        //load out bool diminished by metalarmor onto stack
        myInstructs.Add(new CodeInstruction(OpCodes.Ldloca_S, 3));


        //damageDefAdjustManager.adjustedGetPostArmorDamage(pawn, num, ref damageInfo, out bool deflectedByMetalArmor, out bool diminishedByMetalArmor)
        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefAdjustManager), "adjustedGetPostArmorDamage"));

        //load above argument into num
        myInstructs.Add(new CodeInstruction(OpCodes.Stloc_0, null));

        lineList.RemoveRange(replacePoint, 17);//remove the il code we are replacing and the damageDef creation from IL_0037-IL_0060
        lineList.InsertRange(replacePoint, myInstructs);

        return lineList;
    }
}
[HarmonyPatch(typeof(CompShield))]
[HarmonyPatch("PostPreApplyDamage")]
[HarmonyPatch()]
public static class CompShield_PostPreApplyDamage_Patch
{
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
    {
        var lineList = new List<CodeInstruction>(lines);

        bool found = false;
        int replacePoint = 0;
        while (!found && replacePoint < lineList.Count)
        {
            replacePoint++;
            if (lineList[replacePoint].ToString().Contains("get_Amount")) found = true;
        }

        List<CodeInstruction> myInstructs = new List<CodeInstruction>();

        //call retrieveShieldDamage(ref Verse.DamageInfo damageInfo), damageinfo is already loaded onto stack as a reference or pointer for us :3
        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefAdjustManager), "retrieveShieldDamage"));

        lineList.RemoveRange(replacePoint, 5);//remove the il from IL_0059-IL_0069
        lineList.InsertRange(replacePoint, myInstructs);

        return lineList;
    }
}
[HarmonyPatch(typeof(PawnWeaponGenerator))]
[HarmonyPatch("Reset")]
[HarmonyPatch()]
public static class PawnWeaponGenerator_Reset_Patch
{
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
    {
        var lineList = new List<CodeInstruction>(lines);

        int replacePoint = 0;

        List<CodeInstruction> myInstructs = new List<CodeInstruction>();

        //call damageDefAdjustManager.EvilHasBeenCommited();
        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefAdjustManager), "EvilHasBeenCommited"));

        lineList.InsertRange(replacePoint, myInstructs);

        return lineList;
    }
}
//[HarmonyPatch(typeof(RimWorld.CompProjectileInterceptor))]
//[HarmonyPatch("CheckIntercept")]
//public static class SheildIntercept_Patch//this is the non-shieldpack shields
//{
//    [HarmonyTranspiler]
//    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
//    {
//        //this is the patch for damage for shields that are not on a character, like mechshields and broadshields

//        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);
//        int adjustPoint = 0;
//        int found = 0;
//        while (found < 2 && adjustPoint < lineList.Count)
//        {
//            adjustPoint += 1;

//            if (lineList[adjustPoint].opcode == OpCodes.Ble_S)
//            {
//                found += 1;
//            }

//            if (lineList[adjustPoint].ToString().Contains("TriggerEffecter")) found = 200;
//        }
//        //adjustPoint += 1;

//        List<CodeInstruction> myInstructs = new List<CodeInstruction>();

//        lineList.InsertRange(0, myInstructs);
//        //okay this is where we need to add our shielddamage onload function

//        return lineList;
//    }
//}

//[HarmonyPatch(typeof(RimWorld.CompShield))]//this is shieldpack shields
//[HarmonyPatch("PostPreApplyDamage")]
//public static class CompShield_PostPreApplyDamage_Patch
//{
//    [HarmonyTranspiler]
//    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
//    {
//        //this is the patch for the shieldpack damage
//        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);

//        List<CodeInstruction> myInstructs = new List<CodeInstruction>();


//        //damaginfo.weapon.defname
//        //myInstructs.Add(new CodeInstruction(OpCodes.Ldarga_S, 1));
//        //myInstructs.Add(CodeInstruction.Call(typeof(DamageInfo), "get_Weapon"));
//        //myInstructs.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDef), "defName")));
//        //DamageDefAdjustManager.pulldamagedef(damageinfo.weapon)
//        //myInstructs.Add(CodeInstruction.Call(typeof(DamageDefAdjustManager), "partTwo"));
//        //pop
//        //myInstructs.Add(new CodeInstruction(OpCodes.Pop, null));

//        lineList.InsertRange(0, myInstructs);

//        return lineList;
//    }
//}
//[HarmonyPatch(typeof(Verse.ProjectileProperties), "GetDamageAmount",new Type[]{typeof(float), typeof(StringBuilder)})]
//public static class ProjectileProperties_GetDamageAmount_Patch
//{
//    [HarmonyTranspiler]
//    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
//    {

//        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);

//        List<CodeInstruction> myInstructs = new List<CodeInstruction>();

//        //if(explanation != null)
//        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg, 2));
//        myInstructs.Add(new CodeInstruction(OpCodes.Brfalse_S));
//        int jf1 = myInstructs.Count - 1;

//        //if(explanation.ToString == "yes)
//        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg_2));
//        myInstructs.Add(CodeInstruction.Call(typeof(StringBuilder), "ToString"));
//        myInstructs.Add(new CodeInstruction(OpCodes.Ldstr, "yes"));
//        myInstructs.Add(new CodeInstruction(OpCodes.Beq));
//        int jf2 = myInstructs.Count - 1;

//        //damageamountbase = weapondamagemultiplier
//        myInstructs.Add(new CodeInstruction(OpCodes.Ldstr, "Returning Base"));
//        Type[] myParams = { typeof(string) };
//        myInstructs.Add(CodeInstruction.Call(typeof(Verse.Log), "Warning", myParams));

//        //if(damageamountbase == null)
//        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg, 0));
//        myInstructs.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Verse.ProjectileProperties), "damageAmountBase")));
//        myInstructs.Add(new CodeInstruction(OpCodes.Brfalse_S));
//        int jf3 = myInstructs.Count - 1;

//        myInstructs.Add(new CodeInstruction(OpCodes.Ldstr, "Before Damage Assignment"));
//        myInstructs.Add(CodeInstruction.Call(typeof(Verse.Log), "Warning", myParams));

//        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg, 1));
//        myInstructs.Add(new CodeInstruction(OpCodes.Ldarga, 0));
//        myInstructs.Add(new CodeInstruction(OpCodes.Stind_R4, AccessTools.Field(typeof(Verse.ProjectileProperties), "damageAmountBase")));

//        //return 0
//        //myInstructs.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
//        myInstructs.Add(new CodeInstruction(OpCodes.Ret));

//        Label jt1Label = il.DefineLabel();
//        lineList[0].labels.Add(jt1Label);

//        myInstructs[jf1].operand = jt1Label;
//        myInstructs[jf2].operand = jt1Label;
//        myInstructs[jf3].operand = jt1Label;

//        lineList.InsertRange(0, myInstructs);


//        return lineList;
//    }
//}
//[HarmonyPatch(typeof(Verse.DebugThingPlaceHelper))]
//[HarmonyPatch("DebugSpawn")]
//public static class DebugThingPlaceHelper_DebugSpawn_Patch
//{
//    [HarmonyTranspiler]
//    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
//    {
//        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);

//        int found = 0;
//        int adjustPoint = 0;
//        while (found < 2 && adjustPoint < lineList.Count)
//        {
//            adjustPoint++;
//            if (lineList[adjustPoint].opcode == OpCodes.Brfalse_S) found += 1;
//        }
//        ++adjustPoint;

//        List<CodeInstruction> myInstructs = new List<CodeInstruction>();
//        //verse.log.warning("This means...");
//        //myInstructs.Add(new CodeInstruction(OpCodes.Ldstr, "This Means that the thing does not generate with a comp quality automatically"));
//        //Type[] myParams = { typeof(string) };
//        //myInstructs.Add(CodeInstruction.Call(typeof(Verse.Log), "Warning", myParams));

//        lineList.InsertRange(adjustPoint, myInstructs);

//        return lineList;
//    }
//}
//[HarmonyPatch(typeof(Verse.PawnGenerator))]
//[HarmonyPatch("PostProcessGeneratedGear")]
//public static class PawnGenerator_PostProcessGeneratedGear_Patch
//{
//    [HarmonyTranspiler]
//    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
//    {
//        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);

//        bool found = false;
//        int adjustPoint = 0;
//        while (!found && adjustPoint < lineList.Count)
//        {
//            adjustPoint++;
//            if (lineList[adjustPoint].opcode == OpCodes.Brfalse_S) found = true;
//        }
//        ++adjustPoint;

//        List<CodeInstruction> myInstructs = new List<CodeInstruction>();
//        //verse.log.warning("This means...");
//        //myInstructs.Add(new CodeInstruction(OpCodes.Ldstr, "This Means that the thing does not generate with a comp quality automatically || In PostProcessGeneratedGear"));
//        //Type[] myParams = { typeof(string) };
//        //myInstructs.Add(CodeInstruction.Call(typeof(Verse.Log), "Warning", myParams));


//        //print(thing.thingdef.defname)
//        //myInstructs.Add(new CodeInstruction(OpCodes.Ldarg_0, null));
//        //myInstructs.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "def")));
//        //myInstructs.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDef), "defName")));
//        //myInstructs.Add(CodeInstruction.Call(typeof(Verse.Log), "Warning", myParams));

//        //print(pawn.name.fullstring)
//        //myInstructs.Add(new CodeInstruction(OpCodes.Ldarg_1, null));
//        //myInstructs.Add(CodeInstruction.Call(typeof(Verse.Pawn), "get_Name"));
//        //myInstructs.Add(CodeInstruction.Call(typeof(Verse.Name), "get_ToStringFull"));
//        //myInstructs.Add(CodeInstruction.Call(typeof(Verse.Log), "Warning", myParams));
//        lineList.InsertRange(adjustPoint, myInstructs);

//        return lineList;
//    }
//}
//[HarmonyPatch(typeof(Verse.ThingWithComps))]
//[HarmonyPatch("PostMake")]
//public static class ThingWithComps_PostMake_Patch
//{
//    [HarmonyTranspiler]
//    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
//    {
//        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);


//        List<CodeInstruction> myInstructs = new List<CodeInstruction>();
//        //printmyinfo(thingwithcomps);
//        //myInstructs.Add(new CodeInstruction(OpCodes.Ldarg_0, null));
//        //myInstructs.Add(CodeInstruction.Call(typeof(CombatRework.DamageDefAdjustManager), "printMyInfo"));

//        lineList.InsertRange(0, myInstructs);

//        return lineList;
//    }
//}
//[HarmonyPatch(typeof(Verse.ThingWithComps))]
//[HarmonyPatch("InitializeComps")]
//public static class ThingWithComps_InitializeComps_Patch
//{
//    [HarmonyTranspiler]
//    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
//    {
//        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);

//        bool found = false;
//        int adjustPoint = 0;
//        //while (!found && adjustPoint < lineList.Count)
//        //{
//        //    if (lineList[adjustPoint].opcode == OpCodes.Stfld) found = false;
//        //}
//        adjustPoint += 1;

//        List<CodeInstruction> myInstructs = new List<CodeInstruction>();
//        //printmyinfo(thingwithcomps);
//        //myInstructs.Add(new CodeInstruction(OpCodes.Ldarg_0, null));
//        //myInstructs.Add(CodeInstruction.Call(typeof(CombatRework.DamageDefAdjustManager), "printMyInfo"));

//        lineList.InsertRange(lineList.Count - 1, myInstructs);

//        return lineList;
//    }
//}
//[HarmonyPatch(typeof(Verse.Thing))]
//[HarmonyPatch("TakeDamage")]
//public static class Thing_TakeDamage_Patch
//{
//    [HarmonyTranspiler]
//    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
//    {
//        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);

//        bool found = false;
//        int adjustPoint = 0;
//        //while (!found && adjustPoint < lineList.Count)
//        //{
//        //    if (lineList[adjustPoint].opcode == OpCodes.Stloc_2) found = true;
//        //}
//        //adjustPoint += 1;

//        List<CodeInstruction> myInstructs = new List<CodeInstruction>();
//        //if(thing thing is pawn);
//        //myInstructs.Add(new CodeInstruction(OpCodes.Ldarg_0, null));
//        //myInstructs.Add(new CodeInstruction(OpCodes.Isinst, typeof(Verse.Pawn)));
//        //myInstructs.Add(new CodeInstruction(OpCodes.Stloc_S, 6));

//        //myInstructs.Add(new CodeInstruction(OpCodes.Ldloc_S, 6));
//        //myInstructs.Add(new CodeInstruction(OpCodes.Brtrue_S, null));
//        //int jf1 = myInstructs.Count - 1;

//        //applytopawn(dinfo, pawn, 

//        //verse.log.warning(this.def.defname)
//        //myInstructs.Add(new CodeInstruction(OpCodes.Ldarg_0, null));
//        //myInstructs.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "def")));
//        //myInstructs.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDef), "defName")));
//        //Type[] myParams = { typeof(string) };
//        //myInstructs.Add(CodeInstruction.Call(typeof(Verse.Log), "Warning", myParams));

//        lineList.InsertRange(0, myInstructs);

//        return lineList;
//    }
//}
//[HarmonyPatch(typeof(RimWorld.Bullet))]
//[HarmonyPatch("Impact")]
//public static class Bullet_Impact_Patch
//{
//    [HarmonyTranspiler]
//    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
//    {
//        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);

//        bool found = false;
//        int adjustPoint = 0;
//        while (!found && adjustPoint < lineList.Count)
//        {
//            if (lineList[adjustPoint].ToString().Contains("Verse.DamageInfo")) found = true;
//            adjustPoint++;
//        }
//        adjustPoint += 1;


//        List<CodeInstruction> myInstructs = new List<CodeInstruction>();

//        //myInstructs.Add(new CodeInstruction(OpCodes.Ldarg_0, null));
//        //myInstructs.Add(CodeInstruction.Call(typeof(CombatRework.DamageDefAdjustManager), "printBullet"));

//        lineList.InsertRange(adjustPoint, myInstructs);

//        return lineList;
//    }
//}
//[HarmonyPatch(typeof(Verse.ProjectileProperties), "GetDamageAmount",new Type[]{typeof(float), typeof(StringBuilder)})]
//[HarmonyPatch(typeof(Verse.DamageWorker_AddInjury), "FinalizeAndAddInjury", new Type[] {typeof(Pawn), typeof(Hediff_Injury), typeof(DamageInfo), typeof(Verse.DamageWorker.DamageResult)})]
//public static class Finalize_Injury_Patch
//{
//    [HarmonyTranspiler]
//    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
//    {
//        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);

//        //        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefAdjustManager), "GetPostArmorDamage"));
//        //myInstructs.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "def")));

//        List<CodeInstruction> myInstructs = new List<CodeInstruction>();

//        //call printMyShit(injury);

//        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg_2, null));
//        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefAdjustManager), "PrintMyShit"));

//        //call print(injury.source)
//        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg_2, null));
//        myInstructs.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Verse.Thing), "def")));
//        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefAdjustManager), "Printer"));

//        lineList.InsertRange(0, myInstructs);

//        return lineList;

//    }
//}
//[HarmonyPatch(typeof(Verse.ArmorUtility))]
//[HarmonyPatch("ApplyArmor")]
//public static class Apply_Armor_Patch
//{
//    [HarmonyTranspiler]
//    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
//    {
//        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);

//        List<CodeInstruction> myInstructs = new List<CodeInstruction>();

//        //call print(damageDef)

//        myInstructs.Add(new CodeInstruction(OpCodes.Ldarga_S, 5));
//        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefAdjustManager), "MyPrinter"));

//        //call print(pawn)
//        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg, 6));
//        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefAdjustManager), "MyPrinted"));


//        lineList.InsertRange(0, myInstructs);

//        return lineList;
//    }
//}
//[HarmonyPatch(typeof(Verse.Thing))]
//[HarmonyPatch("TakeDamage")]
//public static class Take_Damage_Patch
//{
//    [HarmonyTranspiler]
//    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
//    {
//        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);

//        List<CodeInstruction> myInstructs = new List<CodeInstruction>();

//        //call damaging(dinfo)

//        myInstructs.Add(new CodeInstruction(OpCodes.Ldarga_S, 1));
//        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg_0, null));
//        myInstructs.Add(CodeInstruction.Call(typeof(CombatRework.DamageDefAdjustManager), "Damaging"));

//        lineList.InsertRange(0, myInstructs);

//        return lineList;
//    }
//}