// Assembly-CSharp, Version=1.4.8706.7168, Culture=neutral, PublicKeyToken=null
// Verse.ArmorUtility
using RimWorld;
using UnityEngine;

private static void ApplyArmor(ref float damAmount, float armorPenetration, float armorRating, Thing armorThing, ref DamageDef damageDef, Pawn pawn, out bool metalArmor)
{
	if (armorThing != null)
	{
		metalArmor = armorThing.def.apparel.useDeflectMetalEffect || (armorThing.Stuff != null && armorThing.Stuff.IsMetal);
	}
	else
	{
		metalArmor = pawn.RaceProps.IsMechanoid;
	}
	if (armorThing != null)
	{
		float f = damAmount * 0.25f;
		armorThing.TakeDamage(new DamageInfo(damageDef, GenMath.RoundRandom(f)));
	}
	float num = Mathf.Max(armorRating - armorPenetration, 0f);
	float value = Rand.Value;
	float num2 = num * 0.5f;
	float num3 = num;
	if (value < num2)
	{
		damAmount = 0f;
	}
	else if (value < num3)
	{
		damAmount = GenMath.RoundRandom(damAmount / 2f);
		if (damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp)
		{
			damageDef = DamageDefOf.Blunt;
		}
	}
}
