using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using CalamityMod;
using System.Diagnostics.Metrics;
using Terraria.DataStructures;
using Terraria.Audio;

namespace CatharsisMod.Content.NPCs.Bloodwalker.Projectiles
{
    public class SkeletalArm : ModProjectile
    {
        public override string Texture => "CatharsisMod/Content/NPCs/Bloodwalker/Projectiles/BoneArmHand";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DontAttachHideToAlpha[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 74;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 250;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.scale = 1f;
        }

        int Counter { get => (int)Projectile.ai[2]; set => Projectile.ai[2] = value; }
        Vector2 StartPosition = Vector2.Zero;
        Vector2 SpawnPosition = Vector2.Zero;
        public override void OnSpawn(IEntitySource source)
        {

            Projectile.rotation = -MathHelper.PiOver2 + Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4);

            SpawnPosition = Projectile.Center;

            StartPosition = Projectile.Center - Projectile.rotation.ToRotationVector2() * 24f;

            SoundEngine.PlaySound(SoundID.DD2_DarkMageSummonSkeleton with { MaxInstances = 8 }, SpawnPosition); 
            WorldGen.KillTile(SpawnPosition.ToTileCoordinates().X, SpawnPosition.ToTileCoordinates().Y, effectOnly: true);
        }

        public override void AI()
        {
            if (Counter <= 10)
            {
                Projectile.Center = Vector2.Lerp(StartPosition, StartPosition + Projectile.rotation.ToRotationVector2() * 54f, Counter / 10f);

                Dust d = Dust.NewDustPerfect(SpawnPosition + Main.rand.NextVector2Circular(16, 8), DustID.Blood);
                d.velocity = Projectile.rotation.ToRotationVector2().RotatedByRandom(MathHelper.PiOver4 / 2f) * Main.rand.NextFloat(1f, 6f);
                d.scale *= 2f;
            }
            if (Counter >= 220)
            {
                Projectile.Center = Vector2.Lerp(StartPosition + Projectile.rotation.ToRotationVector2() * 54f, StartPosition, (Counter - 220) / 30f);
                if(Counter % 10 == 0)
                    WorldGen.KillTile(SpawnPosition.ToTileCoordinates().X, SpawnPosition.ToTileCoordinates().Y, effectOnly: true);
            }

            if (Projectile.Center.Y < SpawnPosition.Y)
            {
                Color color = Color.Lerp(Color.Red, Color.Purple, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f) / 2f + 0.5f);
                Lighting.AddLight(Projectile.Center, color.ToVector3());
            }

            Counter++;
        }

        public override Color? GetAlpha(Color lightColor) => lightColor * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D Arm = ModContent.Request<Texture2D>("CatharsisMod/Content/NPCs/Bloodwalker/Projectiles/BoneArm").Value;
            Texture2D Hand = ModContent.Request<Texture2D>(Texture).Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Vector2 scale = Vector2.One;
            if (Counter < 10)
                scale = Vector2.Lerp(new(0, 1), Vector2.One, Counter / 10f);
            else if(Counter > 220)
                scale = Vector2.Lerp(Vector2.One, new(0, 1), (Counter - 220) / 40f);

            Main.EntitySpriteDraw(Arm, drawPosition, null, lightColor, Projectile.rotation + MathHelper.PiOver2, Arm.Size() * 0.5f, scale * Projectile.scale, 0);
            Main.EntitySpriteDraw(Hand, drawPosition + Projectile.rotation.ToRotationVector2() * 24f, null, lightColor, Projectile.rotation + MathHelper.PiOver2, Hand.Size() * 0.5f, scale * Projectile.scale, 0);

            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 v = Projectile.rotation.ToRotationVector2();
            Vector2 lineStart = Projectile.Center - (v * Projectile.width * 0.5f);
            Vector2 lineEnd = Projectile.Center + (v * Projectile.width * 0.5f);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), lineStart, lineEnd, Projectile.height, ref collisionPoint);
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI, List<int> overWiresUI)
        {
            drawCacheProjsBehindNPCsAndTiles.Add(index);
        }
    }
}
