using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CatharsisMod.Content.Items.Accessories
{
	public class DemonTrophy : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 54;
			Item.height = 58;
			Item.accessory = true;
			Item.rare = ItemRarityID.Orange;
		}
		
		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			player.GetDamage<GenericDamageClass>() += 0.12f;
		}
	}
}