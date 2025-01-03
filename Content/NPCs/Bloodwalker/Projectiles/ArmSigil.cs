using CalamityMod.Projectiles.Ranged;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static CatharsisMod.Content.NPCs.Bloodwalker.Projectiles.SpellSigil;
using Terraria.GameContent;
using CalamityMod;
using Terraria.DataStructures;
using Terraria.Audio;

namespace CatharsisMod.Content.NPCs.Bloodwalker.Projectiles
{
    public class ArmSigil : ModProjectile
    {
        public override string Texture => "CatharsisMod/Content/NPCs/Bloodwalker/Projectiles/Sigil2";
        public new string LocalizationCategory => "Projectiles.Bloodwalker";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DontAttachHideToAlpha[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 74;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 360;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.scale = 1f;
        }

        public override void OnSpawn(IEntitySource source)
        {
            for (int i = 0; i < 800; i++)
            {
                Projectile.position.Y++;
                if (Main.tile[Projectile.Center.ToTileCoordinates()].IsTileSolid())
                    break;
            }
        }

        Player Target { get => Main.player[(int)Projectile.ai[0]]; set => Projectile.ai[0] = value.whoAmI; }
        int Counter { get => (int)Projectile.ai[2]; set => Projectile.ai[2] = value; }

        Vector2 Scale = Vector2.Zero;

        public override void AI()
        {
            if (Counter <= 10)
                Scale = Vector2.Lerp(Vector2.Zero, new(1, 0.5f), Counter / 10f);
            if(Counter >= 340)
                Scale = Vector2.Lerp(new(1, 0.5f), Vector2.Zero, (Counter - 340) / 20f);

            Vector2 idealPos = Target.Center;

            for (int i = 0; i < 800; i++)
            {
                idealPos.Y++;
                if (Main.tile[new Vector2(Projectile.Center.X, idealPos.Y).ToTileCoordinates()].IsTileSolid())
                    break;
            }
            if (Math.Abs(Projectile.Center.Y - idealPos.Y) > 4)
            {
                if ((int)Projectile.Center.Y < (int)idealPos.Y)
                    Projectile.position.Y += 4;
                else if ((int)Projectile.Center.Y > (int)idealPos.Y)
                    Projectile.position.Y -= 4;
            }
            else
                Projectile.position.Y = idealPos.Y - Projectile.height / 2f;


            float XFactor = Math.Abs(Projectile.Center.X - idealPos.X) / 16f * Scale.X;

            if (Math.Abs(Projectile.Center.X - idealPos.X) > XFactor)
            {
                if ((int)Projectile.Center.X < (int)idealPos.X)
                    Projectile.position.X += XFactor;
                else if ((int)Projectile.Center.X > (int)idealPos.X)
                    Projectile.position.X -= XFactor;
            }
            else
                Projectile.position.Y = idealPos.Y - Projectile.height / 2f;

            if (Counter > 20 && Counter % 15 == 0)
            {
                Vector2 projectileSpawn = Projectile.Center + (Vector2.UnitY * 8f * Projectile.scale);
                
                if(Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), projectileSpawn, Vector2.Zero, ModContent.ProjectileType<SkeletalArm>(), 30, 0f);
            }

            Color color = Color.Lerp(Color.Red, Color.Purple, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f) / 2f + 0.5f);
            Lighting.AddLight(Projectile.Center, color.ToVector3());

            Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(48 * Scale.X, 48 * Scale.Y), DustID.LifeDrain);
            d.velocity = -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver2);
            Counter++;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center + (Vector2.UnitY * 8f * Projectile.scale) - Main.screenPosition;
            Color auraColor = Color.Lerp(Color.Red, Color.Purple, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f) / 2f + 0.5f);

            Main.EntitySpriteDraw(texture, drawPosition + Vector2.UnitX * 2f, null, auraColor * Projectile.Opacity, Projectile.rotation, texture.Size() * 0.5f, Scale * Projectile.scale, 0, 0);
            Main.EntitySpriteDraw(texture, drawPosition + Vector2.UnitY * 2f, null, auraColor * Projectile.Opacity, Projectile.rotation, texture.Size() * 0.5f, Scale * Projectile.scale, 0, 0);
            Main.EntitySpriteDraw(texture, drawPosition - Vector2.UnitX * 2f, null, auraColor * Projectile.Opacity, Projectile.rotation, texture.Size() * 0.5f, Scale * Projectile.scale, 0, 0);
            Main.EntitySpriteDraw(texture, drawPosition - Vector2.UnitY * 2f, null, auraColor * Projectile.Opacity, Projectile.rotation, texture.Size() * 0.5f, Scale * Projectile.scale, 0, 0);

            Main.EntitySpriteDraw(texture, drawPosition, null, Color.White * Projectile.Opacity, Projectile.rotation, texture.Size() * 0.5f, Scale * Projectile.scale, 0, 0);
            return false;
        }
    }
}
