using CalamityMod.Schematics;
using CalamityMod;
using CatharsisMod.Common.Systems;
using System;
using static CalamityMod.Schematics.SchematicManager;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using Terraria;
using Microsoft.Xna.Framework;

namespace CatharsisMod.Content.World
{
    public class NecroticCrypt
    {
        public static bool ShouldAvoidLocation(Point placementPoint)
        {
            Tile tile = CalamityUtils.ParanoidTileRetrieval(placementPoint.X, placementPoint.Y);
            if (tile.LiquidType == LiquidID.Shimmer)
                return false;
            if (tile.TileType == TileID.Sand)
                return true;
            return false;
        }
        public static void PlaceNecroticCrypt(StructureMap structures)
        {
            string mapKey = "Necrotic Crypt";
            SchematicMetaTile[,] schematic = CatharsisSchematicManager.TileMaps[mapKey];
            Point placementPoint = Point.Zero;
            SchematicAnchor anchor = (Main.dungeonX > Main.maxTilesX / 2) ? SchematicAnchor.TopLeft : SchematicAnchor.BottomRight;
            int tries = 0;
            do
            {
                int underworldTop = Main.UnderworldLayer;
                int placementPositionX = anchor == SchematicAnchor.TopLeft ? WorldGen.genRand.Next(0, (int)(Main.maxTilesX / 2 - Main.maxTilesX / 3f)) : WorldGen.genRand.Next((int)(Main.maxTilesX / 2 + Main.maxTilesX / 3f), Main.maxTilesX);
                
                int placementPositionY = WorldGen.genRand.Next((int)(Main.worldSurface + 200), underworldTop - 175);

                placementPoint = new(placementPositionX, placementPositionY);
                Vector2 schematicSize = new(schematic.GetLength(0), schematic.GetLength(1));
                bool canGenerateInLocation = true;

                for (int x = 0; x < schematicSize.X; x++)
                {
                    for (int y = 0; y < schematicSize.Y; y++)
                    {
                        Point p = Point.Zero;
                        if (anchor == SchematicAnchor.TopLeft)
                            p = new(x + placementPositionX, y + placementPositionY);
                        else
                            p = new(placementPositionX - x, placementPositionY - y);

                        Tile tile = CalamityUtils.ParanoidTileRetrieval(p.X, p.Y);
                        if (ShouldAvoidLocation(new Point(p.X, p.Y)))
                            canGenerateInLocation = false;
                    }
                }
                
                if (!canGenerateInLocation || !structures.CanPlace(new Rectangle(placementPoint.X, placementPoint.Y, (int)schematicSize.X, (int)schematicSize.Y)))
                {
                    tries++;
                }
                else
                {
                    placementPoint = new(placementPositionX, placementPositionY);

                    bool place = true;
                    PlaceSchematic<Action<Chest>>(mapKey, placementPoint, anchor, ref place);

                    Rectangle protectionArea = CalamityUtils.GetSchematicProtectionArea(schematic, placementPoint, anchor);
                    CalamityUtils.AddProtectedStructure(protectionArea, 30);

                    break;
                }
            }
            while (tries <= 20000);
        }
    }
}
