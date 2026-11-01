using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace VideoGame.Editor {
public sealed class FactoryEnemyImporter:AssetPostprocessor { public override int GetPostprocessOrder()=>100;
 void OnPreprocessModel(){if(!assetPath.Contains("/FactoryEnemies/Models/"))return;var i=(ModelImporter)assetImporter;i.importCameras=i.importLights=i.importAnimation=false;i.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;i.animationType=ModelImporterAnimationType.Legacy;i.isReadable=true;}
 void OnPreprocessTexture(){if(!assetPath.Contains("/FactoryEnemies/Textures/"))return;var i=(TextureImporter)assetImporter;i.maxTextureSize=2048;i.sRGBTexture=assetPath.Contains("Albedo")||assetPath.Contains("Emission");if(assetPath.Contains("Normal"))i.textureType=TextureImporterType.NormalMap;}
}
public static class FactoryEnemyAssets {
 const string Root="Assets/Game/Resources/Detailed/FactoryEnemies/";
 [Serializable] class Maps {public string Albedo,Normal,Metallic,Roughness,Emission;}
 [Serializable] class Surface {public string name,id;public bool glass;public float emissionStrength;public Maps maps;}
 [Serializable] class Model {public Surface[] materials;public int bones,triangles;}
 [Serializable] class Manifest {public Model FactoryMelee,FactoryGunner,FactoryAoe,FactoryHeavy;}
 [MenuItem("Nexus/Rebuild Factory enemies")] public static void Build(){
 var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(Root+"manifest.json"));
 Directory.CreateDirectory(Root+"Materials");Directory.CreateDirectory(Root+"Prefabs");AssetDatabase.Refresh();
 foreach(var id in new[]{"FactoryHeavy","FactoryMelee","FactoryGunner","FactoryAoe"}){
 var info=id=="FactoryMelee"?data.FactoryMelee:id=="FactoryGunner"?data.FactoryGunner:id=="FactoryAoe"?data.FactoryAoe:data.FactoryHeavy;var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Models/"+id+".fbx"));model.name=id;
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
FactorySocket(model,id);
 PrefabUtility.SaveAsPrefabAsset(model,Root+"Prefabs/"+id+".prefab");BiomeCombatClips.Bake(model,id);UnityEngine.Object.DestroyImmediate(model);
 }AssetDatabase.SaveAssets();
 }
 [MenuItem("Nexus/Factory enemy proof")] public static void Proof(){
 Build();UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/FactoryReview/FactoryRoomAudition.unity");
 var model=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Detailed/FactoryEnemies/Prefabs/FactoryHeavy"));model.name="FactoryHeavy proof";
 Resources.Load<AnimationClip>("FactoryEnemyAnimations/FactoryHeavy/Idle").SampleAnimation(model,0);var b=VideoGame.Art.RenderedBounds(model);model.transform.localScale*=2.5f/b.size.y;
 b=VideoGame.Art.RenderedBounds(model);model.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
 var clip=Resources.Load<AnimationClip>("FactoryEnemyAnimations/FactoryHeavy/RunForward");clip.SampleAnimation(model,.2f);foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){skin.updateWhenOffscreen=true;var mesh=new Mesh();skin.BakeMesh(mesh);Debug.Log("HEAVY_BAKE "+skin.transform.TransformPoint(mesh.bounds.center)+" scale="+skin.transform.lossyScale+" worldsize="+skin.transform.TransformVector(mesh.bounds.size)+" size="+mesh.bounds.size+" enabled="+skin.enabled+" active="+skin.gameObject.activeInHierarchy+" root="+model.transform.position);UnityEngine.Object.DestroyImmediate(mesh);}
 var cam=UnityEngine.Object.FindFirstObjectByType<Camera>();cam.orthographic=true;cam.orthographicSize=3.5f;cam.transform.rotation=Quaternion.Euler(50,35,0);cam.transform.position=new Vector3(0,1,0)-cam.transform.forward*20;
 Directory.CreateDirectory("Art/FactoryIntake/Enemies");var rt=new RenderTexture(1200,1000,24){antiAliasing=4};cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var image=new Texture2D(1200,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,1000),0,0);image.Apply();File.WriteAllBytes("Art/FactoryIntake/Enemies/heavy-unity-proof.png",image.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;rt.Release();UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
 Debug.Log("FACTORY_ENEMY_PROOF_RENDERED: "+b.size+" "+clip.name+" curves="+AnimationUtility.GetCurveBindings(clip).Length);
 }
 static void FactorySocket(GameObject model,string id){var hand=model.GetComponentsInChildren<Transform>().First(t=>t.name==(id=="FactoryAoe"?"CC_Base_Spine02":"hand.R"));var socket=new GameObject("ShotOrigin").transform;socket.SetParent(hand,false);socket.position=hand.position+Vector3.forward*(id=="FactoryHeavy"?.95f:.4f);}
 static Texture2D Tex(string id,string file)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/"+id+"/"+file);
}
}
