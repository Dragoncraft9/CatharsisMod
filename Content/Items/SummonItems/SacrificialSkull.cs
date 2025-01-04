using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using CalamityMod.Items.Placeables;

namespace CatharsisMod.Content.Items.SummonItems
{
    public class SacrificialSkull : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Summoning";
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 24;
            Item.rare = ItemRarityID.Orange;
            Item.maxStack = 99;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
            AddIngredient(ModContent.ItemType<ScorchedBone>(), 16).
            AddTile(TileID.WorkBenches).
            Register();
        }
    }
}
