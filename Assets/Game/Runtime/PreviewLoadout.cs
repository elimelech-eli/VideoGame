using System.Collections.Generic;
using UnityEngine;
using VideoGame.Core;

namespace VideoGame
{
    // Portrait-only arm poses. The sampled base rig and gameplay animation clips
    // remain untouched. Weapon meshes use metres, grip origin, +Z muzzle / +Y blade.
    public sealed class PreviewLoadout
    {
        readonly Transform model;
        readonly Dictionary<string,Transform> bones=new Dictionary<string,Transform>();
        readonly Dictionary<Transform,Vector3> positions=new Dictionary<Transform,Vector3>();
        readonly WeaponClipLayer clips;
        readonly WeaponGripPose fingers;
        readonly Dictionary<Transform,Quaternion> rest=new Dictionary<Transform,Quaternion>();
        Transform presentation;
        readonly float height;
        public string Pose {get;private set;}
        public PreviewLoadout(Transform root)
        {
            model=root;clips=new WeaponClipLayer(root.gameObject);fingers=new WeaponGripPose(root);
            foreach(var t in root.GetComponentsInChildren<Transform>()){bones[t.name]=t;rest[t]=t.localRotation;positions[t]=t.localPosition;}
            height=Vector3.Distance(bones["hips"].position,bones["foot_L"].position);
        }
        public void Apply(GearProfile profile)
        {
            foreach(var entry in rest){entry.Key.localRotation=entry.Value;entry.Key.localPosition=positions[entry.Key];}
            if(presentation!=null){presentation.gameObject.SetActive(false);Object.Destroy(presentation.gameObject);}
            presentation=new GameObject("Loadout presentation").transform;presentation.SetParent(model,false);
            foreach(var t in model.GetComponentsInChildren<Transform>())if(t.name=="SOCKET_weapon_R")foreach(var r in t.GetComponentsInChildren<Renderer>())r.enabled=false;
            var main=profile.Equipped[3];var off=profile.Equipped[4];
            Pose=main?.Design.TwoHanded==true?"Rifle ready":(main?.Design.Kind.ToString()??"Empty")+" / "+(off?.Design.Kind.ToString()??"Empty");
            if(main?.Design.Kind==GearKind.Sword)clips.Apply("SwordR",0,1,0,true,true);
            if(off?.Design.Kind==GearKind.Sword)clips.Apply("SwordL",0,1,1,main?.Design.Kind!=GearKind.Sword,true);
            if(main?.Design.Kind!=GearKind.Sword && off?.Design.Kind!=GearKind.Sword && off?.Design.Kind==GearKind.Shield)clips.Apply("ShieldGuard",.5f,1,-1,true,true);
            Arm("R",main?.Design.Kind,main?.Design.TwoHanded==true);
            Arm("L",off?.Design.Kind,false,main?.Design.TwoHanded==true);
            fingers.Apply(0,main?.Design.Kind,bones["hand_R"].rotation*bones["SOCKET_weapon_R"].localRotation);
            fingers.Apply(1,off?.Design.Kind??(main?.Design.TwoHanded==true?GearKind.Rifle:(GearKind?)null),bones["hand_L"].rotation*bones["SOCKET_weapon_R"].localRotation);
            foreach(var t in presentation.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
        }
        void Arm(string side,GearKind? kind,bool rifle,bool support=false)
        {
            float sign=side=="R"?1:-1;Vector3 chest=bones["chest"].position;
            Vector3 offset;
            if(support)offset=new Vector3(-.06f,-.07f,.48f);
            else if(rifle)offset=new Vector3(.13f,-.05f,.24f);
            else if(kind==GearKind.Handgun)offset=new Vector3(sign*.31f,-.12f,.37f);
            else if(kind==GearKind.Shield)offset=new Vector3(sign*.30f,-.18f,.28f);
            else if(kind==GearKind.Sword)offset=new Vector3(sign*.34f,-.40f,.12f);
            else offset=new Vector3(sign*.30f,-.61f,.02f);
            Vector3 target=chest+model.TransformDirection(offset)*height;
            if(kind!=GearKind.Sword)SolveArm(side,target,model.TransformDirection(new Vector3(sign,-.7f,-.1f)));
            if(kind==null)return;
            var hand=bones["hand_"+side];
            Quaternion rotation=model.rotation;
            if(kind==GearKind.Handgun)rotation*=Quaternion.Euler(-18,sign*12,0);
            if(kind==GearKind.Sword)rotation=hand.rotation*bones["SOCKET_weapon_R"].localRotation;
            if(kind==GearKind.Shield)rotation*=Quaternion.Euler(0,-15,0);
            // The published right-hand socket supplies the wrist-to-grip basis.
            hand.rotation=rotation*Quaternion.Inverse(bones["SOCKET_weapon_R"].localRotation);
            var source=Resources.Load<GameObject>("LoadoutWeapons/"+kind.Value);
            if(source==null)throw new System.InvalidOperationException("Missing loadout weapon "+kind);
            var weapon=Object.Instantiate(source,presentation);weapon.name="Preview "+side+" "+kind;
            weapon.transform.position=fingers.Center(side=="R"?0:1);weapon.transform.rotation=rotation;
            float unit=height/Mathf.Max(.001f,model.lossyScale.x);weapon.transform.localScale=Vector3.one*unit;
        }
        void Aim(string a,string b,Vector3 direction)
        {
            var bone=bones[a];bone.rotation=Quaternion.FromToRotation(bones[b].position-bone.position,direction)*bone.rotation;
        }
        void SolveArm(string side,Vector3 target,Vector3 pole)
        {
            var upper=bones["upper_arm_"+side];var lower=bones["forearm_"+side];var hand=bones["hand_"+side];
            Vector3 shoulder=upper.position;float a=Vector3.Distance(shoulder,lower.position),b=Vector3.Distance(lower.position,hand.position);
            Vector3 ray=target-shoulder;float distance=Mathf.Clamp(ray.magnitude,Mathf.Abs(a-b)+.002f,a+b-.002f);Vector3 axis=ray.normalized;
            Vector3 bend=Vector3.ProjectOnPlane(pole,axis).normalized;float along=(a*a-b*b+distance*distance)/(2*distance);
            Vector3 elbow=shoulder+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            Aim("upper_arm_"+side,"forearm_"+side,elbow-shoulder);Aim("forearm_"+side,"hand_"+side,target-lower.position);
        }
    }
}
