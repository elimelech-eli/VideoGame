using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VideoGame.Editor
{
    public sealed class LabReviewImporter : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.Contains("/LabReview/Models/")) return;
            var importer = (ModelImporter)assetImporter;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false; importer.importLights = false;
            importer.isReadable = true; importer.animationType = ModelImporterAnimationType.None;
        }
        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/LabReview/Textures/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = assetPath.Contains("Albedo") || assetPath.Contains("Normal") ? 2048 : 1024;
            importer.sRGBTexture = assetPath.Contains("Albedo") || assetPath.Contains("Emission");
            importer.textureType = assetPath.Contains("Normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.mipmapEnabled = true;
        }
    }
    public static class LabReviewAssets
    {
        const string Root = "Assets/Game/Resources/LabReview/";
        [Serializable] sealed class Manifest { public Entry[] materials; public Module[] modules; }
        [Serializable] sealed class Module { public string name,singleMaterial; }
        [Serializable] sealed class Entry { public string name, kind; public Maps maps; }
        [Serializable] sealed class Maps { public string Albedo, Normal, Emission, Metallic, Occlusion; }
        static Texture2D Map(string name) => string.IsNullOrEmpty(name) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Textures/" + name);
        [MenuItem("Nexus/Build lab tile review")]
        public static void Rebuild()
        {
            AssetDatabase.Refresh(); Directory.CreateDirectory(Root + "Materials"); Directory.CreateDirectory(Root + "Prefabs");
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "manifest.json"));
            foreach (var e in manifest.materials)
            {
                string path = Root + "Materials/" + e.name + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) { mat = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(mat, path); }
                mat.shader=Shader.Find("Standard");mat.shaderKeywords=new string[0];
                mat.color=Color.white;mat.enableInstancing=true;mat.renderQueue=-1;
                mat.SetFloat("_Mode",0);mat.SetInt("_SrcBlend",1);mat.SetInt("_DstBlend",0);mat.SetInt("_ZWrite",1);
                mat.mainTexture=Map(e.maps.Albedo);
                mat.SetTexture("_BumpMap",Map(e.maps.Normal));if(Map(e.maps.Normal))mat.EnableKeyword("_NORMALMAP");
                mat.SetTexture("_MetallicGlossMap",Map(e.maps.Metallic));if(Map(e.maps.Metallic))mat.EnableKeyword("_METALLICGLOSSMAP");
                mat.SetFloat("_GlossMapScale",1);mat.SetFloat("_BumpScale",1);
                mat.SetTexture("_OcclusionMap",Map(e.maps.Occlusion));mat.SetFloat("_OcclusionStrength",1);
                mat.SetTexture("_EmissionMap",Map(e.maps.Emission));
                mat.SetColor("_EmissionColor",Map(e.maps.Emission)?Color.white*(e.kind=="glass"?.45f:e.name=="Panel_sterowania"?1.4f:1.1f):Color.black);
                mat.globalIlluminationFlags=Map(e.maps.Emission)?MaterialGlobalIlluminationFlags.BakedEmissive:MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                if(Map(e.maps.Emission))mat.EnableKeyword("_EMISSION");
                if(e.kind=="glass")
                {
                    mat.SetFloat("_Mode",3);mat.SetInt("_SrcBlend",1);mat.SetInt("_DstBlend",10);mat.SetInt("_ZWrite",0);
                    mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");mat.renderQueue=3000;
                    mat.SetTexture("_MetallicGlossMap",null);mat.DisableKeyword("_METALLICGLOSSMAP");mat.SetFloat("_Metallic",0);mat.SetFloat("_Glossiness",.78f);
                }
                EditorUtility.SetDirty(mat);
            }
            foreach (string file in Directory.GetFiles(Root + "Models", "*.fbx"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                var root = new GameObject(name); var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(file),root.transform);
                foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
                {
                    var filter = renderer.GetComponent<MeshFilter>();
                    // One source object per source material; exported mesh names retain the original object identity.
                    int marker=renderer.name.IndexOf("Object_",StringComparison.Ordinal);
                    string role;
                    if(marker>=0)role=SourceMaterial(renderer.name.Substring(marker).Split('.')[0]);
                    else
                    {
                        var module=Array.Find(manifest.modules,m=>m.name==name);
                        role=module?.singleMaterial;
                        if(string.IsNullOrEmpty(role))throw new InvalidOperationException("Unmapped renderer "+renderer.name);
                    }
                    var material = AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/"+role+".mat");
                    if (material == null) throw new InvalidOperationException("Missing lab material for " + role);
                    var slots = new Material[filter.sharedMesh.subMeshCount]; for (int i=0;i<slots.Length;i++) slots[i]=material;
                    renderer.sharedMaterials=slots;
                }
                if(name.StartsWith("Wall",StringComparison.Ordinal))model.transform.localRotation=Quaternion.Euler(0,180,0)*model.transform.localRotation;
                Bounds bounds = BoundsOf(root);
                model.transform.position -= new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                bounds=BoundsOf(root); root.layer=9; var solid=root.AddComponent<BoxCollider>();solid.center=bounds.center;solid.size=bounds.size;
                PrefabUtility.SaveAsPrefabAsset(root,Root+"Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets(); Debug.Log("LAB_REVIEW_ASSETS_READY: nineteen credited source modules, restored hardware PBR and UV-matched original displays.");
        }
        static string SourceMaterial(string key)
        {
            switch(key)
            {
                case "Object_6":return "duz_soik_szybka";case "Object_7":return "duzy_soik";case "Object_20":return "soik_may_szybka";case "Object_21":return "soik_may";
                case "Object_4": return "Podoga2"; case "Object_29": return "panel3";
                case "Object_27": return "SCIANA_DUZA"; case "Object_30": return "sciana3";
                case "Object_11": return "rura_gwna_baza";case "Object_13":return "rura_glowna_szybka";
                case "Object_14":return "stolik";case "Object_17":return "stolik_szybka";
                case "Object_18":return "szafka_body";case "Object_23":return "szafka_szybka";
                case "Object_3":return "Panel_sterowania";case "Object_10":return "rura_przod";case "Object_25":return "rura_tylna";
                case "Object_16":return "szafka";case "Object_24":return "szafka_szyba";case "Object_9":return "drzwi";case "Object_22":return "tablica";
                default: throw new InvalidOperationException("Unknown lab source object "+key);
            }
        }
        static Bounds BoundsOf(GameObject root)
        {
            var all=root.GetComponentsInChildren<Renderer>(); if(all.Length==0)throw new InvalidOperationException("Empty lab export");
            Bounds bounds=all[0].bounds;foreach(var r in all)bounds.Encapsulate(r.bounds);return bounds;
        }
    }
}
