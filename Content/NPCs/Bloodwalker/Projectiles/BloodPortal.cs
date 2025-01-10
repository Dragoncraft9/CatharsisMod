using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CatharsisMod.Content.NPCs.Bloodwalker.Projectiles
{
    public class BloodPortal : ModProjectile
    {
        public override string Texture => "CalamityMod/ExtraTextures/SoulVortex";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 90;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 300;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.scale = 1f;
            Projectile.netImportant = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            aiCounter = 0;
            Projectile.scale = 0;
            SoundEngine.PlaySound(SoundID.DD2_EtherianPortalSpawnEnemy with { Volume = 1f }, Projectile.Center);
        }

        private int aiCounter = 0;
        public override void AI()
        {
            if (aiCounter < 20)
                Projectile.scale = CalamityUtils.SineBumpEasing(aiCounter / 40f, 1);
            else
                Projectile.scale = 1f;
            if (aiCounter > 120)
            {
                Projectile.scale = CalamityUtils.SineBumpEasing((aiCounter - 100) / 40f, 1);
                if (aiCounter > 140)
                    OnKill(Projectile.timeLeft);
            }

            if(aiCounter == 70)
            {
                Particle circle = new PulseRing(Projectile.Center, Vector2.Zero, Color.Crimson, 0.25f, 1f, 20);
                GeneralParticleHandler.SpawnParticle(circle);

                NPC.NewNPCDirect(NPC.GetSource_NaturalSpawn(), Projectile.Center, Main.rand.NextBool() ? NPCID.IchorSticker : NPCID.FloatyGross).velocity = Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4) * -4;
            }

            #region Visuals
            if (aiCounter > 20 && aiCounter % 10 == 0)
            {
                Vector2 spawnPosition = Projectile.Center + Main.rand.NextVector2CircularEdge(30f * Projectile.scale, 30f * Projectile.scale);
                Color color = Color.Lerp(Color.Red, Color.Magenta, Main.rand.NextFloat(0f, 1f));
                Particle particle = new AltSparkParticle(spawnPosition, (spawnPosition - Projectile.Center).SafeNormalize(Vector2.Zero) * (Main.rand.NextFloat(0.5f, 3f) * Projectile.scale), false, 200, Main.rand.NextFloat(0.5f, 1f) * Projectile.scale, color);
                GeneralParticleHandler.SpawnParticle(particle);
            }
            Lighting.AddLight(Projectile.Center, Color.Magenta.ToVector3());
            Projectile.rotation += 0.05f;
            #endregion
            aiCounter++;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Particles/LargeBloom").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.Black), 0f, origin, Projectile.scale / 1.8f, SpriteEffects.None, 0f);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            texture = ModContent.Request<Texture2D>("Terraria/Images/Projectile_656").Value;
            origin = texture.Size() * 0.5f;
            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.Crimson), -Projectile.rotation / 2.25f, origin, Projectile.scale * (2.25f + (0.5f * ((float)Math.Sin(aiCounter / -20f) / 2f + 0.5f))), SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.Crimson), Projectile.rotation / 1.75f, origin, Projectile.scale * (2.25f + (0.5f * ((float)Math.Sin(aiCounter / 20f) / 2f + 0.5f))), SpriteEffects.None, 0f);

            texture = ModContent.Request<Texture2D>("CalamityMod/Particles/LargeBloom").Value;
            drawPosition = Projectile.Center - Main.screenPosition;
            origin = texture.Size() * 0.5f;
            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.Black), 0f, origin, Projectile.scale / 3f, SpriteEffects.None, 0f);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            texture = TextureAssets.Projectile[Projectile.type].Value;
            origin = texture.Size() * 0.5f;
            Main.EntitySpriteDraw(texture, drawPosition, null, Color.Violet with {A = 150}, Projectile.rotation, origin, Projectile.scale / 3.5f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(texture, drawPosition, null, Color.MediumVioletRed with { A = 225 }, -Projectile.rotation, origin, Projectile.scale / 3.75f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(texture, drawPosition, null, Color.Crimson with { A = 200 }, Projectile.rotation, origin, Projectile.scale / 4.5f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(texture, drawPosition, null, Color.DarkRed with { A = 175 }, -Projectile.rotation, origin, Projectile.scale / 5f, SpriteEffects.None, 0f);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i <= 50; i++)
            {
                int dustStyle = Main.rand.NextBool() ? DustID.LifeDrain : DustID.PortalBolt;
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(3) ? DustID.SpookyWood : dustStyle);
                dust.scale = Main.rand.NextFloat(1.5f, 2.3f);
                dust.velocity = Main.rand.NextVector2Circular(10f, 10f);
                dust.noGravity = true;
                dust.color = (dust.type == DustID.PortalBolt ? Color.Magenta : default);
            }

            Projectile.active = false;
        }

    }
}
