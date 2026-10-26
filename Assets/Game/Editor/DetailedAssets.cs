using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VideoGame.Editor
{
    public sealed class DetailedImporter : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.Contains("/Detailed/")) return;
            var importer = (ModelImporter)assetImporter;
            importer.importCameras = false; importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.isReadable = true;
        }
        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Detailed/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = 2048;
            string lower = assetPath.ToLowerInvariant();
            if (lower.Contains("normalmap") || lower.Contains("normalgl") || lower.Contains("_norm.")) importer.textureType = TextureImporterType.NormalMap;
            else if (!lower.Contains("albedo") && !lower.Contains("_diff") && !lower.Contains("_color")) importer.sRGBTexture = false;
        }
    }
    [InitializeOnLoad]
    public static class DetailedAssets
    {
        const string Root = "Assets/Game/Resources/Detailed/";
        static DetailedAssets() => EditorApplication.delayCall += Build;
        public static void Build()
        {
            if (!File.Exists(Root + "RawSoldier/Mesh.fbx")) return;
            ProjectSetup.EnsureScene();
            Directory.CreateDirectory(Root + "Materials");
            Directory.CreateDirectory(Root + "Prefabs");
            AssetDatabase.Refresh();
            Soldier("Operative", "FederalSoldierSkin", "Head1");
            Soldier("Hunter", "EvilSkin", "Head2");
            Soldier("Gunner", "EvilSkin", "Head3");
            foreach (string file in Directory.GetFiles(Root + "RawIndustrial", "*.fbx"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string texture = name == "roof_thing" ? "roof_thing_3" : name;
                string path = Root + "Prefabs/" + name + ".prefab";
                if (File.Exists(path)) continue;
                var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(file));
                model.name = name;
                var material = name == "Bulkhead" ? Material("Bulkhead", "Deck/MetalPlates017A_1K-JPG_Color.jpg", "Deck/MetalPlates017A_1K-JPG_NormalGL.jpg", null, "Deck/MetalPlates017A_1K-JPG_AmbientOcclusion.jpg") : Material(texture, "Textures/" + texture + "_diff.jpg", "Textures/" + texture + "_norm.jpg", "Textures/" + texture + "_spec.jpg", "Textures/" + texture + "_ao.jpg");
                foreach (var renderer in model.GetComponentsInChildren<Renderer>()) renderer.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, renderer.sharedMaterials.Length)).ToArray();
                PrefabUtility.SaveAsPrefabAsset(model, path); Object.DestroyImmediate(model);
            }
            var deck = Material("Deck", "Deck/MetalPlates017A_1K-JPG_Color.jpg", "Deck/MetalPlates017A_1K-JPG_NormalGL.jpg", null, "Deck/MetalPlates017A_1K-JPG_AmbientOcclusion.jpg");
            deck.SetTexture("_RoughnessMap", Tex("Deck/MetalPlates017A_1K-JPG_Roughness.jpg")); deck.SetFloat("_UseRoughness", 1);
            deck.SetTexture("_MetallicMap", Tex("Deck/MetalPlates017A_1K-JPG_Metalness.jpg")); deck.SetFloat("_UseMetallic", .7f);
            deck.color = new Color(.32f, .38f, .42f);
            deck.SetFloat("_Grime", .8f);
            EditorUtility.SetDirty(deck);
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] {Root + "Materials"}))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material.name == "Deck" || material.name.Contains("Skin")) continue;
                material.color = material.name == "Bulkhead" ? new Color(.25f, .3f, .33f) : new Color(.7f, .75f, .8f);
                material.SetFloat("_Grime", material.name == "Bulkhead" ? .45f : .2f);
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }
        static Texture2D Tex(string path) => string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(Root + path);
        static Material Material(string name, string albedo, string normal, string specular, string occlusion)
        {
            string path = Root + "Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Nexus/Detailed Surface"));
            material.mainTexture = Tex(albedo); material.SetTexture("_BumpMap", Tex(normal));
            if (Tex(specular) != null) material.SetTexture("_SpecGlossMap", Tex(specular));
            if (Tex(occlusion) != null) material.SetTexture("_OcclusionMap", Tex(occlusion));
            material.enableInstancing = true; AssetDatabase.CreateAsset(material, path);
            return material;
        }
        static void Soldier(string name, string skin, string head)
        {
            string path = Root + "Prefabs/" + name + ".prefab";
            if (File.Exists(path)) return;
            var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "RawSoldier/Mesh.fbx"));
            model.name = name;
            var material = Material(skin, "RawSoldier/" + skin + "_Albedo.tga", "RawSoldier/" + skin + "_NormalMap.png", "RawSoldier/" + skin + "_Specular.tga", "RawSoldier/" + skin + "_Occlusion.png");
            material.SetTexture("_EmissionMap", Tex("RawSoldier/" + skin + "_Emission.png")); material.SetColor("_EmissionColor", Color.white * 1.1f);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name.StartsWith("Head") && renderer.name != head) renderer.gameObject.SetActive(false);
                if (renderer.name == "HandSimple") renderer.gameObject.SetActive(false);
                renderer.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, renderer.sharedMaterials.Length)).ToArray();
            }
            foreach (var animator in model.GetComponentsInChildren<Animator>()) Object.DestroyImmediate(animator);
            PrefabUtility.SaveAsPrefabAsset(model, path); Object.DestroyImmediate(model);
        }
    }
}
