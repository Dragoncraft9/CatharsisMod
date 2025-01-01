using System;
using System.Collections.Generic;
using CalamityMod.Schematics;
using Terraria.WorldBuilding;
using static CalamityMod.Schematics.SchematicManager;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;
using CalamityMod;
using CatharsisMod.Content.Items.Accessories;
using CalamityMod.Items.Materials;
using CatharsisMod.Common.Systems;
using CalamityMod.Tiles.Crags;

namespace CatharsisMod.Content.World
{
    internal struct ChestItem
    {
        internal int Type;

        internal int Stack;

        internal ChestItem(int type, int stack)
        {
            Type = type;
            Stack = stack;
        }
    }
    public static class UnderworldShrine
    {
        public static bool ShouldAvoidLocation(Point placementPoint)
        {
            Tile tile = CalamityUtils.ParanoidTileRetrieval(placementPoint.X, placementPoint.Y);
            if (tile.LiquidType == LiquidID.Lava)
                return false;
            if (tile.TileType == TileID.ObsidianBrick || tile.TileType == TileID.Hellstone || tile.TileType == ModContent.TileType<ScorchedBone>() || tile.TileType == ModContent.TileType<BrimstoneSlab>())
                return true;
            return false;
        }
        public static void PlaceUnderworldShrine(StructureMap structures)
        {
            string mapKey = "Underworld Shrine";
            SchematicMetaTile[,] schematic = CatharsisSchematicManager.TileMaps[mapKey];
            Point placementPoint = Point.Zero;
            int tries = 0;
            do
            {
                int underworldTop = Main.UnderworldLayer;
                int placementPositionX = (Main.dungeonX < Main.maxTilesX / 2) ? WorldGen.genRand.Next((int)(Main.maxTilesX / 2 - Main.maxTilesX / 3f), (int)(Main.maxTilesX / 2 - Main.maxTilesX / 3.5f)) : WorldGen.genRand.Next((int)(Main.maxTilesX / 2 + Main.maxTilesX / 3.5f), (int)(Main.maxTilesX / 2 + Main.maxTilesX / 3f));
                int placementPositionY = WorldGen.genRand.Next(Main.maxTilesY - 128, Main.maxTilesY - 115);

                placementPoint = new(placementPositionX, placementPositionY);
                Vector2 schematicSize = new(schematic.GetLength(0), schematic.GetLength(1));
                bool canGenerateInLocation = true;

                for (int x = (int)(placementPoint.X - (schematicSize.X / 2)); x < placementPoint.X + (schematicSize.X / 2); x++)
                {
                    for (int y = placementPoint.Y; y < placementPoint.Y + schematicSize.Y; y++)
                    {
                        Tile tile = CalamityUtils.ParanoidTileRetrieval(x, y);
                        if (ShouldAvoidLocation(new Point(x, y)))
                            canGenerateInLocation = false;
                    }
                }
                if (!canGenerateInLocation || !structures.CanPlace(new Rectangle(placementPoint.X, placementPoint.Y, (int)schematicSize.X, (int)schematicSize.Y)))
                {
                    tries++;
                }
                else
                {

                    placementPoint = new(placementPositionX, placementPositionY + 5);
                    SchematicAnchor anchorType = SchematicAnchor.BottomCenter;

                    bool place = true;
                    PlaceSchematic(mapKey, placementPoint, anchorType, ref place, new Action<Chest, int, bool>(FillUnderworldChest));

                    Rectangle protectionArea = CalamityUtils.GetSchematicProtectionArea(schematic, placementPoint, anchorType);
                    CalamityUtils.AddProtectedStructure(protectionArea, 30);

                    break;
                }
            }
            while (tries <= 40000);
        }

        private static void FillUnderworldChest(Chest chest, int Type, bool place)
        {
            List<ChestItem> contents =
            [
                new ChestItem(ModContent.ItemType<DemonTrophy>(), 1),
                new ChestItem(ModContent.ItemType<DemonicBoneAsh>(), WorldGen.genRand.Next(12, 16)),
                new ChestItem(ItemID.DemonTorch, WorldGen.genRand.Next(100, 120)),
                new ChestItem(ItemID.GoldCoin, WorldGen.genRand.Next(20, 30)),
                new ChestItem(ItemID.HealingPotion, WorldGen.genRand.Next(10, 15)),
                new ChestItem(ItemID.WrathPotion, WorldGen.genRand.Next(10, 15)),
                new ChestItem(ItemID.RagePotion, WorldGen.genRand.Next(10, 15)),
                new ChestItem(ItemID.PotionOfReturn, WorldGen.genRand.Next(10, 15)),
            ];

            for (int i = 0; i < contents.Count; i++)
            {
                chest.item[i].SetDefaults(contents[i].Type);
                chest.item[i].stack = contents[i].Stack;
            }
        }
    }
}
