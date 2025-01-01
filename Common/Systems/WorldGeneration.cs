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
                int DraedonStructuresIndex = FinalIndex + 6;

                tasks.Insert(DraedonStructuresIndex + 1, new PassLegacy("Underworld Shrine", (progress, config) =>
                {
                    progress.Message = Language.GetOrRegister("Mods.CatharsisMod.UI.WorldGen.UnderworldShrine").Value;

                    UnderworldShrine.PlaceUnderworldShrine(GenVars.structures);
                }));

                int FinalCalamityIndex = FinalIndex + 9;

                tasks.Insert(FinalCalamityIndex + 1, new PassLegacy("Necrotic Crypt", (progress, config) =>
                {
                    progress.Message = Language.GetOrRegister("Mods.CatharsisMod.UI.WorldGen.NecroticCrypt").Value;

                    NecroticCrypt.PlaceNecroticCrypt(GenVars.structures);
                }));
            }
        }
    }
}
