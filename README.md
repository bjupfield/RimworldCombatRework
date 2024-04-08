# RimWorldCombatRework
## HEY PATCHERS LOOK HERE

For those of you wanting to create your own patches on weapon mods or defs the information is located below.

### The Mod Template for Adjusting and Adding Armor Damage is located in the folder MOD TEMPLATE.

The two files you need to adjust to make your patch are the Lucid_Damages_Def.xml in Folder Common\Defs\Lucid_Damages_Def, and the About.xml in Folder About.

To make the Lucid_Damages_Def look inside the file. As you might notice there is a template for Lucids Damages.
It should Look something like this:

	<Lucids_Damage>
		<defName>Gun_[gun name no spaces]</defName>
		<armorDamage>0</armorDamage>
		<shieldDamage>0</shieldDamage>
		<baseDamage>0</baseDamage>
		<armorPen>0</armorPen>
    <accClose>0</accClose>
    <accShort>0</accShort>
    <accMed>0</accMed>
    <accLong>0</accLong>
	</Lucids_Damage>

To target a weapon type replace the defName with the name of the weapon you want to adjust. 
Rimworld naming convention for guns is "Gun_[gun name no spaces]" as you can see with the Gun_AssaultRifle.
All values below are the values you can adjust. Armor Damage is Armor Damage. It dmaages a total of that many hitpoints.
Shield and Base are the same. Armor Penetration and the Accuracies are all floating-point values and represent the percentages of
thos values. If you don't want to adjust any of these values delete these lines, such as if I only wanted to adjust the armor damage I would do:

	<Lucids_Damage>
		<defName>Gun_AssaultRifle</defName>
		<armorDamage>500000</armorDamage>
	</Lucids_Damage>

 The Second File that needs to be adjusted template is the mod description folder. I have provided a Template were the four things in the .xml file
 you need to change are the mod name, author name, package Id, and description. All these things are pretty self explanatory, for the package ID use the
 RimWorld convention of AuthorName.ModName. More information available at [url]https://rimworldwiki.com/wiki/Modding_Tutorials/About.xml
