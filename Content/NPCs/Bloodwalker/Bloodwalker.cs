using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using System.IO;
using Terraria.DataStructures;
using System;
using System.Linq;
using CalamityMod.DataStructures;
using System.Collections.Generic;
using Terraria.GameContent.Animations;

namespace CatharsisMod.Content.NPCs.Bloodwalker
{
    [AutoloadBossHead]
    public class Bloodwalker : ModNPC
    {
        public override string Texture => "CatharsisMod/Content/NPCs/Bloodwalker/BloodwalkerBody";
        public override string BossHeadTexture => "CatharsisMod/Content/NPCs/Bloodwalker/Bloodwalker_Head_Boss";

        public class BloodwalkerLimb(Vector2 offset, float rotation, float length)
        {
            public Vector2 Offset = offset;
            public float Rotation = rotation;
            public readonly float Length = length;

            public Vector2 GetFrontPoint() => Offset + (Rotation.ToRotationVector2() * Length);

            public void SendData(BinaryWriter writer)
            {
                writer.WritePackedVector2(Offset);
                writer.Write(Rotation);
            }

            public void ReceiveData(BinaryReader reader)
            {
                Offset = reader.ReadPackedVector2();
                Rotation = reader.ReadSingle();
            }
        }

        public override void SetStaticDefaults()
        {
            NPCID.Sets.TrailingMode[Type] = 7;
            NPCID.Sets.TrailCacheLength[Type] = 20;
        }
        public override void SetDefaults()
        {
            NPC.boss = true;
            NPC.width = NPC.height = 44;
            NPC.Size = new Vector2(150, 150);
            NPC.DR_NERD(0.10f);
            NPC.LifeMaxNERB(Main.masterMode ? 330000 : Main.expertMode ? 240000 : 180000, 300000);
            NPC.npcSlots = 5f;
            NPC.defense = 50;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.Zombie105;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.dontTakeDamage = true;
            NPC.Calamity().canBreakPlayerDefense = true;
        }
        Player target = null;
        BloodwalkerLimb[][] Arms = new BloodwalkerLimb[6][];
        int[] armCounters = new int[6];
        List<VerletSimulatedSegment> tailSegments;
        private Tuple<float, float>[] ArmAngles =
        [
            //Back Arms
            new(-MathHelper.PiOver2, MathHelper.PiOver2 - MathHelper.PiOver4),
            new(MathHelper.PiOver2 + MathHelper.PiOver4 / 2f, MathHelper.PiOver4 / 2f),

            new(MathHelper.PiOver2, -MathHelper.PiOver2 + MathHelper.PiOver4),
            new(-MathHelper.PiOver2 - MathHelper.PiOver4 / 2f, -MathHelper.PiOver4 / 2f),

            //Middle Arms
            new(-MathHelper.Pi/3f, MathHelper.PiOver4),
            new(MathHelper.PiOver2 + MathHelper.PiOver4, MathHelper.PiOver4 / 2f),

            new(MathHelper.Pi/3f, -MathHelper.PiOver4),
            new(-MathHelper.PiOver2 - MathHelper.PiOver4, -MathHelper.PiOver4 / 2f),

            //Front Arms
            new(-MathHelper.PiOver4, MathHelper.PiOver2),
            new(MathHelper.PiOver2 + MathHelper.PiOver4, MathHelper.PiOver4 / 2f),

            new(MathHelper.PiOver4, -MathHelper.PiOver2),
            new(-MathHelper.PiOver2 - MathHelper.PiOver4, -MathHelper.PiOver4 / 2f),
        ];

        private bool moving { get => NPC.ai[0] == 4; set => NPC.ai[0] = value ? 4 : 0; }
        private int counter {  get => (int)NPC.ai[1]; set => NPC.ai[1] = value; }
        private bool AttackPrep = false;

