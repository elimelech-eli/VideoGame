using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace VideoGame.Editor {
public sealed class TransitEnemyImporter:AssetPostprocessor { public override int GetPostprocessOrder()=>100;
 void OnPreprocessModel(){if(!assetPath.Contains("/TransitEnemies/Models/"))return;var i=(ModelImporter)assetImporter;i.importCameras=i.importLights=i.importAnimation=false;i.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;i.animationType=ModelImporterAnimationType.Legacy;i.isReadable=true;}
 void OnPreprocessTexture(){if(!assetPath.Contains("/TransitEnemies/Textures/"))return;var i=(TextureImporter)assetImporter;i.maxTextureSize=2048;i.sRGBTexture=assetPath.Contains("Albedo")||assetPath.Contains("Emission");if(assetPath.Contains("Normal"))i.textureType=TextureImporterType.NormalMap;}
}
public static class TransitEnemyAssets {
 const string Root="Assets/Game/Resources/Detailed/TransitEnemies/";
 [Serializable] class Maps {public string Albedo,Normal,Metallic,Roughness,Emission;}
 [Serializable] class Surface {public string name,id;public bool glass;public float emissionStrength;public Maps maps;}
 [Serializable] class Model {public Surface[] materials;public int bones,triangles;}
 [Serializable] class Manifest {public Model TransitMelee,TransitGunner,TransitAoe,TransitHeavy;}
 [MenuItem("Nexus/Rebuild Transit enemies")] public static void Build(){
 var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(Root+"manifest.json"));
 Directory.CreateDirectory(Root+"Materials");Directory.CreateDirectory(Root+"Prefabs");AssetDatabase.Refresh();
 foreach(var id in new[]{"TransitMelee","TransitGunner","TransitAoe","TransitHeavy"}){
 var info=id=="TransitMelee"?data.TransitMelee:id=="TransitGunner"?data.TransitGunner:id=="TransitAoe"?data.TransitAoe:data.TransitHeavy;var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Models/"+id+".fbx"));model.name=id;
 foreach(var a in model.GetComponentsInChildren<Animator>())UnityEngine.Object.DestroyImmediate(a);
 foreach(var a in model.GetComponentsInChildren<Animation>())UnityEngine.Object.DestroyImmediate(a);
 // FBX keeps source material-slot names even when material import is disabled.
 var importer=(ModelImporter)AssetImporter.GetAtPath(Root+"Models/"+id+".fbx");
 // Assign using material names from a temporary material-enabled import if slots are absent.
 foreach(var renderer in model.GetComponentsInChildren<Renderer>()){
 var names=renderer.sharedMaterials.Select(m=>m==null?"":m.name).ToArray();
 var mesh=renderer is SkinnedMeshRenderer skin?skin.sharedMesh:renderer.GetComponent<MeshFilter>().sharedMesh;
 var mats=new Material[mesh.subMeshCount];
 for(int j=0;j<mats.Length;j++){
 var surface=info.materials.FirstOrDefault(m=>j<names.Length&&names[j].StartsWith(m.name,StringComparison.Ordinal));
 if(surface==null)throw new InvalidOperationException(id+" lost source material slot on "+renderer.name);
 string path=Root+"Materials/"+id+"_"+surface.id+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}
 mat.mainTexture=Tex(id,surface.maps.Albedo);mat.color=surface.glass?new Color(.8f,.9f,1,.22f):Color.white;
 if(!string.IsNullOrEmpty(surface.maps.Normal)){mat.SetTexture("_BumpMap",Tex(id,surface.maps.Normal));mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_BumpScale",.65f);}
 mat.SetTexture("_MetallicGlossMap",Tex(id,surface.id+"_MetalSmooth.png"));mat.EnableKeyword("_METALLICGLOSSMAP");mat.SetFloat("_GlossMapScale",1);
 if(surface.glass){mat.SetFloat("_Mode",3);mat.SetInt("_SrcBlend",1);mat.SetInt("_DstBlend",10);mat.SetInt("_ZWrite",0);mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");mat.renderQueue=3000;}
 if(!string.IsNullOrEmpty(surface.maps.Emission)){mat.SetTexture("_EmissionMap",Tex(id,surface.maps.Emission));mat.SetColor("_EmissionColor",Color.white*surface.emissionStrength);mat.EnableKeyword("_EMISSION");}
 mat.enableInstancing=true;EditorUtility.SetDirty(mat);mats[j]=mat;
 }
 renderer.sharedMaterials=mats;
 }
 if(id!="TransitMelee")BiomeCombatClips.Prepare(model,id);
 PrefabUtility.SaveAsPrefabAsset(model,Root+"Prefabs/"+id+".prefab");BiomeCombatClips.Bake(model,id);UnityEngine.Object.DestroyImmediate(model);
 }AssetDatabase.SaveAssets();
 }
 static Texture2D Tex(string id,string file)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/"+id+"/"+file);
}
}
