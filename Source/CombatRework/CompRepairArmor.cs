using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorld
{
    public class CompRepairArmor : ThingComp
    {
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            Verse.Log.Warning("This has been spawned?");

        }
    }
    public class CompProperties_RepairArmor : CompProperties 
    {
        public CompProperties_RepairArmor() 
        {
            compClass = typeof(CompRepairArmor);
        }
    }
    public class RepairRecipe : RecipeDef
    {
        public RepairRecipe(List<ThingDef> connectedRecipes, string name)
        {
            string desc = "Repair_Armor_" + name;
            
            defName = desc;
            label = desc;
            jobString = desc;
            displayPriority = 0;
            workAmount = 0;
            workAmount = 1;
            workSkill = SkillDefOf.Crafting;
            workSkillLearnFactor = 1;
            useIngredientsForColor = false;
            researchPrerequisite = null;//we will make if you repair an armor peace without the research it instantly makes the armor terrible or poor
            factionPrerequisiteTags = null;
            fromIdeoBuildingPreceptOnly = false;
            description = desc;

            //unfinishedThingDef;
            //soundWorking;
            //effectWorking;
            IngredientCount iC = new IngredientCount();
            iC.SetBaseCount(1);
            iC.filter.SetAllowAllWhoCanMake(connectedRecipes[0]);
            ingredients.Add(iC);//okay this seems to be what is displayed for the ingredient count, so Ill put osmeithing like htis

            if (connectedRecipes != null) Verse.Log.Warning("This is not null");

            ThingFilter iF = new ThingFilter();
            FieldInfo iFCat = typeof(ThingFilter).GetField("categories", BindingFlags.NonPublic | BindingFlags.Instance);
            List<string> iFCategories = new List<string>();
            iFCategories.Add("Root");
            iFCat.SetValue(iF, iFCategories);
            if (connectedRecipes != null) Verse.Log.Warning("Past root assignment");
            FieldInfo IFAll = typeof(ThingFilter).GetField("allowedDefs", BindingFlags.NonPublic | BindingFlags.Instance);
            HashSet<ThingDef> iFAllowed = (HashSet<ThingDef>)IFAll.GetValue(iF);
            foreach(ThingDef t in connectedRecipes)
            {
                iFAllowed.Add(t);
            }
            foreach(ThingDef b in iF.AllowedThingDefs)
            {
                Verse.Log.Warning("Allowed: " + b);
            }
            if (connectedRecipes != null) Verse.Log.Warning("Past If creation");

            fixedIngredientFilter = iF;//this is the actual filter that the player can adjust what is allowed
            defaultIngredientFilter = iF;
            genderPrerequisite = Gender.Female;
            
        }
    }
}
