using UnityEditor;
using UnityEngine;
namespace VideoGame.Editor
{
    public static class RemainingEnemyAssets
    {
        [MenuItem("Nexus/Rebuild remaining biome enemies")]
        public static void Build()
        {
            NeonEnemyAssets.Build();
            CorporateEnemyAssets.Build();
            UndercityEnemyAssets.Build();
            AssetDatabase.SaveAssets();
            Debug.Log("REMAINING_ENEMIES_BUILT: 12 models, 132 gameplay clips");
        }
    }
}