        public override void OnSpawn(IEntitySource source)
        {
            BloodwalkerLimb[] arm = [new(new(-32, -32), -MathHelper.PiOver2, 56), new(Vector2.Zero, MathHelper.PiOver2, 52)];
            Arms[0] = arm;
            arm = [new(new(-32, 32), MathHelper.PiOver2, 56), new(Vector2.Zero, -MathHelper.PiOver2, 52)];
            Arms[1] = arm;
            arm = [new(new(-8, -44), -MathHelper.Pi/3f, 56), new(Vector2.Zero, MathHelper.PiOver2, 52)];
            Arms[2] = arm;
            arm = [new(new(-8, 44), MathHelper.Pi/3f, 56), new(Vector2.Zero, -MathHelper.PiOver2, 52)];
            Arms[3] = arm;
            arm = [new(new(24, -40), -MathHelper.PiOver4, 56), new(Vector2.Zero, MathHelper.PiOver2, 52)];
            Arms[4] = arm;
            arm = [new(new(24, 40), MathHelper.PiOver4, 56), new(Vector2.Zero, -MathHelper.PiOver2, 52)];
            Arms[5] = arm;
            
            tailSegments = new List<VerletSimulatedSegment>(9);
            for (int i = 0; i < 9; i++)
            {
                VerletSimulatedSegment segment = new(NPC.Center + (Vector2.UnitX.RotatedBy(NPC.rotation) * -36) - Vector2.UnitX * 12 * i);
                tailSegments.Add(segment);
            }
            tailSegments[0].locked = true;

            for(int i = 0; i < 6; i++)
            {
                armCounters[i] = 16 * i;
            }

            target = Main.player[Player.FindClosest(NPC.position, NPC.width, NPC.height)];
            NPC.velocity = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX) * 4f;
        }
        public override void AI()
        {
            target = Main.player[Player.FindClosest(NPC.position, NPC.width, NPC.height)];
            
            if (NPC.Center.Distance(target.Center) < 200f)
                NPC.velocity = Vector2.Zero;
            if(NPC.velocity == Vector2.Zero && NPC.Center.Distance(target.Center) > 300f)
                NPC.velocity = NPC.rotation.ToRotationVector2() * 4f;

            if (NPC.velocity != Vector2.Zero)
            {
                moving = true;
                NPC.velocity = RotateTowards(NPC.velocity, (target.Center - NPC.Center).ToRotation(), MathHelper.Pi / 120f);
                NPC.rotation = NPC.velocity.ToRotation();
                for(int i = 0; i < 6; i++)
                {
                    if(!AttackPrep || armCounters[i] % 60 != 0)
                        armCounters[i]++;
                }
            }
            else
                moving = false;

            //Alters B;oodwalker's speed based on how its legs move
            /*
            float armMoveFactor = 0;
            for (int i = 0; i < 6; i++)
            {
                int wrappedCount = armCounters[i] % 60;
                if (wrappedCount >= 15)
                    armMoveFactor += CalamityUtils.SineOutEasing((wrappedCount - 15) / 45f, 1);
            }
            NPC.velocity = NPC.velocity.SafeNormalize(Vector2.Zero) * armMoveFactor * 1.5f;
            */
            if (moving)
                UpdateArms();

            tailSegments[0].position = NPC.Center + (Vector2.UnitX.RotatedBy(NPC.rotation) * -36);
            for(int i = 0; i < tailSegments.Count; i++)
            {
                VerletSimulatedSegment segment = tailSegments[i];
                if (!segment.locked)
                {
                    Vector2 vector = segment.position;
                    segment.position += (NPC.rotation + MathHelper.Pi).ToRotationVector2() * 0.1f / (i+1);
                    segment.oldPosition = vector;
                }
            }
            tailSegments = VerletSimulatedSegment.SimpleSimulation(tailSegments, 12, gravity: 0f);            
        }

