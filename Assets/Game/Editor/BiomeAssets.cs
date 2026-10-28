using System.IO;
using UnityEditor;
using UnityEngine;
using VideoGame.Core;

namespace VideoGame.Editor
{
    public sealed class BiomeImporter : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if(!assetPath.Contains("/Biomes/Models/"))return;
            var importer=(ModelImporter)assetImporter;
            importer.importCameras=importer.importLights=importer.importAnimation=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation=ModelImporterMaterialLocation.InPrefab;
            importer.isReadable=true;
        }
        void OnPreprocessTexture()
        {
            if(!assetPath.Contains("/Biomes/Textures/"))return;
            var importer=(TextureImporter)assetImporter;
            importer.maxTextureSize=512;importer.wrapMode=TextureWrapMode.Repeat;
            importer.textureShape=TextureImporterShape.Texture2D;
            importer.textureType=assetPath.Contains("Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
        }
        void OnPreprocessAudio()
        {
            if(!assetPath.Contains("/Biomes/Audio/"))return;
            var importer=(AudioImporter)assetImporter;var sample=importer.defaultSampleSettings;
            sample.loadType=AudioClipLoadType.Streaming;sample.compressionFormat=AudioCompressionFormat.Vorbis;sample.quality=.65f;
            sample.preloadAudioData=false;importer.defaultSampleSettings=sample;
        }
    }
    [InitializeOnLoad]
    public static class BiomeAssets
    {
        const string Root="Assets/Game/Resources/Biomes/";
        static BiomeAssets()=>EditorApplication.delayCall+=Ensure;
        static void Ensure(){if(!File.Exists(Root+"Prefabs/5_Room.prefab"))Build();}
        public static void Build()
        {
            if(!Directory.Exists(Root+"Models"))return;
            Directory.CreateDirectory(Root+"Materials");Directory.CreateDirectory(Root+"Prefabs");
            AssetDatabase.Refresh();
            for(int id=0;id<6;id++)
            {
                var profile=BiomeProfile.Get((BiomeId)id);
                foreach(string tag in new[]{"Shell","Trim","Glow","Dark","Glass","Floor"})
                {
                    string path=Root+"Materials/"+id+"_"+tag+".mat";
                    var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(mat==null){mat=new Material(Shader.Find("Nexus/Detailed Surface"));AssetDatabase.CreateAsset(mat,path);}
                    mat.enableInstancing=true;
                    mat.color=tag=="Shell"?profile.Shell:tag=="Trim"?new Color(.14f,.17f,.19f):tag=="Glow"?profile.Accent:
                        tag=="Dark"?new Color(.025f,.035f,.045f):tag=="Glass"?profile.Secondary*.25f:Color.white;
                    mat.SetFloat("_Glossiness",tag=="Glass"?.85f:id==0?.62f:id==2?.6f:.32f);
                    mat.SetFloat("_SpecularStrength",tag=="Glass"?.65f:.32f);
                    mat.SetFloat("_Grime",id==1||id==4?.5f:id==0?.22f:.12f);
                    mat.SetColor("_EmissionColor",tag=="Glow"?profile.Accent*1.5f:tag=="Glass"?profile.Secondary*.15f:Color.black);
                    mat.SetTexture("_EmissionMap",Texture("White.png"));
                    if(tag=="Floor")
                    {
                        mat.SetTexture("_MainTex",Texture(id+"_Floor.png"));
                        mat.SetTexture("_BumpMap",Texture(id+"_Normal.png"));
                    }
                    if(tag=="Shell")
                    {
                        mat.SetTexture("_MainTex",Texture(id+"_Shell.png"));
                        mat.SetTexture("_BumpMap",Texture(id+"_ShellNormal.png"));
                    }
                    EditorUtility.SetDirty(mat);
                }
                foreach(string role in new[]{"Wall","WallB","Floor","Room","Feature","Service","Vent","Landmark","Detail","Volatile","Unstable"})
                {
                    var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Models/"+id+"_"+role+".fbx");
                    if(source==null)throw new System.InvalidOperationException("Missing biome model "+id+" "+role);
                    var model=Object.Instantiate(source);model.name=id+"_"+role;
                    foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>())
                    {
                        var imported=renderer.sharedMaterials;
                        for(int i=0;i<imported.Length;i++)
                        {
                            string tag=imported[i]!=null?imported[i].name.Split('.')[0]:"Shell";
                            if(role=="Floor"&&tag=="Shell")tag="Floor";
                            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/"+id+"_"+tag+".mat");
                            if(material==null)throw new System.InvalidOperationException("Unknown material slot "+tag);
                            imported[i]=material;
                        }
                        renderer.sharedMaterials=imported;
                    }
                    PrefabUtility.SaveAsPrefabAsset(model,Root+"Prefabs/"+id+"_"+role+".prefab");Object.DestroyImmediate(model);
                }
            }
            AssetDatabase.SaveAssets();
        }
        static Texture2D Texture(string name)
        {
            string path=Root+"Textures/"+name;
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            var type=name.Contains("Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
            if(importer!=null&&(importer.textureShape!=TextureImporterShape.Texture2D||importer.textureType!=type))
            {importer.textureShape=TextureImporterShape.Texture2D;importer.textureType=type;importer.SaveAndReimport();}
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(texture==null)throw new System.InvalidOperationException("Missing biome texture "+path);
            return texture;
        }
    }
}
