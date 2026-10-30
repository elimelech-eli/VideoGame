using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VideoGame;

public sealed class AsunaRigTests
{
    [Test]
    public void SelectedRigHasUsableMeshesMatchingClipsAndIndependentHands()
    {
        var source=Resources.Load<GameObject>(PlayerRig.PrefabPath);
        Assert.That(source,Is.Not.Null);
        bool asuna=Resources.Load<GameObject>(PlayerRig.AsunaPrefabPath)!=null;
        Assert.That(source.GetComponent<PlayerRig>()!=null,Is.EqualTo(asuna),"Select Asuna only when its licensed prefab is available");
        TestContext.WriteLine(asuna?"Validating locally imported Asuna":"Licensed Asuna content absent: validating the public-checkout operative fallback");
        var model=Object.Instantiate(source);
        try
        {
            var bones=model.GetComponentsInChildren<Transform>().GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
            foreach(string name in new[]{"hips","spine_mid","chest","head","hand_R","hand_L","handindex1_R","handindex1_L","SOCKET_weapon_R"})
                Assert.That(bones.ContainsKey(name),Is.True,name);
            if(asuna)Assert.That(bones.ContainsKey("Fitted closed helmet"),Is.True);
            Assert.That(model.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterials.All(m=>m!=null&&m.shader.name=="Standard")),Is.True);
            string folder=model.GetComponent<PlayerRig>()?.AnimationFolder??"OperatorAnimations";
            var idle=Resources.Load<AnimationClip>(folder+"/Idle");
            var run=Resources.Load<AnimationClip>(folder+"/RunForward");
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
