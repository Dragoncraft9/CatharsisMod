using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using CatharsisMod.Content.Tiles.Furniture;

namespace CatharsisMod.Content.Items.Placeables
{
    public class SacrificialAltar : ModItem, ILocalizedModType, IModType
    {
        public new string LocalizationCategory => "Items.Placeables";

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<SacrificialAltarTile>();
            Item.rare = ItemRarityID.Orange;
        }
    }
}
