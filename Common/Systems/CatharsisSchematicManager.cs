using CalamityMod.Schematics;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria.ModLoader;

namespace CatharsisMod.Common.Systems
{
    public class CatharsisSchematicManager : ModSystem
    {
        private const string StructureFilePath = "Content/World/Schematics/";

        internal const string UnderworldShrineKey = "Underworld Shrine";
        internal const string UnderworldShrineFilename = StructureFilePath + "UnderworldShrine.csch";

        internal const string NecroticCryptKey = "Necrotic Crypt";
        internal const string NecroticCryptFilename = StructureFilePath + "NecroticCrypt.csch";

        internal static Dictionary<string, SchematicMetaTile[,]> TileMaps =>
        typeof(SchematicManager).GetField("TileMaps", (BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)).GetValue(null) as Dictionary<string, SchematicMetaTile[,]>;

        internal static readonly MethodInfo ImportSchematicMethod = typeof(CalamitySchematicIO).GetMethod("ImportSchematic", (BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));


        public override void OnModLoad()
        {
            TileMaps[UnderworldShrineKey] = LoadCatharsischematic(UnderworldShrineFilename);
            TileMaps[NecroticCryptKey] = LoadCatharsischematic(NecroticCryptFilename);
        }

        public static SchematicMetaTile[,] LoadCatharsischematic(string filename)
        {
            SchematicMetaTile[,] ret = null;
            using (Stream st = CatharsisMod.Instance.GetFileStream(filename, true))
                ret = (SchematicMetaTile[,])ImportSchematicMethod.Invoke(null, [st]);
            return ret;
        }
    }
}
