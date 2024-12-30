using CatharsisMod.Content.World;
using System.Collections.Generic;
using Terraria.GameContent.Generation;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace CatharsisMod.Common.Systems
{
    public class WorldGeneration : ModSystem
    {
        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            int FinalIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Final Cleanup"));
            if (FinalIndex != -1)
            {
                int ShrineStructuresIndex = FinalIndex + 7;

                int currentFinalIndex = FinalIndex;
                tasks.Insert(ShrineStructuresIndex + 1, new PassLegacy("Underworld Shrine", (progress, config) =>
                {
                    progress.Message = Language.GetOrRegister("Mods.CatharsisMod.UI.WorldGen.UnderworldShrine").Value;

                    UnderworldShrine.PlaceUnderworldShrine(GenVars.structures);
                }));
            }
        }
    }
}