        private void UpdateArms()
        {
            for(int i = 0; i < Arms.Length; i++)
            {
                BloodwalkerLimb[] arm = Arms[i];
                if (arm == null || armCounters[i] == -1)
                    continue;
                int wrappedCount = armCounters[i] % 60;
                int sign = (i % 2 == 0 ? 1 : -1);
                if (wrappedCount < 15)
                {
                    arm[0].Rotation = MathHelper.Lerp(ArmAngles[i * 2].Item1, ArmAngles[i * 2].Item2, CalamityUtils.PolyOutEasing(wrappedCount / 15f, 1));
                    arm[1].Rotation = MathHelper.Lerp(ArmAngles[i * 2 + 1].Item1, ArmAngles[i * 2 + 1].Item2, CalamityUtils.PolyOutEasing(wrappedCount / 15f, 1));
                }
                else
                {
                    arm[0].Rotation = MathHelper.Lerp(ArmAngles[i * 2].Item2, ArmAngles[i * 2].Item1, CalamityUtils.SineOutEasing((wrappedCount - 15) / 45f, 1));
                    arm[1].Rotation = MathHelper.Lerp(ArmAngles[i * 2 + 1].Item2, ArmAngles[i * 2 + 1].Item1, CalamityUtils.SineOutEasing((wrappedCount - 15) / 45f, 1));
                }
            }
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Vector2 drawPos = NPC.Center - screenPos;

            if (tailSegments != null)
            {
                Texture2D tail1 = ModContent.Request<Texture2D>("CatharsisMod/Content/NPCs/Bloodwalker/BloodwalkerTail1").Value;
                Texture2D tail2 = ModContent.Request<Texture2D>("CatharsisMod/Content/NPCs/Bloodwalker/BloodwalkerTail2").Value;
                Texture2D tail3 = ModContent.Request<Texture2D>("CatharsisMod/Content/NPCs/Bloodwalker/BloodwalkerTail3").Value;

                for (int i = 8; i >= 0; i--)
                {
                    Texture2D tex = i < 4 ? tail1 : i < 8 ? tail2 : tail3;
                    float rot;
                    if (i == 0)
                        rot = (NPC.Center - tailSegments[0].position).ToRotation();
                    else
                        rot = (tailSegments[i - 1].position - tailSegments[i].position).ToRotation();
                    spriteBatch.Draw(tex, tailSegments[i].position - screenPos, null, drawColor, rot - MathHelper.PiOver2, i == 8 ? new(tex.Width * 0.5f, tex.Height * 0.66f) : tex.Size() * 0.5f, 1f, 0, 0);
                }
            }
            Texture2D torso = TextureAssets.Npc[NPC.type].Value;
            spriteBatch.Draw(torso, drawPos, null, drawColor, NPC.rotation - MathHelper.PiOver2, torso.Size() * 0.5f, 1f, 0, 0);
            if (target != null)
            {
                Texture2D upperArm = ModContent.Request<Texture2D>("CatharsisMod/Content/NPCs/Bloodwalker/BloodwalkerArm1").Value;
                Texture2D lowerArm = ModContent.Request<Texture2D>("CatharsisMod/Content/NPCs/Bloodwalker/BloodwalkerArm2").Value;

                for (int i = 0; i < Arms.Length; i++)
                {
                    BloodwalkerLimb[] Arm = Arms[i];
                    if (Arm == null)
                        break;
                    BloodwalkerLimb Upper = Arm[0];
                    Vector2 UpperDrawPos = drawPos + Upper.Offset.RotatedBy(NPC.rotation);
                    float UpperRot = NPC.rotation + Upper.Rotation + (i % 2 == 0 ? -MathHelper.PiOver2 : (MathHelper.TwoPi - MathHelper.PiOver2));
                    spriteBatch.Draw(upperArm, UpperDrawPos, null, drawColor, UpperRot, new(i % 2 == 0 ? 20 : 66, 16), 1f, i % 2 == 0 ? SpriteEffects.FlipHorizontally : 0, 0);
                    BloodwalkerLimb Lower = Arm[1];
                    Vector2 LowerDrawPos = UpperDrawPos + Lower.Offset.RotatedBy(NPC.rotation) + (UpperRot + (i % 2 != 0 ? MathHelper.Pi : 0)).ToRotationVector2() * Upper.Length;
                    spriteBatch.Draw(lowerArm, LowerDrawPos, null, drawColor, NPC.rotation + Upper.Rotation + (i % 2 == 0 ? -MathHelper.PiOver2 : MathHelper.PiOver2 + MathHelper.Pi) + Lower.Rotation, new(i % 2 == 0 ? 14 : 104, 30), 1f, i % 2 == 0 ? SpriteEffects.FlipHorizontally : 0, 0);

                }

                Texture2D head = ModContent.Request<Texture2D>("CatharsisMod/Content/NPCs/Bloodwalker/BloodwalkerHead").Value;
                spriteBatch.Draw(head, drawPos + (NPC.rotation.ToRotationVector2() * 64), null, drawColor, (target.Center - NPC.Center).ToRotation() - MathHelper.PiOver2, head.Size() * 0.5f, 1f, 0, 0);
            }
            return false;
        }
        public static Vector2 RotateTowards(Vector2 originalVector, float idealAngle, float angleIncrement)
        {
            float curAngle = originalVector.ToRotation();
            float f = curAngle.AngleTowards(idealAngle, angleIncrement);
            Vector2 vector = f.ToRotationVector2();
            return vector * originalVector.Length();
        }
    }
}
