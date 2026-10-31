using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;
using VideoGame.Core;
public sealed class LabCompletionTests
{
    Prototype game;
    [UnitySetUp] public IEnumerator Setup()
    {
        var old=Object.FindFirstObjectByType<Prototype>();if(old!=null)Object.Destroy(old.gameObject);yield return null;
        game=new GameObject("Complete Lab test").AddComponent<Prototype>();yield return null;game.NewSession();game.StartMission((int)BiomeId.BiotechLab,6);
        game.Director.enabled=false;game.Player.enabled=false;foreach(var d in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))d.enabled=false;
    }
    [UnityTearDown] public IEnumerator Cleanup(){Time.timeScale=1;Object.Destroy(game.gameObject);yield return null;}
    [UnityTest] public IEnumerator CompleteRosterHasDistinctWeightedBodiesAndFullBodyMotion()
    {
        var meshes=new System.Collections.Generic.HashSet<Mesh>();
        foreach(var role in new[]{"LabSpider","LabHazmat","LabAoe","LabHeavy","LabBoss"})
        {
            var kind=role=="LabSpider"?EnemyKind.Rusher:role=="LabHazmat"?EnemyKind.Gunner:role=="LabAoe"?EnemyKind.Bombardier:EnemyKind.Hunter;
            var d=game.SpawnDrone(new Vector3(0,1,0),kind,role=="LabBoss");d.enabled=false;var motion=d.Motion;motion.enabled=false;
            Assert.AreEqual(role,motion.name.Replace("(Clone)",""));var skin=d.GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(s=>s.sharedMesh.vertexCount).First();Assert.IsTrue(meshes.Add(skin.sharedMesh));
            foreach(var renderer in d.GetComponentsInChildren<Renderer>())foreach(var m in renderer.sharedMaterials){Assert.IsNotNull(m);Assert.IsTrue(m.shader.isSupported);}
            foreach(var state in new[]{"Idle","RunForward","RunBack","RunLeft","RunRight","Fire","Cast","Hit","Melee","Death","Dash"})Assert.IsNotNull(motion.Player[state],role+" / "+state);
            var a=new Mesh();var b=new Mesh();motion.Player.Play("RunForward");motion.Player["RunForward"].normalizedTime=.18f;motion.Player.Sample();skin.BakeMesh(a,true);var firstMatrix=skin.localToWorldMatrix;motion.Player["RunForward"].normalizedTime=.68f;motion.Player.Sample();skin.BakeMesh(b,true);var secondMatrix=skin.localToWorldMatrix;
            Assert.That(a.vertices.Zip(b.vertices,(x,y)=>Vector3.Distance(firstMatrix.MultiplyPoint3x4(x),secondMatrix.MultiplyPoint3x4(y))).Max(),Is.GreaterThan(role=="LabSpider"?.002f:.01f),role+" skin must actually deform");
            motion.Player.Play("Idle");motion.Player["Idle"].time=0;motion.Player.Sample();var bounds=Art.RenderedBounds(motion.gameObject);Assert.Greater(bounds.size.y,.5f);Assert.Less(bounds.size.y,3.3f);Assert.That(bounds.min.y,Is.EqualTo(0).Within(.12f));
            if(role!="LabSpider"&&role!="LabHazmat"){Assert.IsTrue(d.GetComponentsInChildren<Transform>().Any(t=>t.name=="ShotOrigin"));var socketBounds=bounds;socketBounds.Expand(1);Assert.IsTrue(socketBounds.Contains(d.ShotOrigin),role+" muzzle must remain beside the weapon/body");}
            Object.Destroy(a);Object.Destroy(b);Object.Destroy(d.gameObject);yield return null;
        }
    }
    [UnityTest] public IEnumerator HeavyShieldAbsorbsOverflowAndDeathRewardsOnlyOnce()
    {
        var d=game.SpawnDrone(Vector3.up,EnemyKind.Hunter);d.enabled=false;Assert.IsTrue(d.IsLabHeavy);Assert.IsTrue(d.Ranged);Assert.Greater(d.HealthMaximum,Drone.MaxHealth);float health=d.Health;
        d.Damage(30);Assert.AreEqual(health,d.Health);Assert.AreEqual(d.ShieldMaximum-30,d.Shield);Assert.IsTrue(d.HealthBarVisible);
        d.Damage(d.Shield+25);Assert.AreEqual(0,d.Shield);Assert.AreEqual(health-25,d.Health);
        int kills=game.Mission.Kills;d.Damage(d.HealthMaximum);Assert.AreEqual(kills+1,game.Mission.Kills);d.Damage(1);Assert.AreEqual(kills+1,game.Mission.Kills);Assert.IsTrue(d.Motion.Dead);yield return null;
    }
    [UnityTest] public IEnumerator BossIsUniqueOptionalAndResetsWithMissionWorld()
    {
        var bosses=Object.FindObjectsByType<Drone>(FindObjectsSortMode.None).Where(d=>d.IsLabBoss).ToArray();Assert.AreEqual(1,bosses.Length);Assert.LessOrEqual(Object.FindObjectsByType<Drone>(FindObjectsSortMode.None).Length,EncounterDirector.PopulationCap);
        var boss=bosses[0];Assert.IsTrue(boss.Ranged);Assert.Greater(boss.HealthMaximum,400);boss.Damage(boss.HealthMaximum*.51f);Assert.IsTrue(boss.Enraged);
        game.BuildHub();yield return null;Assert.IsFalse(Object.FindObjectsByType<Drone>(FindObjectsSortMode.None).Any());
        game.Mission.Abandon();game.StartMission((int)BiomeId.BiotechLab,6);game.Director.enabled=false;bosses=Object.FindObjectsByType<Drone>(FindObjectsSortMode.None).Where(d=>d.IsLabBoss).ToArray();Assert.AreEqual(1,bosses.Length);Assert.IsFalse(bosses[0].Enraged);Assert.AreEqual(bosses[0].HealthMaximum,bosses[0].Health);
    }
    [UnityTest] public IEnumerator EnragedPulseIsTelegraphedPausedAndCancelledOnBossDeath()
    {
        foreach(var d in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))Object.Destroy(d.gameObject);yield return null;
        game.Player.Teleport(new Vector3(0,1,0));var boss=game.SpawnDrone(new Vector3(0,1,7),EnemyKind.Hunter,true);boss.Damage(boss.HealthMaximum*.51f);
        float until=Time.realtimeSinceStartup+4;
        while(!Object.FindObjectsByType<EnemyMortar>(FindObjectsSortMode.None).Any(m=>m.Owner==boss)&&Time.realtimeSinceStartup<until)yield return null;
        var marks=Object.FindObjectsByType<EnemyMortar>(FindObjectsSortMode.None).Where(m=>m.Owner==boss).ToArray();Assert.AreEqual(3,marks.Length);Assert.IsTrue(marks.All(m=>!m.Detonated));
        float health=game.Player.Health;Time.timeScale=0;yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(health,game.Player.Health);Assert.IsTrue(marks.All(m=>m!=null&&!m.Detonated));Time.timeScale=1;
        boss.Damage(boss.HealthMaximum);yield return null;yield return null;Assert.IsFalse(Object.FindObjectsByType<EnemyMortar>(FindObjectsSortMode.None).Any(m=>m.Owner==boss));Assert.AreEqual("Dead",boss.BossState);Assert.IsFalse(boss.WindingUp);
    }
    [UnityTest] public IEnumerator MeleeCompatibilityAndNonLabBehaviorRemainIntact()
    {
        var melee=game.SpawnDrone(Vector3.up,false);melee.enabled=false;Assert.AreEqual(EnemyKind.Rusher,melee.Kind);Assert.IsFalse(melee.Ranged);
        game.NewSession();game.StartMission((int)BiomeId.NeonStreets,6);game.Director.enabled=false;var hunter=game.SpawnDrone(Vector3.up,EnemyKind.Hunter);hunter.enabled=false;Assert.IsFalse(hunter.Ranged);Assert.AreEqual(0,hunter.Shield);Assert.IsFalse(Object.FindObjectsByType<Drone>(FindObjectsSortMode.None).Any(d=>d.IsLabBoss));
        Assert.Throws<System.ArgumentException>(()=>game.SpawnDrone(Vector3.up,EnemyKind.Hunter,true));yield return null;
    }
}
