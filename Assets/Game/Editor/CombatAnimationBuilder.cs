using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VideoGame.Editor
{
    // Rig-specific anatomy keys, baked once. Runtime consumes normal clips;
    // no runtime hand-target interpolation drives a sword attack.
    public static class CombatAnimationBuilder
    {
        [MenuItem("Nexus/Rebuild weapon combat clips")]
        public static void Build()
            => BuildFor(OperativeV03Assets.Prefab,"OperatorAnimations", "Assets/Game/Resources/WeaponAnimations/");
        public static void BuildFor(string prefab,string animations,string folder)
        {
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            foreach(string name in new[]{"SwordR","SwordL","HandgunKick","RifleKick","ShieldGuard","ShieldBlock","RifleBrace"})
            {
                var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefab));
                var rig=new Rig(model,animations);
                float duration=name.StartsWith("Sword")?PlayerWeaponAnimation.SwingDuration:name=="ShieldGuard"||name=="RifleBrace"?1:.22f;
                var clip=new AnimationClip{name=name,legacy=true,frameRate=60,wrapMode=WrapMode.ClampForever};
                var curves=new Dictionary<Transform,AnimationCurve[]>();
                foreach(var t in rig.Bones.Values)curves[t]=new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};
                int frames=Mathf.CeilToInt(duration*60);
                for(int n=0;n<=frames;n++)
                {
                    float time=duration*n/frames;rig.Pose(name,time,duration);
                    foreach(var pair in curves)
                    {
                        var q=pair.Key.localRotation;var p=pair.Key.localPosition;
                        float[] values={q.x,q.y,q.z,q.w,p.x,p.y,p.z};
                        for(int i=0;i<7;i++)pair.Value[i].AddKey(time,values[i]);
                    }
                }
                string[] properties={"localRotation.x","localRotation.y","localRotation.z","localRotation.w","localPosition.x","localPosition.y","localPosition.z"};
                foreach(var pair in curves)for(int i=0;i<7;i++)
                {
                    for(int k=0;k<pair.Value[i].length;k++)
                    {AnimationUtility.SetKeyLeftTangentMode(pair.Value[i],k,AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetKeyRightTangentMode(pair.Value[i],k,AnimationUtility.TangentMode.ClampedAuto);}
                    clip.SetCurve(AnimationUtility.CalculateTransformPath(pair.Key,model.transform),typeof(Transform),properties[i],pair.Value[i]);
                }
                clip.EnsureQuaternionContinuity();string path=folder+name+".anim";
                var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(existing==null)AssetDatabase.CreateAsset(clip,path);else{EditorUtility.CopySerialized(clip,existing);Object.DestroyImmediate(clip);}
                Object.DestroyImmediate(model);
            }
            AssetDatabase.SaveAssets();
        }
        sealed class Rig
        {
            public readonly Dictionary<string,Transform> Bones=new Dictionary<string,Transform>();
            readonly GameObject model;
            readonly AnimationClip idle;
            readonly Quaternion grip;
            readonly float height;
            public Rig(GameObject model,string animations)
            {
                this.model=model;idle=Resources.Load<AnimationClip>(animations+"/Idle");
                foreach(var t in model.GetComponentsInChildren<Transform>())
                    if(t.name=="hips"||t.name=="spine"||t.name=="spine_mid"||t.name=="chest"||t.name=="head"||t.name.StartsWith("thigh_")||t.name.StartsWith("shin_")||t.name.StartsWith("foot_")||t.name.StartsWith("upper_arm_")||t.name.StartsWith("forearm_")||t.name.StartsWith("hand_"))Bones[t.name]=t;
                idle.SampleAnimation(model,0);height=Vector3.Distance(Bones["hips"].position,Bones["foot_L"].position);
                foreach(var t in model.GetComponentsInChildren<Transform>())if(t.name=="SOCKET_weapon_R")grip=t.localRotation;
            }
            void Turn(string bone,Vector3 axis,float angle)=>Bones[bone].rotation=Quaternion.AngleAxis(angle,axis)*Bones[bone].rotation;
            static float Key(float t,float[] values)
            {
                float[] times={0,.12f,.22f,.34f,.49f,.68f};
                for(int n=1;n<times.Length;n++)if(t<=times[n])return Mathf.Lerp(values[n-1],values[n],Mathf.SmoothStep(0,1,Mathf.InverseLerp(times[n-1],times[n],t)));
                return values[values.Length-1];
            }
            public void Pose(string name,float time,float duration)
            {
                idle.SampleAnimation(model,0);
                Vector3 left=Bones["foot_L"].position,right=Bones["foot_R"].position;
                Quaternion lrot=Bones["foot_L"].rotation,rrot=Bones["foot_R"].rotation;
                if(name.StartsWith("Sword"))
                {
                    float sign=name=="SwordR"?1:-1;
                    // Pelvis turns ahead of the chest; recovery continues past contact.
                    left+=height*new Vector3(-.10f,0,.16f);right+=height*new Vector3(.10f,0,-.13f);
                    Bones["hips"].position+=Vector3.down*height*.095f;
                    float hips=Key(Mathf.Min(time+.025f,duration),new float[]{0,-24,26,32,12,0})*sign;
                    float chest=Key(time,new float[]{0,-25,26,30,10,0})*sign;
                    Turn("hips",Vector3.up,hips);Turn("spine_mid",Vector3.up,chest*.65f);Turn("chest",Vector3.up,chest*.35f);
                    Turn("spine",Vector3.right,Key(time,new float[]{0,-3,9,12,5,0}));
                    Turn("head",Vector3.up,-(hips+chest)*.7f);
                    Bones["hips"].position+=height*new Vector3(sign*Key(time,new float[]{0,.025f,-.02f,-.035f,-.01f,0}),Key(time,new float[]{0,-.025f,-.065f,-.055f,-.02f,0}),Key(time,new float[]{0,-.025f,.065f,.08f,.035f,0}));
                    string side=sign>0?"R":"L";
                    // Anatomy directions: shoulder -> elbow -> wrist. Directions
                    // arc about joints, with a flexed elbow throughout the cut.
                    Vector3[] u={new Vector3(.4f,-.8f,.42f),new Vector3(.65f,-.35f,.45f),new Vector3(.3f,-.4f,.85f),new Vector3(-.1f,-.96f,.26f),new Vector3(.05f,-.9f,.4f),new Vector3(.4f,-.8f,.42f)};
                    Vector3[] f={new Vector3(-.1f,.15f,.98f),new Vector3(.05f,.82f,.55f),new Vector3(-.2f,-.25f,.95f),new Vector3(-.65f,-.60f,.45f),new Vector3(-.55f,.0f,.84f),new Vector3(-.1f,.15f,.98f)};
                    Vector3[] blade={new Vector3(.2f,.87f,.45f),new Vector3(.2f,.91f,-.35f),new Vector3(-.6f,-.3f,.74f),new Vector3(-.65f,-.7f,.3f),new Vector3(-.35f,.6f,.72f),new Vector3(.2f,.87f,.45f)};
                    Vector3 upper=Direction(time,u),fore=Direction(time,f),b=Direction(time,blade);
                    upper.x*=sign;fore.x*=sign;b.x*=sign;
                    if(side=="L"){upper.y-=.12f;fore.y-=.12f;}
                    Aim("upper_arm_"+side,"forearm_"+side,upper);Aim("forearm_"+side,"hand_"+side,fore);
                    Bones["hand_"+side].rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.forward,b).normalized,b)*Quaternion.Inverse(grip);
                }
                else
                {
                    float pulse=Mathf.Sin(Mathf.Clamp01(time/duration)*Mathf.PI);
                    float hold=time/duration;
                    if(name=="ShieldGuard"||name=="RifleBrace")
                    {Turn("hips",Vector3.up,hold*(name=="ShieldGuard"?18:8));Turn("chest",Vector3.up,-hold*12);Turn("spine",Vector3.right,hold*8);Bones["hips"].position+=Vector3.down*height*.035f*hold;}
                    else if(name=="ShieldBlock")
                    {Turn("chest",Vector3.up,-pulse*10);Turn("spine",Vector3.right,-pulse*7);Bones["hips"].position+=Vector3.back*height*.018f*pulse;}
                    else {Turn("chest",Vector3.right,-pulse*(name=="RifleKick"?4:2));Turn("spine",Vector3.right,-pulse*1.5f);}
                }
                // Maintain both floor contacts as the pelvis shifts. The runtime
                // masks these leg keys out while locomotion owns the feet.
                Leg("L",left);Leg("R",right);Bones["foot_L"].rotation=lrot;Bones["foot_R"].rotation=rrot;
            }
            static Vector3 Direction(float t,Vector3[] values)
            {
                float[] times={0,.12f,.22f,.34f,.49f,.68f};
                for(int n=1;n<times.Length;n++)if(t<=times[n])return Vector3.Slerp(values[n-1].normalized,values[n].normalized,Mathf.SmoothStep(0,1,Mathf.InverseLerp(times[n-1],times[n],t)));
                return values[5].normalized;
            }
            void Aim(string a,string b,Vector3 direction){var t=Bones[a];t.rotation=Quaternion.FromToRotation(Bones[b].position-t.position,direction)*t.rotation;}
            void Leg(string side,Vector3 target)
            {
                var hip=Bones["thigh_"+side];var knee=Bones["shin_"+side];var foot=Bones["foot_"+side];
                float a=Vector3.Distance(hip.position,knee.position),b=Vector3.Distance(knee.position,foot.position);
                Vector3 ray=target-hip.position;float d=Mathf.Clamp(ray.magnitude,.01f,a+b-.0001f);Vector3 axis=ray.normalized;
                float along=(a*a-b*b+d*d)/(2*d);Vector3 bend=Vector3.ProjectOnPlane(Vector3.forward,axis).normalized;
                Vector3 point=hip.position+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
                Aim("thigh_"+side,"shin_"+side,point-hip.position);Aim("shin_"+side,"foot_"+side,target-knee.position);
            }
        }
    }
}
