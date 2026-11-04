using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace VideoGame.Editor {
public static class CorporateEnemyClips {
 public static void Bake(GameObject model,string id,Bounds bounds){
  string folder="Assets/Game/Resources/CorporateEnemyAnimations/"+id+"/";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
  bool dog=id=="CorporateMelee",hover=id=="CorporateAoe";
  var bones=model.GetComponentsInChildren<Transform>().Where(t=>t.name=="motion.root"||new[]{"hips","spine","head","tail"}.Contains(t.name)||new[]{"thigh.","calf.","foot.","upperarm.","forearm.","panel.","cannon.lower."}.Any(p=>t.name.StartsWith(p,StringComparison.Ordinal))).ToArray();
  foreach(string state in ActorAnimationBuilder.Names){
   float length=state=="Death"?1.25f:state=="Melee"?.65f:state=="Hit"?.2f:state=="Fire"?.45f:state=="Cast"?.8f:state.StartsWith("Run",StringComparison.Ordinal)?dog?.28f:hover?.8f:id=="CorporateHeavy"?.65f:.4f:.8f;
   bool loop=state=="Idle"||state.StartsWith("Run",StringComparison.Ordinal);var clip=new AnimationClip{name=state,legacy=true,frameRate=30,wrapMode=loop?WrapMode.Loop:WrapMode.ClampForever};
   foreach(var bone in bones){var rest=bone.localRotation;var pos=bone.localPosition;string role=bone.name;var q=Enumerable.Range(0,4).Select(_=>new AnimationCurve()).ToArray();var c=Enumerable.Range(0,3).Select(_=>new AnimationCurve()).ToArray();
    for(int f=0;f<=32;f++){float u=f/32f,w=Mathf.Sin(u*Mathf.PI*2),strike=Mathf.Sin(u*Mathf.PI),side=role.EndsWith("L",StringComparison.Ordinal)?1:-1;bool run=state.StartsWith("Run",StringComparison.Ordinal)||state=="Dash";float phase=dog&&role.Contains(".B")?-side:side,stride=w*phase*(state=="RunBack"?-1:1);float x=0,y=0,z=0,bob=0,back=0;
     if(role=="motion.root"){bob=run?.018f*Mathf.Sin(u*Mathf.PI*4):.003f*w;if(hover){bob=.03f*w;x=run?3*w:1*w;z=1.2f*w;}}
     if(role=="spine"){x=run?1.8f*w:.35f*w;y=run?1.6f*w:0;}
     if(role=="head")y=(run?2:.6f)*w;
     if(role=="tail")y=(run?6:2)*w;
     if(run&&!hover){if(role.StartsWith("thigh."))x=(dog?18:21)*stride;if(role.StartsWith("calf."))x=(dog?25:30)*Mathf.Max(0,stride);if(role.StartsWith("foot."))x=-(dog?18:21)*stride-(dog?25:30)*Mathf.Max(0,stride);if(role.StartsWith("upperarm.")){x=1.2f*w*side;z=.8f*w;}if(role.StartsWith("forearm."))x=1.3f*w*side;if((state=="RunLeft"||state=="RunRight")&&role.StartsWith("thigh.")){z=6*stride;x*=.5f;}}
     if(state=="Fire"){float kick=Mathf.Sin(Mathf.PI*Mathf.Clamp01(u/.35f));if(role.StartsWith("forearm.")||role.StartsWith("cannon.lower."))x-=4*kick;if(role=="spine")x+=1.5f*kick;if(role=="motion.root")back=-.02f*kick;}
     if(state=="Melee"||state=="Cast"){if(role.StartsWith("upperarm."))x-=14*strike;if(role.StartsWith("forearm."))x-=8*strike;if(role.StartsWith("panel."))x-=12*strike;if(role=="head")x-=(dog?20:5)*strike;if(role=="spine")x-=(dog?6:2)*strike;if(role=="motion.root")x-=(dog?6:1)*strike;}
     if(state=="Hit"){if(role=="spine")z=5*strike;if(role=="head")x=-6*strike;if(role=="motion.root")back=-.025f*strike;}
     if(state=="Death"){float fall=u*u*(3-2*u);if(role=="motion.root"){z=90*fall;bob=(bounds.size.x-bounds.size.y)*.5f*fall;}if(role=="head")x=10*fall;if(role.StartsWith("calf."))x=12*fall;if(role.StartsWith("panel."))x=18*side*fall;}
     Quaternion delta=Quaternion.Inverse(bone.parent.rotation)*Quaternion.Euler(x,y,z)*bone.parent.rotation;Quaternion rotation=delta*rest;Vector3 offset=bone.parent.InverseTransformVector(Vector3.up*bob+Vector3.forward*back);
     for(int k=0;k<4;k++)q[k].AddKey(u*length,rotation[k]);for(int k=0;k<3;k++)c[k].AddKey(u*length,pos[k]+offset[k]);
    }
    string path=AnimationUtility.CalculateTransformPath(bone,model.transform);for(int k=0;k<4;k++)clip.SetCurve(path,typeof(Transform),"localRotation."+"xyzw"[k],q[k]);for(int k=0;k<3;k++)clip.SetCurve(path,typeof(Transform),"localPosition."+"xyz"[k],c[k]);
   }
   clip.EnsureQuaternionContinuity();string dest=folder+state+".anim";var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(dest);if(!old)AssetDatabase.CreateAsset(clip,dest);else{EditorUtility.CopySerialized(clip,old);UnityEngine.Object.DestroyImmediate(clip);}
  }
 }
}}
