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
            Verse.Log.Warning("Project Loaded");
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
public static class RimWorldBill_DoInterface_Patch//this is the non-shieldpack shields
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
        adjustPoint -= 4;

        int jt1 = adjustPoint;//also where the brtrue will jump too
        int jt2 = adjustPoint;//where the br will jump too
        while (!(lineList[jt2].ToString().Contains("stloc.2") && lineList[jt2 - 1].ToString().Contains("CreatedBy")))
        {
            jt2++;
        }

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
        //call billRetrieveQuality(recipe, pawn)
        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefManager), "billRetrieveQuality"));
        //jump to stloc.2 to save the quality as q
        myInstructs.Add(new CodeInstruction(OpCodes.Br, null));
        int jp2 = myInstructs.Count - 1;

        //adding label to first il code so it jumps to this instead of the j1

        Label l1 = il.DefineLabel();

        myInstructs[0].labels.Add(l1);

        int recipedefworkskillnullbrtrues = adjustPoint;
        while (!(lineList[recipedefworkskillnullbrtrues].ToString().Contains("brtrue") && lineList[recipedefworkskillnullbrtrues - 1].ToString().Contains("workSkill")))
        {
            recipedefworkskillnullbrtrues--;
        }

        lineList[recipedefworkskillnullbrtrues].operand = l1;

        //adding labels for jumps
        Label J1 = il.DefineLabel();

        lineList[jt1].labels.Add(J1);

        myInstructs[jp1].operand = J1;

        Label J2 = il.DefineLabel();

        lineList[jt2].labels.Add(J2);

        myInstructs[jp2].operand = J2;

        lineList.InsertRange(adjustPoint, myInstructs);


        return lineList;

    }
}
//[HarmonyPatch(typeof(RimWorld.WorkGiver_DoBill))]
//[HarmonyPatch("IsUsableIngredient")]
//public static class RimWorldWorkGiverDoBill_IsUsableIngredient_Patch
//{
//    [HarmonyTranspiler]
//    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
//    {

//        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);
//        int adjustPoint = 1;
//        while (!(lineList[adjustPoint].ToString().Contains("ldc.i4.0") && lineList[adjustPoint - 1].ToString().Contains("endfinally")))
//        {
//            adjustPoint++;
//        }

//        adjustPoint++;

//        List<CodeInstruction> myInstructs = new List<CodeInstruction>();

//        //load bill
//        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg, 1));
//        //load thing
//        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg, 0));
//        //cal hiddenIngredeint(useless bool, biil, thing), useless bool just takes an argument the thing loads that I dont know how to get rid of
//        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefManager), "hiddenIngredient"));
        
//        lineList.InsertRange(adjustPoint, myInstructs);

//        return lineList;
//    }
//}
//might need to uncomment^
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
[HarmonyPatch(typeof(Verse.AI.Toils_Recipe))]
[HarmonyPatch("CalculateIngredients")]
public static class VerseAIToilsRecipe_CalculateIngredients_Patch
{
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
    {

        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);
        int adjustPoint = 3;
        while (!(lineList[adjustPoint - 3].ToString().Contains("ldc.i4.0") && lineList[adjustPoint - 2].ToString().Contains("stloc") && lineList[adjustPoint - 1].ToString().Contains("br") && lineList[adjustPoint].ToString().Contains("ldarg")))
        {
            adjustPoint++;
        }
        adjustPoint++;

        CodeInstruction copy = lineList[adjustPoint];

        List<CodeInstruction> myInstructs = new List<CodeInstruction>();

        //load placedthings
        myInstructs.Add(copy);
        //call uftFlip(placedThings)
        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefManager), "uftFlip"));
        //recall ldarg.0
        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg_0, null));
        //recall copied code
        myInstructs.Add(copy);

        lineList.InsertRange(adjustPoint, myInstructs);

        return lineList;
    }
}
//[HarmonyPatch(typeof(RimWorld.BillRepeatModeUtility))]
//[HarmonyPatch("MakeConfigFloatMenu")]
//public static class RimWorldBillRepeatModeUtility_MakeConfigFloatMenu_Patch
//{
//    [HarmonyTranspiler]
//    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
//    {

