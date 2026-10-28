using System;

namespace VideoGame.Core
{
    public enum BiomeId { NeonStreets, RuinedFactory, CorporateOffice, BiotechLab, Undercity, Transit }
    public readonly struct Deployment
    {
        public const int GeneratorVersion = 3;
        public readonly BiomeId Biome;
        public readonly int Rooms, Seed;
        public Deployment(int seed, int biome = -1, int size = 0)
        {
            if (biome < -1 || biome > 5) throw new ArgumentOutOfRangeException(nameof(biome));
            if (size != 0 && size != 6 && size != 10 && size != 20) throw new ArgumentOutOfRangeException(nameof(size));
            Seed = seed;
            // Independent recipe stream: changing dressing never changes topology or size.
            var random = new Random(unchecked(seed ^ 0x51ed270b));
            Biome = (BiomeId)(biome < 0 ? random.Next(6) : biome);
            Rooms = size == 0 ? new[] {6, 10, 20}[random.Next(3)] : size;
        }
        public static string Name(BiomeId biome) => new[] {
            "Neon Street District", "Ruined Automation Factory", "Corporate Office Breach",
            "Biotech Quarantine", "Undercity Utilities", "Elevated Transit Terminal"
        }[(int)biome];
        public static string SizeName(int size) => size == 6 ? "Small / 6 rooms" : size == 10 ? "Medium / 10 rooms" : size == 20 ? "Large / 20 rooms" : "Random / 6, 10 or 20";
        public static int DressingSeed(int seed, int room) => unchecked(seed * 397 ^ room * 7919 ^ 0x4bd12);
        // Existing enemy kinds: Hunter, Gunner, Rusher, Bombardier. Art/AI unchanged.
        public static int Enemy(BiomeId biome, int roll)
        {
            int[,] weights = { {0,1,2,2,1,3}, {0,0,1,3,3,2}, {1,1,1,0,2,3}, {0,2,3,3,1,2}, {2,2,0,3,1,3}, {1,1,0,3,3,2} };
            return weights[(int)biome, ((roll % 6) + 6) % 6];
        }
    }
}
