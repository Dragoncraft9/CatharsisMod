using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CatharsisMod.Content.Items.LoreItems
{
    public class LoreBloodwalker : ModItem
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
			{
				 ItemID.Sets.ItemNoGravity[Type] = true;
			}
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.rare = ItemRarityID.LightRed;
            Item.consumable = false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.BloodMoonStarter).
                AddIngredient(ItemID.SoulofNight, 3).
                AddTile(TileID.Bookcases).
                Register();
        }
    }
}
