using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;
using VideoGame.Core;
public sealed class RemainingEnemyTests
{
    Prototype game;
    static readonly BiomeId[] Biomes={BiomeId.NeonStreets,BiomeId.CorporateOffice,BiomeId.Undercity};
    static readonly EnemyKind[] Kinds={EnemyKind.Rusher,EnemyKind.Gunner,EnemyKind.Bombardier,EnemyKind.Hunter};
    static readonly string[] States={"Idle","RunForward","RunBack","RunLeft","RunRight","Fire","Melee","Cast","Dash","Hit","Death"};
    [UnitySetUp]public IEnumerator Setup(){var old=Object.FindFirstObjectByType<Prototype>();if(old)Object.Destroy(old.gameObject);yield return null;game=new GameObject("Remaining enemy integration").AddComponent<Prototype>();yield return null;}
    [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;AudioListener.pause=false;if(game)Object.Destroy(game.gameObject);yield return null;}
    void Quiet(){game.Director.enabled=false;game.Player.enabled=false;foreach(var d in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))d.enabled=false;}
    Vector3[] Snapshot(ActorAnimation animation)
    {
        var vertices=new List<Vector3>();
        foreach(var skin in animation.GetComponentsInChildren<SkinnedMeshRenderer>())
        {var mesh=new Mesh();skin.BakeMesh(mesh);vertices.AddRange(mesh.vertices.Select(v=>skin.transform.TransformPoint(v)));Object.Destroy(mesh);}
        return vertices.ToArray();
    }
    static void Sample(ActorAnimation a,string state,float phase){a.Player.Play(state);a.Player[state].normalizedTime=phase;a.Player.Sample();}
    void Capture(string filename,Vector3? focus=null,float size=7)
    {
        string dir=System.Environment.GetEnvironmentVariable("ENEMY_CAPTURE_DIR");if(string.IsNullOrEmpty(dir)||SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
        Directory.CreateDirectory(dir);var cam=game.Player.View;var oldPosition=cam.transform.position;var oldRotation=cam.transform.rotation;float oldSize=cam.orthographicSize;
        var oldTarget=cam.targetTexture;var oldActive=RenderTexture.active;var rt=new RenderTexture(1440,1000,24){antiAliasing=4};var image=new Texture2D(1440,1000,TextureFormat.RGB24,false);
        try{cam.orthographicSize=size;cam.transform.rotation=Quaternion.Euler(50,45,0);cam.transform.position=(focus??new Vector3(0,1,0))-cam.transform.forward*30;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1440,1000),0,0);image.Apply();File.WriteAllBytes(Path.Combine(dir,filename+".png"),image.EncodeToPNG());}
        finally{cam.targetTexture=oldTarget;RenderTexture.active=oldActive;cam.transform.SetPositionAndRotation(oldPosition,oldRotation);cam.orthographicSize=oldSize;rt.Release();Object.Destroy(rt);Object.Destroy(image);}
    }
    [UnityTest]public IEnumerator DedicatedModelsHaveWeightedLocomotionActionsWeaponsAndShields()
    {
        foreach(var biome in Biomes)
        {
            if(game.InMission){game.Mission.Abandon();game.BuildHub();yield return null;}
            game.StartMission((int)biome,6);Quiet();yield return null;
            foreach(var d in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))Object.Destroy(d.gameObject);yield return null;
            game.Player.Teleport(new Vector3(0,1,-5));yield return null;
            var roster=new List<Drone>();
            for(int i=0;i<Kinds.Length;i++)
            {
                var d=game.SpawnDrone(new Vector3(i%2==0?-2.5f:2.5f,1,i<2?-2.5f:2.5f),Kinds[i]);d.enabled=false;var a=d.Motion;Assert.IsNotNull(a);Assert.That(a.name,Does.StartWith(DedicatedEnemyRoster.Prefix(biome)));a.enabled=false;a.Player.enabled=false;
                foreach(string state in States)Assert.IsNotNull(a.Player[state],a.name+" missing "+state);
                Sample(a,"RunForward",.2f);var start=Snapshot(a);Sample(a,"RunForward",.7f);var end=Snapshot(a);Assert.Greater(start.Length,0);Assert.AreEqual(start.Length,end.Length);Assert.Greater(start.Zip(end,Vector3.Distance).Max(),.003f,a.name+" weighted locomotion is static");
                foreach(string state in new[]{"Fire","Melee","Cast","Hit","Death"}){Sample(a,state,0);start=Snapshot(a);Sample(a,state,.8f);end=Snapshot(a);Assert.Greater(start.Zip(end,Vector3.Distance).Max(),.001f,a.name+" static action "+state);}
                Sample(a,"Idle",.2f);var bounds=Art.RenderedBounds(a.gameObject);Assert.That(bounds.size.y,Is.InRange(.25f,4));Assert.Less(bounds.size.magnitude,6,a.name+" exploded bind pose");Assert.That(bounds.min.y,Is.InRange(-.2f,.3f),a.name+" contact plane");
                if(d.Ranged){Assert.IsTrue(a.GetComponentsInChildren<Transform>().Any(t=>t.name=="ShotOrigin"));Assert.Less(Vector3.Distance(d.ShotOrigin,bounds.center),bounds.extents.magnitude+.5f,a.name+" detached muzzle");}
                if(Kinds[i]==EnemyKind.Hunter){Assert.IsTrue(d.IsBiomeHeavy);Assert.IsTrue(d.Ranged);float health=d.Health,shield=d.Shield;d.Damage(20);Assert.AreEqual(health,d.Health);Assert.AreEqual(shield-20,d.Shield);}else Assert.AreEqual(0,d.Shield);
                roster.Add(d);Capture(biome+"-"+Kinds[i]+"-detail",bounds.center,2);
            }
            game.Player.Teleport(new Vector3(0,1,-5));yield return null;yield return null;Capture(biome+"-idle");foreach(var d in roster)Sample(d.Motion,"RunForward",.25f);Capture(biome+"-run");foreach(var d in roster)Sample(d.Motion,d.Kind==EnemyKind.Rusher?"Melee":d.Kind==EnemyKind.Bombardier?"Cast":"Fire",.45f);Capture(biome+"-attack");
            foreach(var d in roster){d.Damage(d.Health+d.Shield+1);Assert.IsTrue(d.Motion.Dead);Sample(d.Motion,"Death",.9f);}Capture(biome+"-death");yield return null;
        }
    }
    [UnityTest]public IEnumerator InitialAndReinforcementModelsStayWithinSelectedBiome()
    {
        foreach(var biome in Biomes)
        {
            if(game.InMission){game.Mission.Abandon();game.BuildHub();yield return null;}
            game.StartMission((int)biome,6);Quiet();yield return null;string prefix=DedicatedEnemyRoster.Prefix(biome);
            var initial=Object.FindObjectsByType<Drone>(FindObjectsSortMode.None);Assert.Greater(initial.Length,0);foreach(var d in initial){Assert.That(d.Motion.name,Does.StartWith(prefix));Object.Destroy(d.gameObject);}yield return null;
            int spawned=game.Director.SpawnWave();Assert.Greater(spawned,0,"No reinforcement candidates");foreach(var d in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))Assert.That(d.Motion.name,Does.StartWith(prefix));
            game.Mission.Abandon();game.BuildHub();yield return null;Assert.IsEmpty(Object.FindObjectsByType<Drone>(FindObjectsSortMode.None),"Mission roster leaked into Nexus");
        }
    }
    [UnityTest]public IEnumerator ElevatedHeavyMuzzlesAimAtPlayerHeight()
    {
        foreach(var biome in Biomes)
        {
            if(game.InMission){game.Mission.Abandon();game.BuildHub();yield return null;}
            game.StartMission((int)biome,6);Quiet();yield return null;
            game.Player.Teleport(new Vector3(0,1,0));var heavy=game.SpawnDrone(new Vector3(0,1,5),EnemyKind.Hunter);
            EnemyBolt shot=null;float deadline=Time.time+1.2f;
            while(Time.time<deadline&&!shot){yield return null;shot=Object.FindFirstObjectByType<EnemyBolt>();}
            Assert.IsNotNull(shot,biome+" heavy did not fire");
            Assert.Less(Vector3.Angle(shot.Direction,game.Player.transform.position-shot.transform.position),.2f,"Fire must aim from the actual muzzle, including its height");
            heavy.enabled=false;heavy.StopAllCoroutines();Object.Destroy(shot.gameObject);yield return null;
        }
    }
}
