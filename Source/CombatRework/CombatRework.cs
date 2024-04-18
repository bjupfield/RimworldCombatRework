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
using Verse.AI;

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
            harmony.PatchAll();
            DamageDefManager.onLoad();
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
        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefManager), "adjustedGetPostArmorDamage"));

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
        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefManager), "retrieveShieldDamage"));

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
        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefManager), "EvilHasBeenCommited"));

        lineList.InsertRange(replacePoint, myInstructs);

        return lineList;
    }
}
[HarmonyPatch(typeof(Verse.Zone))]
[HarmonyPatch("Delete")]
public static class VerseZone_Delete_Patch
{
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
    {

        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);
        int adjustPoint = 0;

        List<CodeInstruction> myInstructs = new List<CodeInstruction>();

        //load zone onto stack
        myInstructs.Add(new CodeInstruction(OpCodes.Ldarga, 0));
        //call unregistzone(zone)
        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefManager), "unRegisterZone"));

        lineList.InsertRange(adjustPoint, myInstructs);

        return lineList;
    }
}
[HarmonyPatch(typeof(RimWorld.BillStack))]
[HarmonyPatch("AddBill")]
public static class RimWorldBillStack_AddBill_Patch
{
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
    {

        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);
        int adjustPoint = 0;
        while (!lineList[adjustPoint].ToString().Contains("Add"))
        {
            adjustPoint++;
        }
        adjustPoint++;
        
        List<CodeInstruction> myInstructs = new List<CodeInstruction>();

        //load bill onto stack
        myInstructs.Add(new CodeInstruction(OpCodes.Ldarga, 1));
        //call createBillManager(bill) to create a bill manager when the player creates a bill
        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefManager), "createBillManager"));

        lineList.InsertRange(adjustPoint, myInstructs);

        return lineList;
    }
}
[HarmonyPatch(typeof(RimWorld.Bill))]
[HarmonyPatch("DoInterface")]
public static class RimWorldBill_DoInterface_Patch//removes some cpabilities
{
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
    {

        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);
        int adjustPoint = 0;
        int count = 0;
        while (count < 2)
        {
            if (lineList[adjustPoint].ToString().Contains("Delete")) count++;
            adjustPoint++;
        }
        adjustPoint -= 4;

        List<CodeInstruction> myInstructs = new List<CodeInstruction>();

        //load bill onto stack
        myInstructs.Add(new CodeInstruction(OpCodes.Ldarga, 0));
        //call deleteBillManager(bill) to delete the bill manager when the player clicks to delete the fake bill
        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefManager), "deleteBillManager"));

        lineList.InsertRange(adjustPoint, myInstructs);

        adjustPoint = 0;
        while (count < 4)
        {
            if (lineList[adjustPoint].ToString().Contains("Delete")) count++;
            adjustPoint++;
        }

        return lineList;

    }
}
[HarmonyPatch(typeof(RimWorld.Dialog_BillConfig))]
[HarmonyPatch("DoWindowContents")]
public static class RimWorldDialogBillConfig_DoWindowContetns_Patch//this is the non-shieldpack shields
{
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
    {
        //ldarg.0
        //ldfld class rimworld.bill_production rimworld.dialog_billconfig::bill 
        //^ these two line load the bill
        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);
        int adjustPoint = 0;
        while (!lineList[adjustPoint].ToString().Contains("ldfld"))
        {
            adjustPoint++;
        }

        CodeInstruction ldfld = lineList[adjustPoint];

        while (!(lineList[adjustPoint - 1].ToString().Contains("Verse.Listing::Begin") && lineList[adjustPoint].ToString().Contains("ldloc.s")))
        {
            adjustPoint++;
        }


        List<CodeInstruction> myInstructs = new List<CodeInstruction>();

