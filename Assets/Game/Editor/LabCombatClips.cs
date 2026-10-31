using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace VideoGame.Editor
{
    public static class LabCombatClips
    {
        static string Role(string name)
        {
            if(name=="Groin")return "hips";
            if(name=="Body")return "spine";
            if(name=="Head")return "head";
            foreach(var side in new[]{"L","R"})
            {
                if(name=="Leg1."+side)return "thigh."+side;
                if(name=="Leg2."+side)return "calf."+side;
                if(name=="Foot."+side)return "foot."+side;
                if(name=="Arm1."+side)return "upperarm."+side;
                if(name=="Arm2."+side)return "forearm."+side;
                if(name=="Hand."+side)return "hand."+side;
            }
            return name;
        }
        static bool Driven(string name)=>new[]{"hips","spine","head"}.Contains(Role(name)) || new[]{"thigh.","calf.","foot.","upperarm.","forearm.","hand."}.Any(p=>Role(name).StartsWith(p,StringComparison.Ordinal));
        public static void Prepare(GameObject model,string id)
        {
            var hand=model.GetComponentsInChildren<Transform>().First(t=>Role(t.name)=="hand.R");
            Vector3 point=hand.position+Vector3.forward*(id=="LabHeavy"?.55f:.22f);
            var gun=model.GetComponentsInChildren<Renderer>().FirstOrDefault(r=>r.name.Contains(id=="LabHeavy"?"weaponhnad":"DecontaminationEmitter2"));
            if(gun!=null)point=new Vector3(gun.bounds.center.x,gun.bounds.center.y,gun.bounds.max.z);
            var socket=new GameObject("ShotOrigin").transform;socket.SetParent(hand,false);socket.position=point;
        }
        public static void Bake(GameObject model,string id)
        {
            string folder="Assets/Game/Resources/LabEnemyAnimations/"+id+"/";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var bones=model.GetComponentsInChildren<Transform>().Where(t=>Driven(t.name)&&t.GetComponent<Renderer>()==null).ToArray();
            foreach(string state in ActorAnimationBuilder.Names)
            {
                float length=state=="Death"?1.25f:state=="Melee"?.65f:state=="Hit"?.2f:state=="Fire"?.45f:state=="Cast"?.8f:.8f;
                var clip=new AnimationClip{name=state,legacy=true,frameRate=30,wrapMode=state=="Idle"||state.StartsWith("Run")?WrapMode.Loop:WrapMode.ClampForever};
                foreach(var bone in bones)
                {
                    string role=Role(bone.name);var rest=bone.localRotation;var pos=bone.localPosition;
                    var q=Enumerable.Range(0,4).Select(_=>new AnimationCurve()).ToArray();var c=Enumerable.Range(0,3).Select(_=>new AnimationCurve()).ToArray();
                    for(int frame=0;frame<=32;frame++)
                    {
                        float u=frame/32f, wave=Mathf.Sin(u*Mathf.PI*2),strike=Mathf.Sin(u*Mathf.PI),side=role.EndsWith(".L",StringComparison.Ordinal)?1:-1;
                        bool run=state.StartsWith("Run")||state=="Dash";float stride=wave*side*(state=="RunBack"?-1:1),x=0,y=0,z=0,bob=0,back=0;
                        if(role=="hips") {bob=run?.025f*Mathf.Sin(u*Mathf.PI*4):.003f*wave;if(run)z=1.5f*wave;}
                        if(role=="spine"){x=run?3+1.5f*wave:.6f*wave;y=run?2*wave:0;}
                        if(role=="head"){x=run?-1.5f:0;y=1.3f*wave;}
                        if(run)
                        {
                            if(role.StartsWith("thigh."))x=24*stride;
                            if(role.StartsWith("calf."))x=38*Mathf.Max(0,stride);
                            if(role.StartsWith("foot."))x=-24*stride-38*Mathf.Max(0,stride);
                            if(role.StartsWith("upperarm.")){x=1.5f*wave*side;z=1.5f*wave;}
                            if(role.StartsWith("forearm."))x=2*wave*side;
                            if((state=="RunLeft"||state=="RunRight")&&role.StartsWith("thigh.")){z=9*stride;x*=.45f;}
                        }
                        if(state=="Fire")
                        {
                            float kick=Mathf.Sin(Mathf.PI*Mathf.Clamp01(u/.35f));
                            if(role=="forearm.R")x-=7*kick;
                            if(role=="upperarm.R")x-=2*kick;
                            if(role=="spine")x+=2*kick;
                            if(role=="hips")back=-.025f*kick;
                        }
                        if(state=="Cast"||state=="Melee")
                        {
                            if(role.StartsWith("upperarm."))x-=(id=="LabBoss"?38:role.EndsWith(".R",StringComparison.Ordinal)?28:12)*strike;
                            if(role.StartsWith("forearm."))x-=15*strike;
                            if(role=="head")x-=6*strike;
                            if(role=="spine")x-=3*strike;
                        }
                        if(state=="Hit"){if(role=="spine")z=6*strike;if(role=="head")x=-8*strike;}
                        if(state=="Death")
                        {
                            float fall=u*u*(3-2*u);
                            if(role=="hips"){z=78*fall;bob=-.6f*fall;}
                            if(role=="spine")x=15*fall;
                            if(role=="head")x=25*fall;
                            if(role.StartsWith("upperarm."))z=18*side*fall;
                            if(role.StartsWith("thigh."))x=-22*fall;
                            if(role.StartsWith("calf."))x=45*fall;
                            if(role.StartsWith("foot."))x=-23*fall;
                        }
                        // Deltas are authored in model-world axes; source bones retain their original local axes.
                        Quaternion delta=Quaternion.Inverse(bone.parent.rotation)*Quaternion.Euler(x,y,z)*bone.parent.rotation;
                        Quaternion rot=delta*rest;Vector3 offset=bone.parent.InverseTransformVector(Vector3.up*bob+Vector3.forward*back);
                        for(int k=0;k<4;k++)q[k].AddKey(u*length,rot[k]);
                        for(int k=0;k<3;k++)c[k].AddKey(u*length,pos[k]+offset[k]);
                    }
                    string path=AnimationUtility.CalculateTransformPath(bone,model.transform);
                    for(int k=0;k<4;k++)clip.SetCurve(path,typeof(Transform),"localRotation."+"xyzw"[k],q[k]);
                    for(int k=0;k<3;k++)clip.SetCurve(path,typeof(Transform),"localPosition."+"xyz"[k],c[k]);
                }
                clip.EnsureQuaternionContinuity();string dest=folder+state+".anim";var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(dest);
                if(old==null)AssetDatabase.CreateAsset(clip,dest);else{EditorUtility.CopySerialized(clip,old);UnityEngine.Object.DestroyImmediate(clip);}
            }
        }
    }
}
