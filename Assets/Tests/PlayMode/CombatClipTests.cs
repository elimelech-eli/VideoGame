using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VideoGame;

public sealed class CombatClipTests
{
    [Test] public void GripUsesFingerJointsWithoutStretchingAndEmptyHandsRestore()
    {
        var model=Object.Instantiate(Resources.Load<GameObject>("Detailed/Prefabs/OperativeV03"));
        try
        {
            var bones=model.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name,t=>t);
            var first=bones["handmiddle1_R"];var second=bones["handmiddle2_R"];Quaternion rest=first.localRotation;float length=Vector3.Distance(first.position,second.position);
            var pose=new WeaponGripPose(model.transform);pose.Apply(0,VideoGame.Core.GearKind.Sword,bones["hand_R"].rotation*bones["SOCKET_weapon_R"].localRotation);
            Assert.Greater(Quaternion.Angle(rest,first.localRotation),30);Assert.AreEqual(length,Vector3.Distance(first.position,second.position),.00001f);
            pose.Apply(0,null,Quaternion.identity);Assert.Less(Quaternion.Angle(rest,first.localRotation),.01f);
        }
        finally{Object.DestroyImmediate(model);}
    }
    [TestCase("SwordR","R")]
    [TestCase("SwordL","L")]
    public void SwordKeysKeepLimbLengthsFeetPlantedAndBodyDrivingTheCut(string clipName,string side)
    {
        var model=Object.Instantiate(Resources.Load<GameObject>("Detailed/Prefabs/OperativeV03"));
        try
        {
            var bones=model.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name,t=>t);
            var clip=Resources.Load<AnimationClip>("WeaponAnimations/"+clipName);Assert.IsNotNull(clip);
            Assert.AreEqual(PlayerWeaponAnimation.SwingDuration,clip.length,.001f);
            clip.SampleAnimation(model,0);
            var upper=bones["upper_arm_"+side];var fore=bones["forearm_"+side];var hand=bones["hand_"+side];
            float a=Vector3.Distance(upper.position,fore.position),b=Vector3.Distance(fore.position,hand.position);
            Vector3 left=bones["foot_L"].position,right=bones["foot_R"].position,root=model.transform.position;
            Quaternion initial=bones["chest"].rotation;
            Vector3 previous=hand.position;
            for(int frame=0;frame<=41;frame++)
            {
                float time=Mathf.Min(clip.length,frame/60f);clip.SampleAnimation(model,time);
                Assert.AreEqual(a,Vector3.Distance(upper.position,fore.position),.0001f);
                Assert.AreEqual(b,Vector3.Distance(fore.position,hand.position),.0001f);
                float flex=Vector3.Angle(fore.position-upper.position,hand.position-fore.position);
                Assert.That(flex,Is.InRange(15f,145f),clipName+" frame "+frame);
                Assert.Less(Vector3.Distance(previous,hand.position),.25f,"Discontinuous wrist path at "+frame);previous=hand.position;
                Assert.Less(Vector3.Distance(left,bones["foot_L"].position),.012f,"Left foot slides at "+frame);
                Assert.Less(Vector3.Distance(right,bones["foot_R"].position),.012f,"Right foot slides at "+frame);
                Assert.AreEqual(root,model.transform.position);
            }
            clip.SampleAnimation(model,PlayerWeaponAnimation.SwordContactTime);
            Assert.Greater(Quaternion.Angle(initial,bones["chest"].rotation),20,"Torso must drive contact, not just the wrist.");
        }
        finally{Object.DestroyImmediate(model);}
    }
}