        //load dialog_bill onto stack
        myInstructs.Add(new CodeInstruction(OpCodes.Ldarga, 0));
        //call billMale(bill) to see if male and skip if so
        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefManager), "billMale"));
        //if(billmale(bill)) to jt1
        myInstructs.Add(new CodeInstruction(OpCodes.Brtrue, null));
        int jp1 = myInstructs.Count - 1;

        //find jt1
        int jt1 = adjustPoint;
        while (!(lineList[jt1 - 4].ToString().Contains("6") && lineList[jt1 - 3].ToString().Contains("EndSection") && lineList[jt1 - 2].ToString().Contains("5") && lineList[jt1 - 1].ToString().Contains("12") && lineList[jt1].ToString().Contains("Gap")))
        {

            jt1++;
        }
        jt1++;

        //add label to jump too for jt1
        Label jT1 = il.DefineLabel();

        lineList[jt1].labels.Add(jT1);

        myInstructs[jp1].operand = jT1;

        lineList.InsertRange(adjustPoint, myInstructs);

        adjustPoint = 0;
        while (!lineList[adjustPoint].ToString().Contains("billMale"))
        {
            adjustPoint++;
        }

        //okay this isnt working, i dont know why... it should work, im simply removing a section with an if statement... but it of course crashes when i do this

        //doing another I think its easier to write

        myInstructs.Clear();

        while (!(lineList[adjustPoint - 3].ToString().Contains("EndSection") && lineList[adjustPoint - 2].ToString().Contains("5") && lineList[adjustPoint - 1].ToString().Contains("End") && lineList[adjustPoint].ToString().Contains("ldloca.s 3")))
        {
            adjustPoint++;
        }

        //load dialog_bill onto stack
        myInstructs.Add(new CodeInstruction(OpCodes.Ldarga, 0));
        //call billMale(bill) to see if male and skip if so
        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefManager), "billMale"));
        //if(billmale(bill)) to jt2
        myInstructs.Add(new CodeInstruction(OpCodes.Brtrue, null));
        int jp2 = myInstructs.Count - 1;

        int jt2 = adjustPoint;
        while (!(lineList[jt2].ToString().Contains("DoIngredientConfigPane")))
        {
            jt2++;
        }
        jt2++;
        //add label to jump too for jt2
        Label jT2 = il.DefineLabel();

        lineList[jt2].labels.Add(jT2);

        myInstructs[jp2].operand = jT2;

        lineList.InsertRange(adjustPoint, myInstructs);

        return lineList;

    }
}
[HarmonyPatch(typeof(Verse.GenRecipe))]
[HarmonyPatch("PostProcessProduct")]
public static class VerseGenRecipe_PostProcessProduct_Patch//this is the non-shieldpack shields
{
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
    {

        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);
        int adjustPoint = 1;
        while (!(lineList[adjustPoint].ToString().Contains("ldarg.2") && lineList[adjustPoint - 1].ToString().Contains("Error")))
        {
            adjustPoint++;
        }
        adjustPoint++;

        List<CodeInstruction> myInstructs = new List<CodeInstruction>();

        //load recipe onto stack
        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg, 1));
        //call billRepair(recipe) check if recipe is hiddenrecipe
        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefManager), "billRepair"));
        //jump to jt1 if true
        myInstructs.Add(new CodeInstruction(OpCodes.Brtrue, null));
        int jp1 = myInstructs.Count - 1;
        //load recipe onto stack
        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg, 1));
        //load pawn onto stack
        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg, 2));
        //load thing
        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg, 0));
        //call billRetrieveQuality(recipe, pawn)
        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefManager), "billRetrieveQuality"));
        //call stloc.2
        myInstructs.Add(new CodeInstruction(OpCodes.Stloc_2, null));
        //jump to ldloc.0 to set the compquality.setquality()
        myInstructs.Add(new CodeInstruction(OpCodes.Br, null));
        int jp2 = myInstructs.Count - 1;
        //replace ldarg2 that we eat out of laziness
        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg_2, null));
        int jt1 = myInstructs.Count - 1;

        //adding label to first il code so it jumps to this instead of the j1

        int jt2 = adjustPoint;
        while (!(lineList[jt2].ToString().Contains("ldloc.0") && lineList[jt2 - 1].ToString().Contains("stloc.2")))
        {
            jt2++;
        }

        //adding labels for jumps
        Label J1 = il.DefineLabel();

        myInstructs[jt1].labels.Add(J1);

        myInstructs[jp1].operand = J1;

        Label J2 = il.DefineLabel();

        lineList[jt2].labels.Add(J2);

        myInstructs[jp2].operand = J2;

        lineList.InsertRange(adjustPoint, myInstructs);

        return lineList;

    }
}
[HarmonyPatch(typeof(RimWorld.WorkGiver_DoBill))]
[HarmonyPatch("TryFindBestIngredientsInSet_NoMixHelper")]
public static class RimWorldWorkGiverDoBill_TryFindBestIngredientsInSet_NoMixHelper_Patch
{
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
    {

        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);
        int adjustPoint = 1;
        while (!(lineList[adjustPoint - 1].ToString().Contains("ldc.i4.1") && lineList[adjustPoint - 2].ToString().Contains("ret") && lineList[adjustPoint].ToString().Contains("ret")))
        {
            adjustPoint++;
        }

        List<CodeInstruction> myInstructs = new List<CodeInstruction>();

        //load bill
        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg, 6));
        //load chosen
        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg, 2));
        //cal hiddenIngredeint2(useless bool, biil, chosen), useless bool just takes an argument the thing loads that I dont know how to get rid of
        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefManager), "hiddenIngredient2"));

        lineList.InsertRange(adjustPoint, myInstructs);

        return lineList;
    }
}
[HarmonyPatch(typeof(Verse.AI.Pawn_JobTracker))]
[HarmonyPatch("EndCurrentJob")]
public static class VerseAIPawnJobTracker_EndCurrentJob_Patch
{
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
    {

        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);
        int adjustPoint = 3;
        while (!(lineList[adjustPoint - 3].ToString().Contains("dup") && lineList[adjustPoint - 2].ToString().Contains("ldc") && lineList[adjustPoint - 1].ToString().Contains("ldarg") && lineList[adjustPoint].ToString().Contains("Verse.AI.Job")))
        {
            adjustPoint++;
        }

        CodeInstruction copy = lineList[adjustPoint];


        List<CodeInstruction> myInstructs = new List<CodeInstruction>();

        //call this
        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg_0, null));
        //load curjob
        myInstructs.Add(copy);
        //call endJob(curjob)
        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefManager), "endJob"));

        lineList.InsertRange(0, myInstructs);

        return lineList;
    }
}