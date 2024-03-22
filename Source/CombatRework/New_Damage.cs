using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Verse
{
    public class Shield_Armor_Damage : Def
    {
        public int shieldDamage;
        public int armorDamage;
        public int baseDamage = 0;
        public float changeDamage;
    }
    public class Lucids_Damage : Def
    {
        public int shieldDamage = -1;
        public int armorDamage = -1;
        public int baseDamage = -1;
        public float armorPen = -1;
    }
}
