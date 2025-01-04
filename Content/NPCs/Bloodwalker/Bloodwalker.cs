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
using static Microsoft.Xna.Framework.MathHelper;
using Terraria.Audio;
using CalamityMod.Particles;
using CatharsisMod.Content.NPCs.Bloodwalker.Projectiles;

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

            public static Vector2[] GetArmPositions(NPC bloodwalker, BloodwalkerLimb[] Arm, bool top)
            {
                Vector2[] Positions = new Vector2[3];

                BloodwalkerLimb Upper = Arm[0];
                Vector2 UpperPos = bloodwalker.Center + Upper.Offset.RotatedBy(bloodwalker.rotation);
                Positions[0] = UpperPos;
                float UpperRot = bloodwalker.rotation + Upper.Rotation + (top ? -PiOver2 : (TwoPi - PiOver2));

                BloodwalkerLimb Lower = Arm[1];
                Vector2 LowerPos = UpperPos + Lower.Offset.RotatedBy(UpperRot) + (UpperRot + (top ? 0 : Pi)).ToRotationVector2() * Upper.Length;
                Positions[1] = LowerPos;
                float lowerRot = bloodwalker.rotation + Upper.Rotation + (top ? -PiOver2 : PiOver2 + Pi) + Lower.Rotation;

                Positions[2] = LowerPos + lowerRot.ToRotationVector2() * Lower.Length;
                return Positions;
            }

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
            NPCID.Sets.TrailingMode[Type] = -1;
            NPCID.Sets.TrailCacheLength[Type] = 31;
        }
        public override void SetDefaults()
        {
            NPC.Size = new Vector2(150, 150);
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.npcSlots = 5f;

            NPC.HitSound = SoundID.NPCHit18;
            NPC.DeathSound = SoundID.NPCDeath21;           

            NPC.damage = 50;
            NPC.DR_NERD(0.10f);
            NPC.LifeMaxNERB(Main.masterMode ? 20000 : Main.expertMode ? 15000 : 11000, 18000);
            NPC.knockBackResist = 0f;
            NPC.defense = 50;
            NPC.Calamity().canBreakPlayerDefense = true;
        }
        Player target = null;
        private readonly BloodwalkerLimb[][] Arms = new BloodwalkerLimb[6][];
        private readonly int[] armCounters = new int[6];
        private readonly Vector2[] tailLocation = new Vector2[11];
        private readonly Tuple<float, float>[] ArmAngles =
        [
            //Back Arms
            new(-PiOver2, PiOver2 - PiOver4),
            new(PiOver2 + PiOver4 / 2f, PiOver4 / 2f),

            new(PiOver2, -PiOver2 + PiOver4),
            new(-PiOver2 - PiOver4 / 2f, -PiOver4 / 2f),

            //Middle Arms
            new(-Pi/3f, PiOver4),
            new(PiOver2 + PiOver4, PiOver4 / 2f),

            new(Pi/3f, -PiOver4),
            new(-PiOver2 - PiOver4, -PiOver4 / 2f),

            //Front Arms
            new(-PiOver4, PiOver2),
            new(PiOver2 + PiOver4, PiOver4 / 2f),

            new(PiOver4, -PiOver2),
            new(-PiOver2 - PiOver4, -PiOver4 / 2f),
        ];

        private enum BloodwalkerAI
        {
            //Phase 1
            Chase,
            Lunge,
            Rush,
            SpellPrep,
        }

        private int Counter { get => (int)NPC.ai[1]; set => NPC.ai[1] = value; }
        private BloodwalkerAI State { get => (BloodwalkerAI)NPC.ai[0]; set => NPC.ai[0] = (float)value; }
        Point storedArmCounters = Point.Zero;
        SpellSigil.BloodwalkerSpells Spell = (SpellSigil.BloodwalkerSpells)(-1);

        private bool AttackPrep = false;
        private int AttackLoops = 0;
        private int AttackCounter = 0;

        public override void OnSpawn(IEntitySource source)
        {
            BloodwalkerLimb[] arm = [new(new(-32, -40), -PiOver2, 70), new(new(4, -7), PiOver2, 68)];
            Arms[0] = arm;
            arm = [new(new(-32, 40), PiOver2, 70), new(new(0, -7), -PiOver2, 68)];
            Arms[1] = arm;
            arm = [new(new(-8, -44), -Pi / 3f, 70), new(new(4, -7), PiOver2, 68)];
            Arms[2] = arm;
            arm = [new(new(-8, 44), Pi / 3f, 70), new(new(0, -7), -PiOver2, 68)];
            Arms[3] = arm;
            arm = [new(new(24, -48), -PiOver4, 70), new(new(4, -7), PiOver2, 68)];
            Arms[4] = arm;
            arm = [new(new(24, 48), PiOver4, 70), new(new(0, -7), -PiOver2, 68)];
            Arms[5] = arm;
            
            for (int i = 0; i < 6; i++)
                armCounters[i] = 12 * i;

            for (int i = 0; i < tailLocation.Length; i++)
            {
                tailLocation[i] = NPC.Center + (Vector2.UnitX * -50).RotatedBy(NPC.rotation);
            }

            target = Main.player[Player.FindClosest(NPC.position, NPC.width, NPC.height)];
        }
        public override void AI()
        {
            Vector2 tailStart = NPC.Center + (Vector2.UnitX * -50).RotatedBy(NPC.rotation);

            switch (State)
            {
                case BloodwalkerAI.Chase:
                    if (NPC.velocity.LengthSquared() != 16f)
                        NPC.velocity = NPC.rotation.ToRotationVector2() * 4f;

                    NPC.velocity = RotateTowards(NPC.velocity, (target.Center - NPC.Center).ToRotation(), Pi / 120f);
                    NPC.rotation = NPC.velocity.ToRotation();
                    
                    for (int i = 0; i < 6; i++)
                        armCounters[i]++;
                    UpdateArms(60);

                    if(Spell != SpellSigil.BloodwalkerSpells.None)
                    {
                        switch(Spell)
                        {
                            case SpellSigil.BloodwalkerSpells.Tears:
                                int BoltRate = 30;
                                if (NPC.life / (float)NPC.lifeMax < 0.5f)
                                    BoltRate /= 2;
                                if (Main.netMode != NetmodeID.MultiplayerClient && Counter % BoltRate == 0)
                                {
                                    bool left = Counter % (BoltRate * 2) == 0;
                                    Projectile.NewProjectile(Projectile.GetSource_NaturalSpawn(), target.Center + new Vector2(left ? -800 : 800, Main.rand.Next(-300, 300)), Vector2.UnitX * (left ? 8 : -8), ProjectileID.BloodNautilusShot, 40, 0.5f);
                                }
                                break;
                            case SpellSigil.BloodwalkerSpells.Hands:
                                if (Main.netMode != NetmodeID.MultiplayerClient && Counter == 0)
                                    Projectile.NewProjectile(Projectile.GetSource_NaturalSpawn(), new(NPC.Center.X, target.Center.Y), Vector2.Zero, ModContent.ProjectileType<ArmSigil>(), 0, 0);
                                break;
                        }
                    }

                    if(Counter > 300)
                    {
                        Spell = SpellSigil.BloodwalkerSpells.None;
                        Counter = -1;
                        AttackPrep = true;
                        if (AttackLoops % 2 == 0)
                            State = BloodwalkerAI.Lunge;
                        else
                        {
                            UpdateArmInverval(60, 90);
                            if (Main.rand.NextBool(3))
                                State = BloodwalkerAI.Rush;
                            else
                            {
                                Spell = (SpellSigil.BloodwalkerSpells)Main.rand.Next(2) + 2;
                                State = BloodwalkerAI.SpellPrep;
                            }
                        }
                    }

                    break;
                case BloodwalkerAI.Lunge:
                    if (AttackPrep)
                    {
                        int interval = 60;

                        NPC.velocity = NPC.rotation.ToRotationVector2() * NPC.velocity.Length();
                        NPC.velocity *= 0.96f;

                        if (Counter < 15)
                        {
                            float toTargetAngle = (target.Center - NPC.Center).ToRotation();
                            NPC.rotation = NPC.rotation.AngleTowards(toTargetAngle, Pi / 32f);
                        }

                        bool prepped = true;

                        for (int i = 0; i < 6; i++)
                        {
                            if (i < 3)
                            {
                                if (armCounters[i] % interval != interval / 4f)
                                {
                                    armCounters[i]++;
                                    prepped = false;
                                }
                            }
                            else
                            {
                                if (armCounters[i] % interval != 0)
                                {
                                    armCounters[i]++;
                                    prepped = false;
                                }
                            }
                            
                        }
                        if (prepped && Counter >= 15)
                        {
                            Counter = -1;
                            AttackPrep = false;
                        }

                        UpdateArms(interval);
                    }
                    else
                    {
                        if (Counter == 0)
                        {
                            SoundEngine.PlaySound(SoundID.ForceRoarPitched);
                            NPC.velocity = NPC.rotation.ToRotationVector2() * 24;

                            for (int i = 0; i < 6; i++)
                            {
                                if(i < 4)
                                    armCounters[i] = 5;
                                else
                                    armCounters[i] = 0;
                            }
                        }

                        if (Counter < 15)
                        {
                            NPC.velocity *= 1.01f;

                            for (int i = 0; i < 6; i++)
                            {
                                if(i < 4 || (armCounters[i] < 5 && Counter % 2 == 0))
                                    armCounters[i]++;                                
                            }
                            UpdateArms(20);
                        }
                        else
                        {
                            NPC.velocity *= 0.92f;
                            if (Counter == 15)
                            {
                                for (int i = 0; i < 6; i++)
                                {
                                    if (i < 4)
                                        armCounters[i] = -12 * i;
                                    else
                                        armCounters[i] = (int)(armCounters[i] % 20 / 20f * 60f);
                                }
                            }
                            for (int i = 0; i < 6; i++)
                            {
                                if (i < 4)
                                    armCounters[i]++;
                                else
                                {
                                    if(Counter >= 20)
                                        armCounters[i] -= i == 4 ? 1 : 2;
                                    if(armCounters[i] < 0)
                                        armCounters[i] = -12 * i + armCounters[0];
                                }
                            }
                            UpdateArms(60);
                        }

                        float lengthSquared = NPC.velocity.LengthSquared();

                        if (lengthSquared >= 56)
                        {
                            //Dust.NewDustPerfect(, DustID.Blood, Scale: Main.rand.NextFloat(1f, 2f));
                            Vector2 bloodSpawn = tailStart + Vector2.UnitY.RotatedBy(NPC.rotation) * Main.rand.NextFloat(-96f, 96f);
                            int bloodLifetime = Main.rand.Next(22, 36);
                            float bloodScale = Main.rand.NextFloat(0.6f, 0.8f);
                            Color bloodColor = Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat());
                            bloodColor = Color.Lerp(bloodColor, new Color(51, 22, 94), Main.rand.NextFloat(0.65f));

                            if (Main.rand.NextBool(20))
                                bloodScale *= 2f;
                            Particle blood = new SparkParticle(bloodSpawn, (NPC.rotation + Pi).ToRotationVector2().RotatedByRandom(PiOver4) * Main.rand.NextFloat(4f, 8f), false, bloodLifetime, bloodScale, bloodColor);
                            GeneralParticleHandler.SpawnParticle(blood);
                        }
                        else if (lengthSquared < 16)
                        {
                            AttackCounter++;
                            float lifeRatio = NPC.life / (float)NPC.lifeMax;

                            if (lifeRatio > 0.66f || (lifeRatio > 0.33f && AttackCounter > 1) || AttackCounter > 2)
                            {
                                AttackLoops++; 
                                AttackCounter = 0;
                                Counter = 0;
                                State = BloodwalkerAI.Chase;
                            }
                            else
                            {
                                Counter = -15;
                                AttackPrep = true;
                            }
                        }
                    }
                    break;
                case BloodwalkerAI.Rush:
                    if(AttackPrep)
                    {
                        NPC.velocity *= 0.98f;

                        for (int i = 0; i < 6; i++)
                            armCounters[i]++;
                        UpdateArms(90);

                        if(NPC.velocity.LengthSquared() < 1f)
                        {
                            SoundEngine.PlaySound(SoundID.ForceRoarPitched);
                            UpdateArmInverval(90, 30);
                            AttackPrep = false;
                        }
                    }
                    else
                    {
                        if (NPC.velocity.LengthSquared() < 36f)
                            NPC.velocity *= 1.1f;
                        else if (NPC.velocity.LengthSquared() > 36f)
                            NPC.velocity = NPC.rotation.ToRotationVector2() * 6f;

                        NPC.velocity = RotateTowards(NPC.velocity, (target.Center - NPC.Center).ToRotation(), Pi / 120f);
                        NPC.rotation = NPC.velocity.ToRotation();
                        for (int i = 0; i < 6; i++)
                            armCounters[i]++;

                        UpdateArms(30);

                        if(Main.netMode != NetmodeID.MultiplayerClient && NPC.life / (float)NPC.lifeMax < 0.5f && Counter % 10 == 0)
                            for(int i = 0; i < 2; i++)
                            Projectile.NewProjectile(Projectile.GetSource_NaturalSpawn(), NPC.Center, Main.rand.NextVector2CircularEdge(4, 4), ProjectileID.BloodNautilusShot, 40, 0.5f);

                        if (Counter > 420)
                        {
                            AttackLoops++;
                            Counter = -1;
                            AttackPrep = true;
                            UpdateArmInverval(30, 60);
                            State = BloodwalkerAI.Chase;
                        }
                    }
                    break;
                case BloodwalkerAI.SpellPrep:
                    if (AttackPrep)
                    {
                        if (NPC.velocity.LengthSquared() > 4f)
                            NPC.velocity *= 0.98f;
                        else
                        {
                            NPC.velocity = NPC.rotation.ToRotationVector2() * 2f;
                            AttackPrep = false;
                            Counter = 0;
                        }

                        NPC.velocity = RotateTowards(NPC.velocity, (target.Center - NPC.Center).ToRotation(), Pi / 180f);
                        NPC.rotation = NPC.velocity.ToRotation();

                        for (int i = 0; i < 6; i++)
                            armCounters[i]++;
                        UpdateArms(90);
                    }
                    else
                    {
                        NPC.velocity = NPC.rotation.ToRotationVector2() * 2f;

                        NPC.velocity = RotateTowards(NPC.velocity, (target.Center - NPC.Center).ToRotation(), Pi / 180f);
                        NPC.rotation = NPC.velocity.ToRotation();

                        if(Counter >= 16)
                        {
                            if (Counter == 16 && Main.netMode != NetmodeID.MultiplayerClient)
                                Projectile.NewProjectile(Projectile.GetSource_NaturalSpawn(), NPC.Center + NPC.rotation.ToRotationVector2() * 150f, Vector2.Zero, ModContent.ProjectileType<SpellSigil>(), 0, 0, -1, NPC.whoAmI, (int)Spell);

                            if (Counter >= 320)
                            {
                                
                                UpdateArmInverval(90, 60);

                                armCounters[4] = armCounters[0] + 48;
                                armCounters[5] = armCounters[0] + 60;

                                Counter = -1;
                                State = BloodwalkerAI.Chase;
                                AttackLoops++;
                                break;
                            }
                            else if (Counter < 256)
                                Dust.NewDustPerfect(NPC.Center + NPC.rotation.ToRotationVector2() * 150f + Main.rand.NextVector2CircularEdge(32, 32), DustID.LifeDrain).velocity = NPC.velocity;
                        }

                        #region Arm Behavior
                        if (Counter == 1)
                        {
                            storedArmCounters = new(armCounters[4], armCounters[5]);
                            armCounters[4] = armCounters[5] = -1;
                        }
                        else
                            armCounters[5] = armCounters[4]--;

                        if(Counter <= 16)
                        { 
                            for (int i = 4; i < 6; i++)
                            {
                                Vector2 angles = GetArmAnglesAt(i, (i == 4 ? storedArmCounters.X : storedArmCounters.Y), 90);

                                BloodwalkerLimb[] arm = Arms[i];
                                int sign = (i % 2 == 0 ? 1 : -1);

                                arm[0].Rotation = angles.X.AngleLerp(PiOver4 * sign, Math.Abs(armCounters[i]) / 16f);
                            
                                arm[1].Rotation = angles.Y.AngleLerp(PiOver2 * sign, Math.Abs(armCounters[i]) / 16f);
                            }
                        }
                        if (Spell != 0 && Counter >= 290)
                        {
                            if(Counter == 290)
                            {
                                storedArmCounters = new(armCounters[0] + 102, armCounters[0] + 120);
                                armCounters[4] = armCounters[5] = -1;
                            }

                            for (int i = 4; i < 6; i++)
                            {
                                Vector2 angles = GetArmAnglesAt(i, (i == 4 ? storedArmCounters.X : storedArmCounters.Y), 90);
                                
                                BloodwalkerLimb[] arm = Arms[i];
                                int sign = (i % 2 == 0 ? 1 : -1);
                                
                                arm[0].Rotation = (PiOver4 * sign).AngleLerp(angles.X, (Counter - 290) / 30f);

                                arm[1].Rotation = (PiOver2 * sign).AngleLerp(angles.Y, (Counter - 290) / 30f);
                            }
                        }

                        for (int i = 0; i < 4; i++)
                            armCounters[i]++;

                        UpdateArms(90);
                        #endregion
                    }
                    break;
            }

            Counter++;

            #region Bloodwalker Tail
            if (NPC.velocity != Vector2.Zero) //Trialing Mode 2 sets OldPos values to NPC.position when set to not store values, so we can't rely on existing trailing modes for the purposes of the tail
            {
                for (int i = NPCID.Sets.TrailCacheLength[Type] - 1; i >= 0; i--)
                {
                    if (i == 0)
                        NPC.oldRot[0] = NPC.rotation;
                    else
                        NPC.oldRot[i] = NPC.oldRot[i - 1];
                }
            }

            float oldRot = NPC.oldRot[30];

            for (int i = 0; i < tailLocation.Length; i++)
            {
                if (i == 0)
                    tailLocation[0] = tailStart;
                else
                    tailLocation[i] = tailLocation[i-1] + NPC.rotation.AngleLerp(oldRot, i / 10f).ToRotationVector2().RotatedBy(Pi) * 12.8f;
            }
            #endregion
        }

        private void UpdateArms(int interval)
        {
            for (int i = 0; i < Arms.Length; i++)
            {
                BloodwalkerLimb[] arm = Arms[i];
                if (arm == null || armCounters[i] < 0)
                    continue;
                int wrappedCount = armCounters[i] % interval;
                if (wrappedCount < interval / 4f)
                {
                    float lerp = CalamityUtils.PolyOutEasing(wrappedCount / (interval / 4f), 1);
                    arm[0].Rotation = Lerp(ArmAngles[i * 2].Item1, ArmAngles[i * 2].Item2, lerp);
                    arm[1].Rotation = Lerp(ArmAngles[i * 2 + 1].Item1, ArmAngles[i * 2 + 1].Item2, lerp);
                }
                else
                {
                    float lerp = CalamityUtils.SineOutEasing((wrappedCount - (interval / 4f)) / (interval - (interval / 4f)), 1);
                    arm[0].Rotation = Lerp(ArmAngles[i * 2].Item2, ArmAngles[i * 2].Item1, lerp);
                    arm[1].Rotation = Lerp(ArmAngles[i * 2 + 1].Item2, ArmAngles[i * 2 + 1].Item1, lerp);
                }
            }
        }

        private void UpdateArmInverval(int oldInt, int newInt)
        {
            for(int i = 0; i < 6; i++)
                armCounters[i] = (int)(armCounters[i] % oldInt / (float)oldInt * newInt);
        }

        private Vector2 GetArmAnglesAt(int index, int count, int interval)
        {
            int wrappedCount = count % interval;
            float storedArmAngle0;
            float storedArmAngle1;
            if (wrappedCount < interval / 4f)
            {
                float lerp = CalamityUtils.PolyOutEasing(wrappedCount / (interval / 4f), 1);
                storedArmAngle0 = Lerp(ArmAngles[index * 2].Item1, ArmAngles[index * 2].Item2, lerp);
                storedArmAngle1 = Lerp(ArmAngles[index * 2 + 1].Item1, ArmAngles[index * 2 + 1].Item2, lerp);
            }
            else
            {
                float lerp = CalamityUtils.SineOutEasing((wrappedCount - (interval / 4f)) / (interval - (interval / 4f)), 1);
                storedArmAngle0 = Lerp(ArmAngles[index * 2].Item2, ArmAngles[index * 2].Item1, lerp);
                storedArmAngle1 = Lerp(ArmAngles[index * 2 + 1].Item2, ArmAngles[index * 2 + 1].Item1, lerp);
            }
            return new(storedArmAngle0, storedArmAngle1);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (target != null)
            {
                Texture2D tail1 = ModContent.Request<Texture2D>("CatharsisMod/Content/NPCs/Bloodwalker/BloodwalkerTail1").Value;
                Texture2D tail2 = ModContent.Request<Texture2D>("CatharsisMod/Content/NPCs/Bloodwalker/BloodwalkerTail2").Value;
                Texture2D tail3 = ModContent.Request<Texture2D>("CatharsisMod/Content/NPCs/Bloodwalker/BloodwalkerTail3").Value;
                Color lightValue = Color.White;
                for (int i = 10; i >= 0; i--)
                {
                    Texture2D tex = i == 10 ? tail3 : i > 4 ? tail2 : tail1;
                    float rot;
                    if (i == 0)
                        rot = (NPC.Center - tailLocation[i]).ToRotation();
                    else
                        rot = (tailLocation[i-1] - tailLocation[i]).ToRotation();
                    lightValue = Lighting.GetColor(tailLocation[i].ToTileCoordinates());

                    spriteBatch.Draw(tex, tailLocation[i] - screenPos, null, lightValue, rot - PiOver2, i == 10 ? new(tex.Width * 0.5f, tex.Height * 0.66f) : tex.Size() * 0.5f, 1f, 0, 0);
                }

                Texture2D torso = TextureAssets.Npc[NPC.type].Value;
                spriteBatch.Draw(torso, NPC.Center - screenPos, null, drawColor, NPC.rotation - PiOver2, torso.Size() * 0.5f, 1f, 0, 0);
            
                Texture2D upperArm = ModContent.Request<Texture2D>("CatharsisMod/Content/NPCs/Bloodwalker/BloodwalkerArm1").Value;
                Texture2D lowerArm = ModContent.Request<Texture2D>("CatharsisMod/Content/NPCs/Bloodwalker/BloodwalkerArm2").Value;

                for (int i = 0; i < Arms.Length; i++)
                {
                    BloodwalkerLimb[] Arm = Arms[i];
                    if (Arm == null)
                        break;
                    
                    BloodwalkerLimb Upper = Arm[0];
                    Vector2 UpperDrawPos = NPC.Center + Upper.Offset.RotatedBy(NPC.rotation);
                    float UpperRot = NPC.rotation + Upper.Rotation + (i % 2 == 0 ? -PiOver2 : (TwoPi - PiOver2));

                    lightValue = Lighting.GetColor((UpperDrawPos + UpperRot.ToRotationVector2() * Upper.Length / (i % 2 == 0 ? 2f : -2f)).ToTileCoordinates());
                    
                    
                    spriteBatch.Draw(upperArm, UpperDrawPos - screenPos, null, lightValue, UpperRot, new(i % 2 == 0 ? 19 : 80, 16), 1f, i % 2 == 0 ? SpriteEffects.FlipHorizontally : 0, 0);
                    
                    BloodwalkerLimb Lower = Arm[1];
                    Vector2 LowerDrawPos = UpperDrawPos + Lower.Offset.RotatedBy(UpperRot) + (UpperRot + (i % 2 != 0 ? Pi : 0)).ToRotationVector2() * Upper.Length;
                    float lowerRot = NPC.rotation + Upper.Rotation + (i % 2 == 0 ? -PiOver2 : PiOver2 + Pi) + Lower.Rotation;
                    
                    lightValue = Lighting.GetColor((LowerDrawPos + lowerRot.ToRotationVector2() * Lower.Length / (i % 2 == 0 ? 2f : -2f)).ToTileCoordinates());
                    
                    spriteBatch.Draw(lowerArm, LowerDrawPos - screenPos, null, lightValue, lowerRot, new(i % 2 == 0 ? 9 : 108, 45), 1f, i % 2 == 0 ? SpriteEffects.FlipHorizontally : 0, 0);

                }
                Texture2D head = ModContent.Request<Texture2D>("CatharsisMod/Content/NPCs/Bloodwalker/BloodwalkerHead").Value;
                Vector2 headPos = NPC.Center + (NPC.rotation.ToRotationVector2() * 64);
                float headAngle = ((target.Center - headPos).SafeNormalize(Vector2.UnitX) + NPC.rotation.ToRotationVector2()).ToRotation();
                lightValue = Lighting.GetColor(headPos.ToTileCoordinates());

                spriteBatch.Draw(head, headPos - screenPos, null, lightValue, headAngle - PiOver2, head.Size() * 0.5f, 1f, 0, 0);
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
