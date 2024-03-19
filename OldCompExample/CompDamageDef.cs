using CombatRework;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorld
{
    public class CompDamageDef : ThingComp
    {
        private int shieldDamage = 0;
        private int armorDamage = 0;

        public int get_shieldDamage(float qualityMultiplier = 1)
        {
            return Mathf.RoundToInt((float)this.shieldDamage * qualityMultiplier);
        }
        public int get_armorDamage(float qualityMultiplier = 1) 
        {
            return Mathf.RoundToInt((float)this.armorDamage * qualityMultiplier);
        }
        override public void Initialize(CompProperties props)
        {
            Shield_Armor_Damage myDamages = DamageDefAdjustManager.pullDamageDef(this.parent.def.defName);
            if (myDamages != null)
            {
                Verse.Log.Warning(this.parent.def.defName + "'s Comp Fired");
                shieldDamage = myDamages.shieldDamage;
                armorDamage = myDamages.armorDamage;
                CompQuality myQuality = this.parent.GetComp<CompQuality>();
                if (myQuality != null)
                {
                    Verse.Log.Warning(this.parent.def.defName + " Quality is: " + myQuality.Quality.ToString());
                }
                else
                {
                    Verse.Log.Warning("Number of comps: " + this.parent.AllComps.Count.ToString());
                    foreach(var myComp in this.parent.AllComps)
                    {
                        Verse.Log.Warning(myComp.GetType().ToString());
                    }
                    Verse.Log.Warning("The weapon Damage multiplier: " + this.parent.GetStatValue(StatDefOf.RangedWeapon_DamageMultiplier).ToString());
                }
            }
        }
    }
}
