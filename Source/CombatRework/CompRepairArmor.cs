using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
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
            connected = parent.GetComp<CompStorageLinker>();

        }
        public override void CompTickRare()
        {//this is called every rare tick
            foreach(billManager b in billManagerList)
            {
                b.rareTick(connected);
            }
            base.CompTickRare();//just in case some dumbass puts some transpiler code in here
        }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {//this is called when the table is destroyed
            foreach(billManager b in billManagerList)
            {
                remove(b.connectedBill, false);
            }
            billManagerList.Clear();
            base.PostDestroy(mode, previousMap);
        }
        public void addBill(Bill_Production bill)
        {//this will be called when a bill is added
            billManagerList.Add(new billManager(bill, this));
        }
        public void removeManagedBill(Hidden_Bill bill)
        {
            billManager deleteManaged = billManagerList.Find(t =>
            {
                return t.managedBill == bill;
            });
            deleteManaged.managedBill = null;
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
            billManager remove = billManagerList.Find(t =>
            {
                return t.managedBill == bill;
            });
            if (remove != null)
            {
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

        private float bill_multiplier = 8.0f;

        public billManager(Bill_Production connectedBill, CompRepairArmor comp)
        {
            this.connectedBill = connectedBill;
            this.connectedComp = comp;
        }

        public bool rareTick(CompStorageLinker connected)
        {
            if(managedBill == null)
            {
                List<ThingDef> allowedThings = connectedBill.ingredientFilter.AllowedThingDefs.ToList().ListFullCopy();
                Thing closestThing = null;
                List<IngredientCount> calcCost = new List<IngredientCount>();
                foreach(Thing t in connected.connectedShelfs)
                {
                    Building_Storage shelf = (Building_Storage)t;
                    shelf.AllSlotCellsList().ForEach(c =>
                    {
                        connected.parent.Map.thingGrid.ThingsListAt(c).ForEach(d =>
                        {
                            if (allowedThings.Contains(d.def))
                            {
                                float percent = (d.MaxHitPoints - d.HitPoints) / d.MaxHitPoints;
                                List<IngredientCount> posCost = new List<IngredientCount>();
                                craftCost(d.CostListAdjusted(), ref posCost, percent);
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
                            float percent = (c.MaxHitPoints - c.HitPoints) / c.MaxHitPoints;
                            List<IngredientCount> posCost = new List<IngredientCount>();
                            craftCost(c.CostListAdjusted(), ref posCost, percent);
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
                        if (b.TryGetComp<CompQuality>() != null)
                        {
                            quality = closestThing.TryGetComp<CompQuality>().Quality;
                        }
                        Hidden_Bill myHiddenBill = new Hidden_Bill(new HiddenRecipe(b, calcCost, bill_multiplier, quality), b, null);
                        Building_WorkTable table = (Building_WorkTable)connectedComp.parent;

                        b.AllComps.Add(new CompSelectedRepair(connectedComp, this, false));

                        managedBill = myHiddenBill;
                        table.billStack.AddBill(myHiddenBill);
                    }
                }


                //Building_WorkTable b = (Building_WorkTable)connected.parent;
                //create weird bill
                //b.billStack.AddBill();
            }
            else
            {
                Apparel c = (Apparel)managedBill.piece;
                if (c == null)
                {
                    deleteManagedBill();
                }
                else
                {
                    if(c.Map == null ||c.Map.uniqueID != connectedComp.parent.Map.uniqueID)
                    {
                        deleteManagedBill();
                        return true;
                    }
                    Pawn d = c.Wearer;
                    if(d == null)
                    {
                    }
                    else
                    {
                        deleteManagedBill();
                    }
                }
            }
            return true;
        }
        public void delete()
        {
            //look I dont know how to delete objects in c#, this it seems that it isnt done manually, so i hope this works
            
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
        private void craftCost(List<ThingDefCountClass> thing, ref List<IngredientCount> cost, float count = 1)
        {
            //get component recipes
            List<RecipeDef> baseRecipes = DefDatabase<RecipeDef>.AllDefs.Where<RecipeDef>(r =>
            {
                if (r.defName.Contains("Component")) return true;
                return false;
            }).ToList();

            foreach(ThingDefCountClass t in thing)
            {
                float myCount = t.ToIngredientCount().GetBaseCount() * count;

                RecipeDef compRecipe = baseRecipes.Find(b =>
                {
                    return b.products.Where(c =>
                    {
                        return c.thingDef.defName == t.thingDef.defName;
                    }).Any();
                });
                if(compRecipe != null)
                {
                    //if thing is comp
                    List<ThingDefCountClass> compIngredients = new List<ThingDefCountClass>();
                    compRecipe.ingredients.ForEach(i =>
                    {
                        ThingDefCountClass thingDef = new ThingDefCountClass(i.filter.AllowedThingDefs.ToList()[0], (int)i.GetBaseCount());
                        compIngredients.Add(thingDef);
                    });

                    craftCost(compIngredients, ref cost, (int)myCount);
                }
                else
                {
                    //not comp just add ingredient
                    myCount /= bill_multiplier;
                    IngredientCount alreadyAdded = cost.Find(c =>
                    {
                        return c.filter.AllowedThingDefs.ToList()[0] == t.thingDef;
                    });
                    if (alreadyAdded != null)
                    {
                        if (t.thingDef.smallVolume && myCount < .1) myCount = 0;
                        else if (myCount < 1) myCount = 0;
                        alreadyAdded.SetBaseCount(alreadyAdded.GetBaseCount() + myCount);
                    }
                    else
                    {
                        if (myCount < 1)
                        {
                            myCount = 1;
                        }
                        if (t.thingDef.smallVolume) myCount *= 0.1f;
                        else myCount = (int)myCount;
                        IngredientCount newIngredient = new IngredientCount();
                        ThingFilter filter = new ThingFilter();
                        newIngredient.filter.SetAllow(t.thingDef, true);
                        newIngredient.SetBaseCount(myCount);
                        cost.Add(newIngredient);//rounds down, why not
                    }
                }
            }
        }
        private bool canCraft(List<IngredientCount> cost)
        {
            //yes I am checking the entire map, deal with it, its not going to be called constantly and holding a work inside this class seems less safe
            List<Thing> possibleThings = connectedBill.Map.listerThings.AllThings.ToList().Where(t =>
            {
                return cost.Where(c =>
                {
                    return c.filter.AllowedThingDefs.ToList()[0] == t.def;
                }).Any() && (connectedBill.ingredientSearchRadius * connectedBill.ingredientSearchRadius > (t.Position - connectedComp.parent.Position).LengthHorizontalSquared);
            }).ToList();//stole this logic from TryFindBestIngredientsHelper, just checks map to find if there are ingredients within the search range and in our ingredient list
            foreach(IngredientCount t in cost)
            {
                int count = 0;
                int index = 0;
                while(count < cost.Count && index < possibleThings.Count)
                {
                    if (possibleThings[index].def == t.filter.AllowedThingDefs.ToList()[0])
                    count += possibleThings[index].stackCount;
                    index++;
                }
                if (count < t.GetBaseCount())
                {
                    return false;
                }

            }
            return true;
        }
        public void deleteManagedBill()
        {
            Building_WorkTable b = (Building_WorkTable)connectedComp.parent;
            b.billStack.Bills.Remove(managedBill);
            
            DefDatabase<RecipeDef>.AllDefs.ToList().Remove(managedBill.recipe);

            ThingWithComps t = (ThingWithComps)((HiddenRecipe)managedBill.recipe).piece;
            if(t != null)
            {
                t.TryGetComp<CompSelectedRepair>().delete();
                t.AllComps.Remove(t.TryGetComp<CompSelectedRepair>());
            }

            managedBill = null;
        }
    }
    class HiddenRecipe : RecipeDef
    {
        public QualityCategory repairQuality = (QualityCategory)7;

        public Thing piece;

        public Color ogColor;
        public HiddenRecipe(Thing thing, List<IngredientCount> cost, float bill_multiplier, QualityCategory repairQuality = (QualityCategory)7)
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

            //foreach (ThingDefCountClass c in cost)
            //{
            //    IngredientCount id = new IngredientCount();
            //    id.SetBaseCount(c.count);
            //    //id.filter.SetAllowAllWhoCanMake(c.thingDef);
            //    id.filter.SetAllow(c.thingDef, allow: true);
            //    ingredients.Add(id);
            //}
            ingredients = cost;

            ThingFilter iF = new ThingFilter();
            FieldInfo iFCat = typeof(ThingFilter).GetField("categories", BindingFlags.NonPublic | BindingFlags.Instance);
            List<string> iFCategories = new List<string>();
            iFCategories.Add("Root");
            iFCat.SetValue(iF, iFCategories);
            FieldInfo IFAll = typeof(ThingFilter).GetField("allowedDefs", BindingFlags.NonPublic | BindingFlags.Instance);
            HashSet<ThingDef> iFAllowed = (HashSet<ThingDef>)IFAll.GetValue(iF);
            foreach (IngredientCount t in cost)
            {
                iFAllowed.Add(t.filter.AllowedThingDefs.ToList()[0]);
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
            CompColorable posComp = thing.TryGetComp<CompColorable>();
            if (posComp != null)
            {
                ogColor = posComp.Color;
            }
        }
    }
    public class Hidden_Bill : Bill_ProductionWithUft 
    {
        protected override bool CanCopy => false;

        public ThingWithComps piece;

        public Hidden_Bill(RecipeDef r, ThingWithComps p, Precept_ThingStyle b = null) : base(r, b)
        { 
            piece = p;
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
        public override void Notify_DoBillStarted(Pawn billDoer)
        {
            base.Notify_DoBillStarted(billDoer);
            piece.GetComp<CompSelectedRepair>().uftConversion = true;
        }
        public void flip()
        {
            CompSelectedRepair b = piece.GetComp<CompSelectedRepair>();
            if (b != null)
            b.uftConversion = false;
        }
        
    }
    public class CompSelectedRepair : ThingComp 
    {
        private CompRepairArmor linkedRepairer;

        private billManager linkedManager;

        public bool uftConversion = false;

        public CompSelectedRepair(CompRepairArmor linkedRepairer, billManager linkedManager, bool uftConversion = false)
        {
            this.linkedRepairer = linkedRepairer;
            this.uftConversion = uftConversion;
            this.linkedManager = linkedManager;
        }

        public void delete()
        {
            linkedRepairer = null;
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);

            Verse.Log.Warning("DeSpawnCalled");

            Apparel a = (Apparel)parent;

            if (a != null)
            {
                Pawn t = a.Wearer;

            }

            CompEquippable b = parent.TryGetComp<CompEquippable>();
            if(b != null)
            {
                Type type = b.GetType();
                FieldInfo pawnInfo = type.GetField("Holder", BindingFlags.NonPublic | BindingFlags.Instance);
                Pawn pawn = (Pawn)pawnInfo.GetValue(b);
            }
            if (!uftConversion)
            {
                linkedManager.deleteManagedBill();
                //this.parent.AllComps.Remove(this);
            }
            else
            {
            }

        }
    }

}
