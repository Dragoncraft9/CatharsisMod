using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using CatharsisMod.Content.Items.Placeables;
using Terraria.DataStructures;
using Terraria.ObjectData;
using Terraria.Enums;
using Microsoft.Xna.Framework;
using CalamityMod.Dusts;
using CalamityMod.NPCs.TownNPCs;
using CalamityMod.Items.Materials;
using CatharsisMod.Content.NPCs.Bloodwalker;
using CalamityMod.NPCs.SlimeGod;
using Terraria.Audio;
using CatharsisMod.Content.Items.SummonItems;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace CatharsisMod.Content.Tiles.Furniture
{
    public class SacrificialAltarTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = false;
            TileID.Sets.DisableSmartCursor[Type] = true;
            TileID.Sets.AvoidedByNPCs[Type] = true;
            TileID.Sets.InteractibleByNPCs[Type] = true;

            RegisterItemDrop(ModContent.ItemType<SacrificialAltar>());

            TileObjectData.newTile.CopyFrom(TileObjectData.Style5x4); // Uses 5x4 style, but reduces height to 2.
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.Width = 4;
            TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 16];
            TileObjectData.newTile.Origin = new Point16(1, 2);

            TileObjectData.addTile(Type);

            AddMapEntry(Color.DarkRed);

            AnimationFrameHeight = 72;

            DustType = ModContent.DustType<BrimstoneFlame>();
        }

        int frameHeight = 72;

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            int Frame = frameY / 18 / 4;
            
            if (Frame != 0 && Main.netMode != NetmodeID.MultiplayerClient)
                Item.NewItem(Item.GetSource_NaturalSpawn(), new Point(i, j).ToWorldCoordinates(0, 0) + Vector2.One * 32, Vector2.Zero, ModContent.ItemType<SacrificialSkull>());
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            int Frame = Main.tile[i, j].TileFrameY / 18 / 4;
            if (Frame == 2 && !NPC.AnyNPCs(ModContent.NPCType<Bloodwalker>()))
                ChangeFrame(i, j, false);
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;

            int Frame = Main.tile[i, j].TileFrameY / 18 / 4;

            if (Frame == 0)
                player.cursorItemIconID = ModContent.ItemType<SacrificialSkull>();
            else if (Frame == 1)
                player.cursorItemIconID = ModContent.ItemType<BloodOrb>();

        }

        public override bool RightClick(int i, int j)
        {
            int Frame = Main.tile[i, j].TileFrameY / 18 / 4;
            if (Frame == 0 && Main.LocalPlayer.HeldItem.type == ModContent.ItemType<SacrificialSkull>())
            {
                Main.LocalPlayer.HeldItem.stack--;
                ChangeFrame(i, j);
                return true;
            }
            else if(Frame == 1 && Main.LocalPlayer.HeldItem.type == ModContent.ItemType<BloodOrb>() && Main.LocalPlayer.HeldItem.stack >= 5)
            {
                Main.LocalPlayer.HeldItem.stack -= 5;

                SoundEngine.PlaySound(SoundID.Roar, Main.LocalPlayer.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NPC.SpawnOnPlayer(Main.LocalPlayer.whoAmI, ModContent.NPCType<Bloodwalker>());
                else
                    NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, -1, -1, null, Main.LocalPlayer.whoAmI, ModContent.NPCType<Bloodwalker>());

                ChangeFrame(i, j);
                return true;
            }
            return false;
        }

        private void ChangeFrame(int i, int j, bool down = true)
        {
            int x = i - Main.tile[i, j].TileFrameX / 18 % 4;
            int y = j - Main.tile[i, j].TileFrameY / 18 % 4;
            for (int l = x; l < x + 4; l++)
            {
                for (int m = y; m < y + 4; m++)
                {
                    if (Main.tile[l, m].HasTile && Main.tile[l, m].TileType == Type)
                    {
                        if (down)
                            Main.tile[l, m].TileFrameY += (short)(AnimationFrameHeight);
                        else
                            Main.tile[l, m].TileFrameY -= (short)(AnimationFrameHeight);
                    }
                }
            }
            
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) => true;
    }
}
