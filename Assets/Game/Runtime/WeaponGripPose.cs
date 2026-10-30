using System.Collections.Generic;
using UnityEngine;
using VideoGame.Core;

namespace VideoGame
{
    // Curl toward the actual handle using measured bone directions. Imported
    // finger axes differ, so do not assume a universal local Euler curl axis.
    public sealed class WeaponGripPose
    {
        readonly Transform[,,] joints=new Transform[2,5,3];
        readonly Quaternion[,,] rest=new Quaternion[2,5,3];
        readonly Transform[] hands=new Transform[2];
        readonly GameObject model;
        readonly Dictionary<string,AnimationClip> native=new Dictionary<string,AnimationClip>();
        static readonly string[] Names={"index","middle","ring","pinky","thumb"};
        static readonly float[] Fist={35,65,45},Trigger={25,45,30},Thumb={25,35,25};
        public WeaponGripPose(Transform root)
        {
            model=root.gameObject;
            if(root.GetComponent<PlayerRig>()!=null)foreach(var clip in Resources.LoadAll<AnimationClip>("AsunaGrips"))native[clip.name]=clip;
            var bones=new Dictionary<string,Transform>();foreach(var t in root.GetComponentsInChildren<Transform>())bones[t.name]=t;
            for(int h=0;h<2;h++)
            {
                string side=h==0?"R":"L";hands[h]=bones["hand_"+side];
                for(int f=0;f<5;f++)for(int n=0;n<3;n++)
                    if(bones.TryGetValue("hand"+Names[f]+(n+1)+"_"+side,out var joint)){joints[h,f,n]=joint;rest[h,f,n]=joint.localRotation;}
            }
        }
        public Vector3 Center(int hand)
        {
            Vector3 knuckles=Vector3.zero;int count=0;
            for(int f=0;f<4;f++)if(joints[hand,f,0]!=null){knuckles+=joints[hand,f,0].position;count++;}
            return count==0?hands[hand].position:Vector3.Lerp(hands[hand].position,knuckles/count,.65f);
        }
        public void Apply(int hand,GearKind? kind,Quaternion grip)
        {
            for(int f=0;f<5;f++)for(int n=0;n<3;n++)if(joints[hand,f,n]!=null)joints[hand,f,n].localRotation=rest[hand,f,n];
            if(kind==null)return;
            string pose=(kind==GearKind.Shield?GearKind.Sword:kind.Value).ToString()+(hand==0?"R":"L");
            if(native.TryGetValue(pose,out var authored)){authored.SampleAnimation(model,0);return;}
            var wrist=hands[hand];Vector3 center=Center(hand)-grip*Vector3.up*.015f*wrist.lossyScale.x;
            for(int f=0;f<5;f++)
            {
                var first=joints[hand,f,0];var second=joints[hand,f,1];if(first==null||second==null)continue;
                Vector3 direction=second.position-first.position;
                Vector3 inward=Vector3.ProjectOnPlane(center-first.position,direction);
                if(inward.sqrMagnitude<.000001f)inward=grip*(hand==0?Vector3.left:Vector3.right);
                Vector3 axis=Vector3.Cross(direction,inward).normalized;
                bool trigger=f==0&&(kind==GearKind.Handgun||kind==GearKind.Rifle);
                var angles=f==4?Thumb:trigger?Trigger:Fist;
                for(int n=0;n<3;n++)if(joints[hand,f,n]!=null)joints[hand,f,n].rotation=Quaternion.AngleAxis(angles[n],axis)*joints[hand,f,n].rotation;
            }
        }
    }
}
