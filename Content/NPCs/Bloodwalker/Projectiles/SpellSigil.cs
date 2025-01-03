using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using CalamityMod;

namespace CatharsisMod.Content.NPCs.Bloodwalker.Projectiles
{
    public class SpellSigil : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CatharsisMod/Content/NPCs/Bloodwalker/Projectiles/Sigil1";

        public new string LocalizationCategory => "Projectiles.Bloodwalker";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DontAttachHideToAlpha[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 74;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = int.MaxValue;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.scale = 0f;
        }
        public enum BloodwalkerSpells
        {
            None, //for use by Bloodwalker, should never be used by the projectile
            Summon,
            Hands,
            Tears,       
        }

        int BloodwalkerIndex {  get => (int)Projectile.ai[0]; set => Projectile.ai[0] = value; }
        BloodwalkerSpells Spell { get => (BloodwalkerSpells)Projectile.ai[1]; set => Projectile.ai[1] = (int)value; }
        int Counter { get => (int)Projectile.ai[2]; set => Projectile.ai[2] = value; }      

        public override void AI()
        {
            NPC bloodwalker = Main.npc[BloodwalkerIndex];
            Projectile.Center = bloodwalker.Center + bloodwalker.rotation.ToRotationVector2() * 150f +bloodwalker.velocity;
            Projectile.rotation = bloodwalker.rotation;

            if (Counter < 30)
                Projectile.scale = CalamityUtils.ExpOutEasing(Counter / 30f, 1);
            else if (Counter > 240)
            {
                Projectile.scale = 1 + CalamityUtils.ExpOutEasing((Counter - 240) / 60f, 1);
                Projectile.Opacity = 1 - (Counter - 240) / 40f;
                if (Counter > 300)
                    Projectile.active = false;
            }
            else
                Projectile.scale = 1f;

            Projectile.rotation += (float)Math.Sin(Counter / 20f) * MathHelper.PiOver4;

            Counter++;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>($"CatharsisMod/Content/NPCs/Bloodwalker/Projectiles/Sigil{(int)Spell}").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color auraColor = Color.Lerp(Color.Red, Color.Purple, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f) / 2f + 0.5f);

            Main.EntitySpriteDraw(texture, drawPosition + Vector2.UnitX * 2f, null, auraColor * Projectile.Opacity, Projectile.rotation - MathHelper.PiOver2, texture.Size() * 0.5f, Projectile.scale, 0, 0);
            Main.EntitySpriteDraw(texture, drawPosition + Vector2.UnitY * 2f, null, auraColor * Projectile.Opacity, Projectile.rotation - MathHelper.PiOver2, texture.Size() * 0.5f, Projectile.scale, 0, 0);
            Main.EntitySpriteDraw(texture, drawPosition - Vector2.UnitX * 2f, null, auraColor * Projectile.Opacity, Projectile.rotation - MathHelper.PiOver2, texture.Size() * 0.5f, Projectile.scale, 0, 0);
            Main.EntitySpriteDraw(texture, drawPosition - Vector2.UnitY * 2f, null, auraColor * Projectile.Opacity, Projectile.rotation - MathHelper.PiOver2, texture.Size() * 0.5f, Projectile.scale, 0, 0);

            Main.EntitySpriteDraw(texture, drawPosition, null, Color.White * Projectile.Opacity, Projectile.rotation - MathHelper.PiOver2, texture.Size() * 0.5f, Projectile.scale, 0, 0);
            return false;
        }
    }
}
