using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VideoGame;

public sealed class AsunaRigTests
{
    [Test]
    public void TrialRigHasVisibleArmorHelmetAndWorkingNativeClips()
    {
        var source=Resources.Load<GameObject>(PlayerRig.AsunaPrefabPath);
        if(source==null)Assert.Ignore("Optional licensed Asuna content is not imported. Follow Art/Asuna/README.md to run this asset-specific check.");
        var model=Object.Instantiate(source);
        try
        {
            var bones=model.GetComponentsInChildren<Transform>().GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
            foreach(string name in new[]{"hips","spine_mid","chest","head","hand_R","hand_L","handindex1_R","handindex1_L","SOCKET_weapon_R","Fitted closed helmet"})
                Assert.That(bones.ContainsKey(name),Is.True,name);
            Assert.That(model.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterials.All(m=>m!=null&&m.shader.name=="Standard")),Is.True);
            var idle=Resources.Load<AnimationClip>("AsunaAnimations/Idle");
            var run=Resources.Load<AnimationClip>("AsunaAnimations/RunForward");
            idle.SampleAnimation(model,0);run.SampleAnimation(model,0);var first=bones["foot_L"].position;
            run.SampleAnimation(model,run.length*.35f);
            Assert.That(Vector3.Distance(first,bones["foot_L"].position),Is.GreaterThan(.025f),"Native run must move the feet");
            idle.SampleAnimation(model,0);var left=bones["hand_L"].localRotation;
            var layer=new WeaponClipLayer(model);layer.Apply("SwordR",.22f,1,0,true,true);
            Assert.That(Quaternion.Angle(left,bones["hand_L"].localRotation),Is.LessThan(.01f),"Main-hand clip must preserve the off hand");
            Assert.That(model.GetComponentsInChildren<SkinnedMeshRenderer>().All(r=>r.sharedMesh!=null&&r.bones.All(b=>b!=null)),Is.True);
        }
        finally{Object.DestroyImmediate(model);}
    }
}
