using RimWorld;
using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Verse
{
    public class Designator_Link : Designator_Cells
    {
        private bool delete = false;

        private Thing attachTo;
        public override int DraggableDimensions => 2;

        public Designator_Link(Thing t, bool lete = false) 
        {
            defaultLabel = "DesignatorLink".Translate();
            defaultDesc = "DesignatorClaimDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("UI/Designators/ForbidOff");
            useMouseIcon = true;
            hotKey = KeyBindingDefOf.Misc4;
            showReverseDesignatorDisabledReason = true;
            attachTo = t;
            delete = lete;
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            //basing off of Designator_Claim, checking if tile selected contains a strorage container return true if so return false if not
            if (!c.InBounds(base.Map))
            {
                return false;
            }
            if (c.Fogged(base.Map))
            {
                return false;
            }
            if (!(from t in c.GetThingList(base.Map)
                  where CanDesignateThing(t).Accepted
                  select t).Any())
            {
                if (delete)
                {
                    if(base.Map.zoneManager.ZoneAt(c)?.GetType() != typeof(Zone_Stockpile))
                        return "Message Must Be Storage and Assigned".Translate();
                    else if (attachTo.TryGetComp<CompStorageLinker>().connectedZone.Find(b => {
                        return b.ID == base.Map.zoneManager.ZoneAt(c)?.ID; }) == null)
                        return "Message Must Be Storage and Assigned".Translate();
                }
                else
                {
                    if (base.Map.zoneManager.ZoneAt(c)?.GetType() != typeof(Zone_Stockpile))
                        return "Message Must Be Storage and Not Assigned".Translate();
                    else if (attachTo.TryGetComp<CompStorageLinker>().connectedZone.Find(b => { return b.ID == base.Map.zoneManager.ZoneAt(c)?.ID; }) != null)
                        return "Message Must Be Storage and Not Assigned".Translate();
                }
            }

            return true;
        }
        public override void DesignateSingleCell(IntVec3 c)
        {
            List<Thing> thingList = c.GetThingList(base.Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                if (CanDesignateThing(thingList[i]).Accepted)
                {
                    DesignateThing(thingList[i]);
                    return;
                }
            }
            if (base.Map.zoneManager.ZoneAt(c) != null)
            {
                DesignateZone(base.Map.zoneManager.ZoneAt(c));
            }
        }
        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            //b.def.building.blueprintClass == typeof(Blueprint_Storage)
            if (t.Faction == Faction.OfPlayer && t.def.HasComp(typeof(CompStorageLink)))
            {
                Thing c = attachTo.TryGetComp<CompStorageLinker>().connectedShelfs.Find(b => { return b.ThingID == t.ThingID; });
                if (!delete && c == null)
                {
                    return true;
                }
                else if(delete && c != null)
                {
                    return true;
                }
            }
            return false;
        }
        public override void DesignateThing(Thing t)
        {
            if (attachTo == null || attachTo.def.comps.Find(b =>
            {
                return b.compClass == typeof(CompStorageLinker);
            }) == null)
            {
                Verse.Log.Error("Why Have You Created A Commandlinker not connected to a CompLinker");
                return;
            }
            if(attachTo.TryGetComp<CompStorageLinker>() != null)
            {
                if(delete)
                {
                    attachTo.TryGetComp<CompStorageLinker>().connectedShelfs.Remove(t);
                    t.TryGetComp<CompStorageLink>().connectedMasters.Remove(attachTo);
                    soundSucceeded = SoundDefOf.Designate_RemovePaint;
                }
                else
                {
                    attachTo.TryGetComp<CompStorageLinker>().connectedShelfs.Add(t);
                    t.TryGetComp<CompStorageLink>().connectedMasters.Add(attachTo);
                    soundSucceeded = SoundDefOf.TabOpen;
                }
                return;
            }
        }
        private void DesignateZone(Zone z)
        {
            if (attachTo == null || attachTo.def.comps.Find(b =>
            {
                return b.compClass == typeof(CompStorageLinker);
            }) == null)
            {
                Verse.Log.Error("Why Have You Created A Commandlinker not linked to a CompLinker");
                return;
            }
            if (attachTo.TryGetComp<CompStorageLinker>() != null)
            {
                if (delete)
                {
                    attachTo.TryGetComp<CompStorageLinker>().connectedZone.Remove(z);
                    soundSucceeded = SoundDefOf.Designate_RemovePaint;
                }
                else
                {
                    attachTo.TryGetComp<CompStorageLinker>().connectedZone.Add(z);
                    soundSucceeded = SoundDefOf.TabOpen;
                }
                return;
            }

        }

    }
}
