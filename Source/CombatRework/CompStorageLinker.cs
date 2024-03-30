using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimWorld
{

    public class CompStorageLinker : ThingComp
    {
        //the compthing I am basing this off is CompFlickable
        public bool Active = false;
        
        public List<Thing> connectedShelfs = new List<Thing>();

        public List<Zone> connectedZone = new List<Zone>();

        private CompProperties_StorageLinker Props => (CompProperties_StorageLinker)props;

        private Texture2D cachedCommandTex;//rimworld stores all its texture for icons like this for some reason

        private Texture2D cachedDelTex;

        private Texture2D CommandTex
        {
            get
            {
                if (cachedCommandTex == null)
                {
                    cachedCommandTex = ContentFinder<Texture2D>.Get(Props.commandTexture);
                }
                return cachedCommandTex;
            }
        }
        private Texture2D DelTex
        {
            get
            {
                if (cachedDelTex == null)
                {
                    cachedDelTex = ContentFinder<Texture2D>.Get(Props.gizmoDeleteTexture);
                }
                return cachedDelTex;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            //here we include data like how much storage is connected and maybe the id of the objects connected
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;//yield return is returning a array, first element yield returned will be first element in array and so on
            }
            if (parent.Faction == Faction.OfPlayer)
            {
                //create link

                Designator_Link linker = new Designator_Link(parent);
                
                Command_Action command_Action = new Command_Action();
                
                command_Action.icon = CommandTex;//insert function that caches in the way rimworld does
                command_Action.defaultLabel = Props.commandLabelKey.Translate();
                command_Action.defaultDesc = Props.commandDescKey.Translate();
                command_Action.activateSound = SoundDefOf.Click;
                
                command_Action.action = delegate
                {
                    Find.DesignatorManager.Select(linker);
                };
                yield return command_Action;

                //create deletion link

                Designator_Link delLink = new Designator_Link(parent, true);

                Command_Action command_DelAction = new Command_Action();
                
                command_DelAction.icon = DelTex;//insert function that caches in the way rimworld does
                command_DelAction.defaultLabel = Props.gizmoDeleteLabelKey.Translate();
                command_DelAction.defaultDesc = Props.gizmoDeleteDescKey.Translate();
                command_DelAction.activateSound = SoundDefOf.Click;
                
                command_DelAction.action = delegate
                {
                    Find.DesignatorManager.Select(delLink);
                };
                yield return command_DelAction;
            }
        }
        public override void PostDrawExtraSelectionOverlays()
        {
            foreach(Thing t in connectedShelfs)
            {
                GenDraw.DrawLineBetween(parent.TrueCenter(), t.TrueCenter());
            }
            foreach(Zone z in connectedZone)
            {
                GenDraw.DrawLineBetween(parent.TrueCenter(), new UnityEngine.Vector3 { x = z.Position.x, y = z.Position.y, z = z.Position.z });
            }
        }
    }
    public class CompProperties_StorageLinker : CompProperties
    {
        //the compproperties I am basing this off is compproperties_flickable
        public int maxconnectedCount = 10;

        [NoTranslate]
        public string commandTexture = "Mineer/Linkester";

        [NoTranslate]
        public string commandLabelKey = "CommandSetLinkedStorageLabel";

        [NoTranslate]
        public string commandDescKey = "CommandSetLinkedStorageDesc";

        [NoTranslate]
        public string gizmoDeleteTexture = "Mineer/Linkester";

        [NoTranslate] 
        public string gizmoDeleteLabelKey = "CommandRemoveLinkedStorageLabel";

        [NoTranslate]
        public string gizmoDeleteDescKey = "CommandSetLinkedStorageDesc";

        public CompProperties_StorageLinker()
        {
            compClass = typeof(CompStorageLinker);
        }

    }
    public class CompStorageLink : ThingComp
    {
        //the compthing I am basing this off is CompFlickable
        public bool Active = false;

        public List<Thing> connectedMasters = new List<Thing>();

        private CompProperties_StorageLink Props => (CompProperties_StorageLink)props;

        private Texture2D cachedCommandTex;//rimworld stores all its texture for icons like this for some reason


        public override void PostExposeData()
        {
            base.PostExposeData();
            //here we include data like how much storage is connected and maybe the id of the objects connected
        }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            foreach (Thing t in connectedMasters) 
            {
                t.TryGetComp<CompStorageLinker>().connectedShelfs.Remove(this.parent);
            }
        }
    }
    public class CompProperties_StorageLink : CompProperties
    {
        public int maxconnectedCount = 10;

        public CompProperties_StorageLink()
        {
            compClass = typeof(CompStorageLink);
        }
    }
}

