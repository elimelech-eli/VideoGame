using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VideoGame.Editor
{
    public sealed class RobotImporter : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.Contains("Resources/Audio/")) return;
            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis; settings.quality = .75f;
            settings.preloadAudioData = false; importer.defaultSampleSettings = settings;
        }
    }
    public static class RobotAssets
    {
        const string Root = "Assets/Game/Resources/Detailed/Robots/";
        public static readonly string[] Models = {"Crawler", "Roller", "Blaster", "Lobber"};
        public static void Build() => Build(false);
        [MenuItem("Nexus/Rebuild robot prefabs and animation clips")]
        public static void Rebuild() => Build(true);
        public static void InspectMeshes()
        {
            foreach(string name in Models)
            {
                var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/"+name+".prefab"));
                Resources.Load<AnimationClip>("RobotAnimations/"+name+"/Idle").SampleAnimation(model,.1f);
                var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();var a=new Mesh();var b=new Mesh();skin.BakeMesh(a,false);skin.BakeMesh(b,true);
                Debug.Log("ROBOT_BOUNDS "+name+" reported="+skin.bounds.size+" scale="+skin.transform.lossyScale+" mesh="+skin.sharedMesh.bounds.size+" baked="+a.bounds.size+" compensated="+b.bounds.size);
                Object.DestroyImmediate(a);Object.DestroyImmediate(b);Object.DestroyImmediate(model);
            }
        }
        static void Build(bool force)
        {
            Directory.CreateDirectory(Root + "Materials"); Directory.CreateDirectory(Root + "Prefabs");
            AssetDatabase.Refresh();
            foreach (string name in Models)
            {
                string file = Root + "Models/" + name + ".fbx";
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(file); if (source == null) continue;
                string path = Root + "Prefabs/" + name + ".prefab";
                if (force || !File.Exists(path))
                {
                    var model = Object.Instantiate(source); model.name = name;
                    var tint = name == "Crawler" ? new Color(.7f, .27f, .1f) : name == "Roller" ? new Color(.65f, .12f, .08f) : name == "Blaster" ? new Color(.12f, .42f, .47f) : new Color(.4f, .2f, .53f);
                    var materials = new[] {Material(name, "colorize plastic albedo.png", "orange plastic norm.png", "orange plastic specular.png", tint), Material("Metal", "metal grid albedo.png", "metal grid normal.png", "metal grid specular.png", new Color(.55f,.6f,.65f)), Material("Rubber", "black rubber albedo.png", "black rubber norm.png", "black rubber spec.png", Color.white)};
                    foreach (var renderer in model.GetComponentsInChildren<Renderer>()) renderer.sharedMaterials = materials;
                    foreach (var animation in model.GetComponentsInChildren<Animation>()) Object.DestroyImmediate(animation);
                    foreach (var animator in model.GetComponentsInChildren<Animator>()) Object.DestroyImmediate(animator);
                    PrefabUtility.SaveAsPrefabAsset(model, path); Object.DestroyImmediate(model);
                }
                var clips = AssetDatabase.LoadAllAssetsAtPath(file).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
                string folder = "Assets/Game/Resources/RobotAnimations/" + name + "/";
                Directory.CreateDirectory(folder); AssetDatabase.Refresh();
                foreach (string alias in ActorAnimationBuilder.Names)
                {
                    string clipPath = folder + alias + ".anim"; if (!force && File.Exists(clipPath)) continue;
                    string action = alias.StartsWith("Run") || alias == "Dash" ? "move" : alias == "Death" ? "die1" : alias == "Idle" || alias == "Hit" ? "idle" : "attack";
                    var original = clips.FirstOrDefault(c => c.name == action || c.name.EndsWith("|" + action));
                    if (original == null) throw new System.InvalidOperationException(name + " missing robot action " + action + "; imported: " + string.Join(",", clips.Select(c => c.name)));
                    var clip = Object.Instantiate(original); clip.name = alias; clip.legacy = true;
                    clip.wrapMode = alias == "Idle" || alias.StartsWith("Run") ? WrapMode.Loop : WrapMode.ClampForever;
                    var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    if (existing == null) AssetDatabase.CreateAsset(clip, clipPath);
                    else { EditorUtility.CopySerialized(clip,existing); Object.DestroyImmediate(clip); }
                }
            }
            AssetDatabase.SaveAssets();
        }
        static Material Material(string name, string albedo, string normal, string specular, Color tint)
        {
            string path = Root + "Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path); if (material != null) return material;
            material = new Material(Shader.Find("Nexus/Detailed Surface"));
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Textures/" + albedo);
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Textures/" + normal));
            material.SetTexture("_SpecGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Textures/" + specular));
            material.color = tint; material.SetFloat("_Grime", .25f); material.enableInstancing = true;
            AssetDatabase.CreateAsset(material, path); return material;
        }
    }
}