//        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);
//        int adjustPoint = 1;
//        while ((lineList[adjustPoint - 1].ToString().Contains(">::Add")) && (lineList[adjustPoint].ToString().Contains("TargetCount")) && (lineList[adjustPoint + 1].ToString().Contains("LabelCap")))
//        {
//            adjustPoint++;
//        }
//        int addJP = 1;
//        while ((lineList[addJP].ToString().Contains("ldloc.1")) && (lineList[addJP - 1].ToString().Contains(">::Add")) && (lineList[addJP + 1].ToString().Contains("Forever")))
//        {
//            addJP++;
//        }
//        List<CodeInstruction> myInstructs = new List<CodeInstruction>();

//        //load bill onto stack
//        myInstructs.Add(new CodeInstruction(OpCodes.Ldarga, 0));
//        //call billGendered(bill) to see if we jump over add dialogue "Do until have x"
//        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefManager), "billGendered"));
//        //jump instruct if bill gendered returns true
//        myInstructs.Add(new CodeInstruction(OpCodes.Brtrue, null));

//        //add jump to
//        Label JT = il.DefineLabel();

//        lineList[addJP].labels.Add(JT);

//        myInstructs[myInstructs.Count - 1].operand = JT;

//        lineList.InsertRange(adjustPoint, myInstructs);

//        return lineList;
//    }
//}
//[HarmonyPatch(typeof(InspectGizmoGrid))]
//[HarmonyPatch("DrawInspectGizmoGridFor")]
//public static class InspectGizmoGrid_DrawInspectGizmoGridFor_Patch//this is the non-shieldpack shields
//{
//    [HarmonyTranspiler]
//    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
//    {
//        //this is the patch for damage for shields that are not on a character, like mechshields and broadshields

//        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);
//        int adjustPoint = 0;

//        List<CodeInstruction> myInstructs = new List<CodeInstruction>();

//        lineList.InsertRange(adjustPoint, myInstructs);

//        return lineList;
//    }
//}
//[HarmonyPatch(typeof(RimWorld.PlaceWorker_ShowFacilitiesConnections))]
//[HarmonyPatch("DrawGhost")]
//public static class RimWorldPlaceWorker_ShowFacilitiesConnections_DrawGhost_Patch//this is the non-shieldpack shields
//{
//    [HarmonyTranspiler]
//    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> lines, ILGenerator il)
//    {
//        //this is the patch for damage for shields that are not on a character, like mechshields and broadshields

//        List<CodeInstruction> lineList = new List<CodeInstruction>(lines);
//        int adjustPoint = 1;

//        while (lineList[adjustPoint - 1].opcode == OpCodes.Stloc)
//        {
//            adjustPoint++;
//        }

//        List<CodeInstruction> myInstructs = new List<CodeInstruction>();

//        ////call bool thingComped(thing)

//        //myInstructs.Add(new CodeInstruction(OpCodes.Ldarg, 0));

//        //myInstructs.Add(CodeInstruction.Call(typeof(DamageDefManager), "thingComped"));

//        ////if thing comped returns false skip

//        //myInstructs.Add(new CodeInstruction(OpCodes.Brfalse, null));// add jump over here

//        //int jf1 = myInstructs.Count - 1;

//        //call drawConnectLines(rot, pos)

//        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg_3, null));
//        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg_2, null));
//        myInstructs.Add(new CodeInstruction(OpCodes.Ldarg_1, null));
//        myInstructs.Add(CodeInstruction.Call(typeof(DamageDefManager), "callDraw"));

//        lineList.InsertRange(adjustPoint, myInstructs);

//        //insert label
//        //Label jtLabel = il.DefineLabel();

//        //lineList[adjustPoint].labels.Add(jtLabel);

//        //myInstructs[jf1].operand = jtLabel;

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