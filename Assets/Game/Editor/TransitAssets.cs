using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace VideoGame.Editor
{
    public sealed class TransitImporter:AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if(!assetPath.Contains("/Transit/Models/"))return;
            var i=(ModelImporter)assetImporter;i.materialImportMode=ModelImporterMaterialImportMode.None;
            i.importCameras=false;i.importLights=false;i.animationType=ModelImporterAnimationType.None;i.isReadable=true;
        }
        void OnPreprocessTexture()
        {
            if(!assetPath.Contains("/Transit/Textures/"))return;
            var i=(TextureImporter)assetImporter;i.maxTextureSize=2048;i.mipmapEnabled=true;
            i.sRGBTexture=assetPath.Contains("Albedo")||assetPath.Contains("Emission");
            i.textureType=assetPath.Contains("Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
        }
    }
    public static class TransitAssets
    {
        const string Root="Assets/Game/Resources/Transit/";
        [Serializable]sealed class Manifest{public Entry[] materials;public Module[] modules;}
        [Serializable]sealed class Module{public string name,singleMaterial;}
        [Serializable]sealed class Entry{public string name;public Maps maps;public float[] color,emission;public float metallic,smoothness;}
        [Serializable]sealed class Maps{public string Albedo,Normal,Emission,Metallic;}
        static Texture2D Map(string n)=>string.IsNullOrEmpty(n)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/"+n);
        [MenuItem("Nexus/Build Transit kit")]
        public static void Build()
        {
            AssetDatabase.Refresh();Directory.CreateDirectory(Root+"Materials");Directory.CreateDirectory(Root+"Prefabs");
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Root+"manifest.json"));
            foreach(var e in manifest.materials)
            {
                string path=Root+"Materials/"+e.name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
                m.shader=Shader.Find("Standard");m.shaderKeywords=new string[0];m.enableInstancing=true;
                m.mainTexture=Map(e.maps.Albedo);m.color=m.mainTexture?Color.white:new Color(e.color[0],e.color[1],e.color[2],1);
                m.SetTexture("_BumpMap",Map(e.maps.Normal));if(Map(e.maps.Normal))m.EnableKeyword("_NORMALMAP");
                m.SetTexture("_MetallicGlossMap",Map(e.maps.Metallic));if(Map(e.maps.Metallic))m.EnableKeyword("_METALLICGLOSSMAP");
                m.SetFloat("_Metallic",e.metallic);m.SetFloat("_Glossiness",e.smoothness);m.SetFloat("_GlossMapScale",1);
                m.SetTexture("_EmissionMap",Map(e.maps.Emission));
                var emission=new Color(e.emission[0],e.emission[1],e.emission[2]);
                m.SetColor("_EmissionColor",Map(e.maps.Emission)?Color.white:emission);
                if(Map(e.maps.Emission)||emission.maxColorComponent>0)m.EnableKeyword("_EMISSION");
                EditorUtility.SetDirty(m);
            }
            foreach(string file in Directory.GetFiles(Root+"Models","*.fbx"))
            {
                string name=Path.GetFileNameWithoutExtension(file);var root=new GameObject(name);
                var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(file),root.transform);
                foreach(var r in model.GetComponentsInChildren<MeshRenderer>())
                {
                    int index=r.name.IndexOf("M_",StringComparison.Ordinal);
                    string key=index<0?Array.Find(manifest.modules,m=>m.name==name)?.singleMaterial:r.name.Substring(index+2);
                    if(string.IsNullOrEmpty(key))throw new InvalidOperationException("Unmapped Transit mesh "+r.name);
                    var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/"+key+".mat");
                    if(mat==null)throw new InvalidOperationException("Missing Transit material "+key);
                    r.sharedMaterials=new[]{mat};
                }
                var rs=root.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
                // Retain exported sockets. Track and train pivots are their common rail datum.
                root.layer=name.StartsWith("Floor",StringComparison.Ordinal)?8:9;
                if(name!="Track"&&name!="Fixture")
                {
                    var c=root.AddComponent<BoxCollider>();c.center=bounds.center;c.size=bounds.size;
                    if(name.StartsWith("Wall",StringComparison.Ordinal))c.size=new Vector3(bounds.size.x,bounds.size.y,.22f);
                    if(name.StartsWith("Floor",StringComparison.Ordinal)){c.center=new Vector3(0,-.12f,0);c.size=new Vector3(bounds.size.x,.24f,bounds.size.z);}
                }
                PrefabUtility.SaveAsPrefabAsset(root,Root+"Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();Debug.Log("TRANSIT_KIT_READY: source architecture, station equipment and intact material-batched train.");
        }
    }
}
