using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace VideoGame.Editor {
public sealed class LabEnemyImporter:AssetPostprocessor { public override int GetPostprocessOrder()=>100;
 void OnPreprocessModel(){if(!assetPath.Contains("/LabEnemies/Models/"))return;var i=(ModelImporter)assetImporter;i.importCameras=i.importLights=i.importAnimation=false;i.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;i.animationType=ModelImporterAnimationType.Legacy;i.isReadable=true;}
 void OnPreprocessTexture(){if(!assetPath.Contains("/LabEnemies/Textures/"))return;var i=(TextureImporter)assetImporter;i.maxTextureSize=2048;i.sRGBTexture=assetPath.Contains("Albedo");if(assetPath.Contains("Normal"))i.textureType=TextureImporterType.NormalMap;}
}
public static class LabEnemyAssets {
 const string Root="Assets/Game/Resources/Detailed/LabEnemies/";
 [Serializable] class Maps {public string Albedo,Normal,Metallic,Roughness;}
 [Serializable] class Surface {public string name,id;public bool glass;public Maps maps;}
 [Serializable] class Model {public Surface[] materials;public int bones,triangles;}
 [Serializable] class Manifest {public Model LabSpider,LabHazmat,LabAoe,LabHeavy,LabBoss;}
 [MenuItem("Nexus/Rebuild Lab enemies")] public static void Build(){
 var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(Root+"manifest.json"));
 Directory.CreateDirectory(Root+"Materials");Directory.CreateDirectory(Root+"Prefabs");AssetDatabase.Refresh();
 foreach(var id in new[]{"LabSpider","LabHazmat","LabAoe","LabHeavy","LabBoss"}){
 var info=id=="LabSpider"?data.LabSpider:id=="LabHazmat"?data.LabHazmat:id=="LabAoe"?data.LabAoe:id=="LabHeavy"?data.LabHeavy:data.LabBoss;var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Models/"+id+".fbx"));model.name=id;
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
 mat.enableInstancing=true;EditorUtility.SetDirty(mat);mats[j]=mat;
 }
 renderer.sharedMaterials=mats;
 }
 if(id!="LabSpider"&&id!="LabHazmat")LabCombatClips.Prepare(model,id);
 PrefabUtility.SaveAsPrefabAsset(model,Root+"Prefabs/"+id+".prefab");BakeClips(model,id);UnityEngine.Object.DestroyImmediate(model);
 }AssetDatabase.SaveAssets();
 }
 static Texture2D Tex(string id,string file)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/"+id+"/"+file);
 static void BakeClips(GameObject model,string id){
 if(id!="LabSpider"&&id!="LabHazmat"){LabCombatClips.Bake(model,id);return;}
 string folder="Assets/Game/Resources/LabEnemyAnimations/"+id+"/";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
 var bones=model.GetComponentsInChildren<Transform>().Where(t=>id=="LabSpider"?(t.name=="body"||t.name.StartsWith("leg_")||t.name.StartsWith("foot_")):new[]{"Hips","Spine","Spine1","Spine2","Head","L_up_leg","R_up_leg","L_knee","R_knee","L_foot","R_foot","L_shoulder","R_shoulder"}.Contains(t.name)).ToArray();
 foreach(string state in ActorAnimationBuilder.Names){
 float length=state=="Death"?1.25f:state=="Melee"?.65f:state=="Hit"?.2f:state=="Fire"?.45f:state=="Cast"?.8f:.8f;
 var clip=new AnimationClip{name=state,legacy=true,frameRate=30,wrapMode=state=="Idle"||state.StartsWith("Run")?WrapMode.Loop:WrapMode.ClampForever};
 foreach(var bone in bones){var rest=bone.localRotation;var pos=bone.localPosition;var q=new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};var px=new AnimationCurve();var py=new AnimationCurve();var pz=new AnimationCurve();
 for(int frame=0;frame<=24;frame++){float u=frame/24f;float wave=Mathf.Sin(u*Mathf.PI*2);float x=0,y=0,z=0,bob=0;bool run=state.StartsWith("Run")||state=="Dash";float strike=Mathf.Sin(Mathf.PI*u);
 if(id=="LabSpider"){
 if(bone.name=="body"){bob=(run?.018f:.004f)*wave;if(state=="Melee"){x=-18*strike;bob=.06f*strike;}if(state=="Death"){z=85*u;bob=-.18f*u;}if(state=="Hit")x=10*strike;}
 else {bool foot=bone.name.StartsWith("foot_");int j=int.Parse(bone.name.Substring(bone.name.LastIndexOf('_')+1));float sign=bone.name.Contains("_-1_")?-1:1;float w=Mathf.Sin(u*Mathf.PI*2+((j+(sign>0?1:0))%2)*Mathf.PI);
 if(run){x=(foot?24:16)*w;y=(foot?0:18)*w*sign;}if(state=="Melee")z=(foot?30:-25)*strike*sign;if(state=="Death")z=(foot?-65:35)*u*sign;
 }}else{
 float side=bone.name.StartsWith("L_")?1:-1;
 if(bone.name=="Hips"){bob=(run?.018f:.003f)*wave;if(state=="Death"){bob=-.75f*u;x=90*u;}}
 if(bone.name.EndsWith("up_leg")&&run)x=27*wave*side;
 if(bone.name.EndsWith("knee")&&run)x=-38*Mathf.Max(0,wave*side);
 if(bone.name.EndsWith("foot")&&run)x=-10*wave*side;
 if(bone.name=="Spine"||bone.name=="Spine1"||bone.name=="Spine2"){x=(run?1.4f:.4f)*wave;if(state=="Fire")x-=5*Mathf.Sin(Mathf.PI*Mathf.Clamp01(u/.35f));if(state=="Hit")z=8*strike;if(state=="Death")x=18*u;}
 if(bone.name.EndsWith("shoulder")&&run)z=1.3f*wave*side;
 }
 var rot=rest*Quaternion.Euler(x,y,z);q[0].AddKey(u*length,rot.x);q[1].AddKey(u*length,rot.y);q[2].AddKey(u*length,rot.z);q[3].AddKey(u*length,rot.w);var offset=bone.parent.InverseTransformVector(Vector3.up*bob);px.AddKey(u*length,pos.x+offset.x);py.AddKey(u*length,pos.y+offset.y);pz.AddKey(u*length,pos.z+offset.z);
 }
 string path=AnimationUtility.CalculateTransformPath(bone,model.transform);for(int k=0;k<4;k++)clip.SetCurve(path,typeof(Transform),"localRotation."+"xyzw"[k],q[k]);clip.SetCurve(path,typeof(Transform),"localPosition.x",px);clip.SetCurve(path,typeof(Transform),"localPosition.y",py);clip.SetCurve(path,typeof(Transform),"localPosition.z",pz);
 }clip.EnsureQuaternionContinuity();string dest=folder+state+".anim";var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(dest);if(old==null)AssetDatabase.CreateAsset(clip,dest);else{EditorUtility.CopySerialized(clip,old);UnityEngine.Object.DestroyImmediate(clip);}
 }
 }
}
}
