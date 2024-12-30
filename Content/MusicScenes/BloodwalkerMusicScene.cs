using Terraria.ModLoader;
using Terraria;
using CatharsisMod.Content.NPCs.Bloodwalker;

namespace CatharsisMod.Content.MusicScenes
{
    public class BloodwalkerMusicScene : ModSceneEffect
    {
        public override int Music => MusicLoader.GetMusicSlot(CatharsisMod.Instance, "Assets/Music/Bloodwalker");
        public override bool IsSceneEffectActive(Player player) => NPC.AnyNPCs(ModContent.NPCType<Bloodwalker>());
        public override SceneEffectPriority Priority => SceneEffectPriority.BossMedium;
    }
}
