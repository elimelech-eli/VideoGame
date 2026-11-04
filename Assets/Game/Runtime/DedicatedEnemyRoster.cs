using System;
using VideoGame.Core;
namespace VideoGame
{
    // Asset selection only; roles, attack timing, difficulty and shields stay in Drone.
    public static class DedicatedEnemyRoster
    {
        public static string Prefix(BiomeId biome)=>biome==BiomeId.NeonStreets?"Neon":biome==BiomeId.CorporateOffice?"Corporate":biome==BiomeId.Undercity?"Undercity":null;
        public static string PrefabFolder(BiomeId biome)=>"Detailed/"+(Prefix(biome)??throw new ArgumentException("No dedicated roster"))+"Enemies/Prefabs/";
        public static string AnimationFolder(string model)
        {
            foreach(string prefix in new[]{"Neon","Corporate","Undercity"})
                if(model.StartsWith(prefix,StringComparison.Ordinal))return prefix+"EnemyAnimations/"+model;
            return null;
        }
        public static void Resolve(BiomeId biome,EnemyKind kind,out string model,out float size)
        {
            string prefix=Prefix(biome)??throw new ArgumentException("No dedicated roster");
            model=prefix+(kind==EnemyKind.Hunter?"Heavy":kind==EnemyKind.Gunner?"Gunner":kind==EnemyKind.Bombardier?"Aoe":"Melee");
            size=kind==EnemyKind.Hunter?3f:kind==EnemyKind.Gunner?2.15f:kind==EnemyKind.Bombardier?(biome==BiomeId.Undercity?3f:2.4f):(biome==BiomeId.CorporateOffice?1.7f:2.1f);
        }
    }
}
