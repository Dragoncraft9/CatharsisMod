using Terraria.ModLoader;

namespace CatharsisMod
{
    public class CatharsisMod : Mod
    {
        internal static CatharsisMod Instance;
        public override void Load()
        {
            Instance = this;
        }
        public override void Unload()
        {
            Instance = null;
        }
    }
}
