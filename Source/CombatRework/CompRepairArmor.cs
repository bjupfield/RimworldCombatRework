using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld
{
    public class CompRepairArmor : ThingComp
    {
        List<billManager> billManagerList = new List<billManager>();
        CompStorageLinker connected;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            Verse.Log.Warning("This has been spawned?");
            connected = parent.GetComp<CompStorageLinker>();

        }
        public override void CompTickRare()
        {//this is called every rare tick
            Verse.Log.Warning("RareTick occur?");
            foreach(billManager b in billManagerList)
            {
                b.rareTick(connected);
            }
            base.CompTickRare();//just in case some dumbass puts some transpiler code in here
        }
        public override void CompTick()
        {//this is called every rare tick
            Verse.Log.Warning("RareTick occur?");
            foreach (billManager b in billManagerList)
            {
                b.rareTick(connected);
            }
            base.CompTickRare();//just in case some dumbass puts some transpiler code in here
        }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {//this is called when the table is destroyed
            Verse.Log.Warning("hey || Count: " + billManagerList.Count);
            foreach(billManager b in billManagerList)
            {
                remove(b.connectedBill, false);
            }
            billManagerList.Clear();
            Verse.Log.Warning("Post hey");
            base.PostDestroy(mode, previousMap);
        }
        public void addBill(Bill_Production bill)
        {//this will be called when a bill is added
            billManagerList.Add(new billManager(bill, this));
        }
        public void remove(Bill_Production bill, bool which = true)
        {//will be called when a bill is remvoed
            billManager remove = billManagerList.Find(t =>
            {
                return t.connectedBill == bill;
            });
            if(remove != null)
            {
                remove.delete();
                if(which)billManagerList.Remove(remove);
            }
        }
        public void deleteManaged(Bill_Production bill, bool which = true)
        {//will be called when an armor is repaired
            Verse.Log.Warning("Managed Occurs");
            billManager remove = billManagerList.Find(t =>
            {
                return t.managedBill == bill;
            });
            if (remove != null)
            {
                Verse.Log.Warning("Remove Found Bill");
                remove.managedCompleted();
            }
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

            //unfinishedThingDef; will need to add
            //soundWorking; will need to add
            //effectWorking; will need to add
            IngredientCount iC = new IngredientCount();
            iC.SetBaseCount(1);
            iC.filter.SetAllowAllWhoCanMake(connectedRecipes[0]);
            ingredients.Add(iC);//okay this seems to be what is displayed for the ingredient count, so Ill put osmeithing like htis


            ThingFilter iF = new ThingFilter();
            FieldInfo iFCat = typeof(ThingFilter).GetField("categories", BindingFlags.NonPublic | BindingFlags.Instance);
            List<string> iFCategories = new List<string>();
            iFCategories.Add("Root");
            iFCat.SetValue(iF, iFCategories);
            FieldInfo IFAll = typeof(ThingFilter).GetField("allowedDefs", BindingFlags.NonPublic | BindingFlags.Instance);
            HashSet<ThingDef> iFAllowed = (HashSet<ThingDef>)IFAll.GetValue(iF);
            foreach(ThingDef t in connectedRecipes)
            {
                iFAllowed.Add(t);
            }

            fixedIngredientFilter = iF;//this is the actual filter that the player can adjust what is allowed
            defaultIngredientFilter = iF;//this is what the player is allowed to fix
            genderPrerequisite = Gender.Female;//using this to not allow the player to pick do untill
            
        }
    }
    public class billManager
    {
        public Bill_Production connectedBill;
        
        private CompRepairArmor connectedComp;

        public Hidden_Bill managedBill;

        private float bill_multiplier = 20.0f;

        public billManager(Bill_Production connectedBill, CompRepairArmor comp)
        {
            Verse.Log.Warning("bill manager created");
            this.connectedBill = connectedBill;
            this.connectedComp = comp;
        }

        public bool rareTick(CompStorageLinker connected)
        {
            if(managedBill == null)
            {
                List<ThingDef> allowedThings = connectedBill.ingredientFilter.AllowedThingDefs.ToList().ListFullCopy();
                Thing closestThing = null;
                List<ThingDefCountClass> calcCost = new List<ThingDefCountClass>();
                foreach(Thing t in connected.connectedShelfs)
                {
                    Building_Storage shelf = (Building_Storage)t;
                    shelf.AllSlotCellsList().ForEach(c =>
                    {
                        connected.parent.Map.thingGrid.ThingsListAt(c).ForEach(d =>
                        {
                            if (allowedThings.Contains(d.def))
                            {
                                List<ThingDefCountClass> posCost = craftCost(d);
                                if (canCraft(posCost))
                                {
                                    //will need to see if the thing can be made before we set it as closest thing
                                    if (closestThing != null)
                                    {
                                        IntVec2 close = closestThing.Position.ToIntVec2;
                                        IntVec2 table = connected.parent.Position.ToIntVec2;
                                        IntVec2 newClose = c.ToIntVec2;
                                        if (Mathf.Abs(close.x - table.x) + Mathf.Abs(close.z + table.z) > Mathf.Abs(newClose.x - table.x) + Mathf.Abs(newClose.z + table.z))
                                        {
                                            closestThing = d;
                                            calcCost = posCost;
                                        }
                                    }
                                    else
                                    {
                                        closestThing = d;
                                        calcCost = posCost;
                                    }
                                }
                            }
                        });
                    });
                }
                foreach(Zone z in connected.connectedZone)
                {
                    z.AllContainedThings.ToList().ForEach(c =>
                    {
                        if (allowedThings.Contains(c.def))
                        {
                            //will need to see if the thing can be made before we set it as closest thing
                            List<ThingDefCountClass> posCost = craftCost(c);
                            if (canCraft(posCost)) 
                            { 
                                if (closestThing != null)
                                {
                                    IntVec2 close = closestThing.Position.ToIntVec2;
                                    IntVec2 table = connected.parent.Position.ToIntVec2;
                                    IntVec2 newClose = c.Position.ToIntVec2;
                                    if (Mathf.Abs(close.x - table.x) + Mathf.Abs(close.z + table.z) > Mathf.Abs(newClose.x - table.x) + Mathf.Abs(newClose.z + table.z))
                                    {
                                        closestThing = c;
                                        calcCost = posCost;
                                    }
                                }
                                else
                                {
                                    closestThing = c;
                                    calcCost = posCost;
                                }
                            }
                        }
                    });
                }
                if (closestThing == null)
                {
                    //skip did not find a usable item
                }
                else
                {
                    //create and add custom bill
                    ThingWithComps b = closestThing as ThingWithComps;
                    if (b != null)
                    {
                        QualityCategory quality = (QualityCategory)7;
                        if (closestThing.TryGetComp<CompQuality>() != null)
                        {
                            quality = closestThing.TryGetComp<CompQuality>().Quality;
                        }
                        Hidden_Bill myHiddenBill = new Hidden_Bill(new HiddenRecipe(closestThing, calcCost, bill_multiplier, quality), null);
                        Building_WorkTable table = (Building_WorkTable)connectedComp.parent;

                        b.AllComps.Add(new CompSelectedRepair(connectedComp, myHiddenBill, false));

                        managedBill = myHiddenBill;
                        table.billStack.AddBill(myHiddenBill);
                    }
                }


                //Building_WorkTable b = (Building_WorkTable)connected.parent;
                //create weird bill
                //b.billStack.AddBill();
            }
            Verse.Log.Warning("Inside Rare Tick");
            return true;
        }
        public void delete()
        {
            //look I dont know how to delete objects in c#, this it seems that it isnt done manually, so i hope this works
            Verse.Log.Warning("Bill deleted");
            
                Building_WorkTable b = (Building_WorkTable)connectedComp.parent;
                if (managedBill != null)
                {
                    b.billStack.Delete(managedBill);
                    deleteManagedBill();
                }
                connectedBill = null;
                connectedComp = null;
        }
        public void managedCompleted()
        {
            Building_WorkTable b = (Building_WorkTable)connectedComp.parent;
            b.billStack.Delete(managedBill);
            deleteManagedBill();
        }
        private List<ThingDefCountClass> craftCost(Thing thing)
        {
            List<ThingDefCountClass> cost = thing.CostListAdjusted();
            List<ThingDefCountClass> myCost = new List<ThingDefCountClass>();
            foreach(ThingDefCountClass t in cost)
            {
                float myCount = t.count;
                myCount /= bill_multiplier;
                if(myCount < 1)
                {
                    //use rand if cost for item is below one to determine if this time it will need one, so we don't need to
                    //Verse.Rand.Value;
                    myCount = Rand.Value < myCount ? 1 : 0; 
                }
                if(myCount > 0)
                myCost.Add(new ThingDefCountClass(t.thingDef, (int)myCount));//rounds down, why not
            }
            return myCost;
        }
        private bool canCraft(List<ThingDefCountClass> cost)
        {
            Verse.Log.Warning("canCraft");
            //yes I am checking the entire map, deal with it, its not going to be called constantly and holding a work inside this class seems less safe
            List<Thing> possibleThings = connectedBill.Map.listerThings.AllThings.ToList().Where(t =>
            {
                return cost.Where(c =>
                {
                    return c.thingDef == t.def;
                }).Any() && (connectedBill.ingredientSearchRadius * connectedBill.ingredientSearchRadius > (t.Position - connectedComp.parent.Position).LengthHorizontalSquared);
            }).ToList();//stole this logic from TryFindBestIngredientsHelper, just checks map to find if there are ingredients within the search range and in our ingredient list
            foreach(ThingDefCount t in cost)
            {
                int count = 0;
                int index = 0;
                while(count < cost.Count && index < possibleThings.Count)
                {
                    if (possibleThings[index].def == t.ThingDef)
                    count += possibleThings[index].stackCount;
                    index++;
                }
                if (count < t.Count)
                {
                    Verse.Log.Warning("Not enough ingredients needed: " + t.Count + "x " + t.ThingDef.defName + " recieved " + count + "x");
                    return false;
                }

            }
            return true;
        }
        private void deleteManagedBill()
        {
            DefDatabase<RecipeDef>.AllDefs.ToList().Remove(managedBill.recipe);
            ThingWithComps t = (ThingWithComps)((HiddenRecipe)managedBill.recipe).piece;
            t.TryGetComp<CompSelectedRepair>().delete();
            t.AllComps.Remove(t.TryGetComp<CompSelectedRepair>());
            managedBill = null;
        }
    }
    class HiddenRecipe : RecipeDef
    {
        public QualityCategory repairQuality = (QualityCategory)7;

        public Thing piece;
        public HiddenRecipe(Thing thing, List<ThingDefCountClass> cost, float bill_multiplier, QualityCategory repairQuality = (QualityCategory)7)
        {
            string desc = thing.def.defName;

            defName = "Repair " + desc;
            label = "Repair " + desc;
            jobString = "Repairing " + desc;
            displayPriority = 0;
            workAmount = 0;
            workAmount = 1;
            workSkill = SkillDefOf.Crafting;
            workSkillLearnFactor = 1;
            skillRequirements = thing.def.recipeMaker.skillRequirements;
            useIngredientsForColor = true;
            researchPrerequisite = null;//we will make if you repair an armor peace without the research it instantly makes the armor terrible or poor
            factionPrerequisiteTags = null;
            fromIdeoBuildingPreceptOnly = false;
            description = desc;
            workTableSpeedStat = StatDefOf.WorkTableWorkSpeedFactor;
            workTableEfficiencyStat = StatDefOf.WorkTableEfficiencyFactor;
            workAmount = thing.GetStatValue(StatDefOf.WorkToMake) / bill_multiplier;


            //unfinishedThingDef; will need to add
            //soundWorking; will need to add
            //effectWorking; will need to add
            //attempting to make armor piece filtered by isusableingredient
            //IngredientCount iC = new IngredientCount();
            //iC.SetBaseCount(1);
            //iC.filter.SetAllow(thing.def, allow: true);
            //ingredients.Add(iC);

            foreach (ThingDefCountClass c in cost)
            {
                IngredientCount id = new IngredientCount();
                id.SetBaseCount(c.count);
                //id.filter.SetAllowAllWhoCanMake(c.thingDef);
                id.filter.SetAllow(c.thingDef, allow: true);
                ingredients.Add(id);
            }

            ThingFilter iF = new ThingFilter();
            FieldInfo iFCat = typeof(ThingFilter).GetField("categories", BindingFlags.NonPublic | BindingFlags.Instance);
            List<string> iFCategories = new List<string>();
            iFCategories.Add("Root");
            iFCat.SetValue(iF, iFCategories);
            FieldInfo IFAll = typeof(ThingFilter).GetField("allowedDefs", BindingFlags.NonPublic | BindingFlags.Instance);
            HashSet<ThingDef> iFAllowed = (HashSet<ThingDef>)IFAll.GetValue(iF);
            foreach (ThingDefCountClass t in cost)
            {
                iFAllowed.Add(t.thingDef);
            }
            iFAllowed.Add(thing.def);
            //unfinishedThingDef = ThingDefOf.AncientBed;

            fixedIngredientFilter = iF;//this is the actual filter that the player can adjust what is allowed
            defaultIngredientFilter = iF;//this is what the player is allowed to fix
            genderPrerequisite = Gender.Male;//using this to not allow the player to pick any craft amount type, and generally just make the bill non-intereactable

            unfinishedThingDef = thing.def.recipeMaker.unfinishedThingDef;

            products.Add(new ThingDefCountClass(thing.def, 1));
            this.repairQuality = repairQuality;
            this.piece = thing;
        }
    }
    public class Hidden_Bill : Bill_ProductionWithUft 
    {
        protected override bool CanCopy => false;

        public Hidden_Bill(RecipeDef r, Precept_ThingStyle b = null) : base(r, b)
        { 
            
        }
        protected override void DoConfigInterface(Rect baseRect, Color baseColor)
        {
            Rect rect = new Rect(28f, 32f, 100f, 30f);
            GUI.color = new Color(1f, 1f, 1f, 0.65f);
            Widgets.Label(rect, RepeatInfoText);
            GUI.color = baseColor;
            WidgetRow widgetRow = new WidgetRow(baseRect.xMax, baseRect.y + 29f, UIDirection.LeftThenUp);
            if (widgetRow.ButtonText("Details".Translate() + "..."))
            {
                Find.WindowStack.Add(GetBillDialog());
            }
        }
        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            base.Notify_IterationCompleted(billDoer, ingredients);
            Building_WorkTable b =  (Building_WorkTable)this.billStack.billGiver;
            b.GetComp<CompRepairArmor>().deleteManaged(this);
        }
    }
    public class CompSelectedRepair : ThingComp 
    {
        private CompRepairArmor linkedRepairer;

        private Bill_Production linkedBill;

        public bool uftConversion = false;

        public CompSelectedRepair(CompRepairArmor linkedRepairer, Bill_Production linkedBill, bool uftConversion = false)
        {
            this.linkedRepairer = linkedRepairer;
            this.uftConversion = uftConversion;
            this.linkedBill = linkedBill;
        }

        public void delete()
        {
            linkedRepairer = null;
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            if (!uftConversion)
            {
                linkedRepairer.remove(linkedBill);
            }
            this.parent.AllComps.Remove(this);
        }
    }

}
