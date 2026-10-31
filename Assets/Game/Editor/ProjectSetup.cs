using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VideoGame.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        static ProjectSetup() => EditorApplication.delayCall += EnsureScene;
        public static void EnsureScene()
        {
            System.IO.Directory.CreateDirectory("Assets/Game/Resources");
            EnsureMaterial("PrototypeSurface", "Standard");
            EnsureMaterial("PrototypeTrace", "Sprites/Default");
            EnsureMaterial("Cinematic", "Hidden/Nexus/Cinematic");
            EnsureMaterial("Particle", "Nexus/Particle");
            EnsureMaterial("Atmosphere", "Nexus/Atmospheric Shaft");
            const string path = "Assets/Game/Scenes/Prototype.unity";
            if (System.IO.File.Exists(path)) return;
            System.IO.Directory.CreateDirectory("Assets/Game/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
            PlayerSettings.companyName = "Eli";
            PlayerSettings.productName = "Sci-Fi RPG Prototype";
            AssetDatabase.SaveAssets();
        }
        static void EnsureMaterial(string name, string shader)
        {
            string path = "Assets/Game/Resources/" + name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
                AssetDatabase.CreateAsset(new Material(Shader.Find(shader)), path);
        }
        public static void BuildWindows()
        {
            EnsureScene();
            DetailedAssets.Build();
            RobotAssets.Build();
            ActorAnimationBuilder.Build();
            OperativeV03Assets.Build();
            LoadoutWeaponAssets.Build();
            BiomeAssets.Build();
            LabReviewAssets.Rebuild();
            LabEnemyAssets.Build();
            TransitEnemyAssets.Build();
            TransitAssets.Build();
            var report = BuildPipeline.BuildPlayer(new[] { "Assets/Game/Scenes/Prototype.unity" }, "Builds/Windows/VideoGame.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Windows build failed: " + report.summary.result);
        }
    }
}
