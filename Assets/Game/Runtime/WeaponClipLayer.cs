using System.Collections.Generic;
using UnityEngine;

namespace VideoGame
{
    // Masked sampling of baked weapon clips over the live locomotion pose.
    // No root transform curves and no animation-driven gameplay events.
    public sealed class WeaponClipLayer
    {
        readonly GameObject model;
        readonly Transform[] bones;
        readonly Quaternion[] rotations;
        readonly Vector3[] positions;
        readonly Quaternion[] sampledWorld;
        readonly int[] armSide;
        readonly bool[] lowerBody;
        readonly Dictionary<string,AnimationClip> clips=new Dictionary<string,AnimationClip>();
        public WeaponClipLayer(GameObject model)
        {
            this.model=model;var list=new List<Transform>();
            foreach(var t in model.GetComponentsInChildren<Transform>())
                if(t!=model.transform)list.Add(t);
            bones=list.ToArray();rotations=new Quaternion[bones.Length];positions=new Vector3[bones.Length];sampledWorld=new Quaternion[bones.Length];
            armSide=new int[bones.Length];lowerBody=new bool[bones.Length];
            for(int i=0;i<bones.Length;i++)
            {
                armSide[i]=-1;lowerBody[i]=bones[i].name=="hips";
                for(var t=bones[i];t!=null&&t!=model.transform;t=t.parent)
                {
                    string n=t.name;
                    if(n.StartsWith("upper_arm_")||n.StartsWith("forearm_")||n.StartsWith("hand_")||n.StartsWith("clavicle_"))armSide[i]=n.EndsWith("_R")?0:1;
                    if(n.StartsWith("thigh_")||n.StartsWith("shin_")||n.StartsWith("foot_"))lowerBody[i]=true;
                }
            }
            foreach(var clip in Resources.LoadAll<AnimationClip>(model.GetComponent<PlayerRig>()?.WeaponAnimationFolder??"WeaponAnimations"))clips[clip.name]=clip;
            if(!clips.ContainsKey("SwordR"))throw new System.InvalidOperationException("Missing authored weapon animation clips");
        }
        public void Apply(string name,float time,float weight,int arm,bool body,bool grounded)
        {
            if(weight<=0 || time<0)return;
            for(int i=0;i<bones.Length;i++){rotations[i]=bones[i].localRotation;positions[i]=bones[i].localPosition;}
            clips[name].SampleAnimation(model,Mathf.Min(time,clips[name].length));
            for(int i=0;i<bones.Length;i++)sampledWorld[i]=bones[i].rotation;
            for(int i=0;i<bones.Length;i++)
            {
                bool include=armSide[i]>=0?armSide[i]==arm:body&&(!lowerBody[i]||grounded);
                float w=include?weight:0;
                bones[i].localRotation=Quaternion.Slerp(rotations[i],bones[i].localRotation,w);
                bones[i].localPosition=Vector3.Lerp(positions[i],bones[i].localPosition,w);
            }
            // An independent off-hand clip was authored with its own torso turn.
            // Preserve its actor-space arm directions when the other hand owns
            // the actual torso; copying local rotations would twist the cut twice.
            if(!body && arm>=0)for(int i=0;i<bones.Length;i++)
            {
                if(armSide[i]==arm)
                    bones[i].rotation=Quaternion.Slerp(bones[i].rotation,sampledWorld[i],weight);
            }
        }
    }
}
