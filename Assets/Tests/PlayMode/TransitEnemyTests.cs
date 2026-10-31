using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;
using VideoGame.Core;
public sealed class TransitEnemyTests
{
    Prototype game;
    [UnitySetUp]public IEnumerator Setup(){var old=Object.FindFirstObjectByType<Prototype>();if(old!=null)Object.Destroy(old.gameObject);yield return null;game=new GameObject("Transit roster test").AddComponent<Prototype>();yield return null;game.NewSession();game.StartMission((int)BiomeId.Transit,6);game.Director.enabled=false;game.Player.enabled=false;foreach(var d in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))d.enabled=false;}
    [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;Object.Destroy(game.gameObject);yield return null;}
    [UnityTest]public IEnumerator CompleteRosterHasDistinctGeometryWeightedMotionAndWeaponSockets()
    {
        var meshes=new System.Collections.Generic.HashSet<Mesh>();
        foreach(var kind in new[]{EnemyKind.Rusher,EnemyKind.Gunner,EnemyKind.Bombardier,EnemyKind.Hunter})
        {
            string expected=kind==EnemyKind.Rusher?"TransitMelee":kind==EnemyKind.Gunner?"TransitGunner":kind==EnemyKind.Bombardier?"TransitAoe":"TransitHeavy";
            var d=game.SpawnDrone(Vector3.up,kind);d.enabled=false;var motion=d.Motion;motion.enabled=false;motion.Player.enabled=false;
            Assert.AreEqual(expected,motion.name.Replace("(Clone)",""));Assert.AreEqual(kind!=EnemyKind.Rusher,d.Ranged);
            var skin=d.GetComponentInChildren<SkinnedMeshRenderer>();Assert.IsNotNull(skin);Assert.IsTrue(meshes.Add(skin.sharedMesh));
            foreach(var state in new[]{"Idle","RunForward","RunBack","RunLeft","RunRight","Fire","Cast","Melee","Hit","Death","Dash"})Assert.IsNotNull(motion.Player[state],expected+" / "+state);
            var a=new Mesh();var b=new Mesh();motion.Player.Play("RunForward");motion.Player["RunForward"].normalizedTime=.2f;motion.Player.Sample();skin.BakeMesh(a,true);var matrix=skin.localToWorldMatrix;motion.Player["RunForward"].normalizedTime=.7f;motion.Player.Sample();skin.BakeMesh(b,true);
            Assert.That(a.vertices.Zip(b.vertices,(x,y)=>Vector3.Distance(matrix.MultiplyPoint3x4(x),skin.transform.TransformPoint(y))).Max(),Is.GreaterThan(.005f),expected+" weighted mesh must move");
            motion.Player.Play("Idle");motion.Player["Idle"].time=0;motion.Player.Sample();var bounds=Art.RenderedBounds(motion.gameObject);Assert.That(bounds.min.y,Is.EqualTo(kind==EnemyKind.Bombardier?.9f:0).Within(.15f),expected+" ground/hover plane");Assert.That(bounds.size.y,Is.InRange(.3f,2.6f),expected+" scale");
            if(kind!=EnemyKind.Rusher){Assert.IsTrue(d.GetComponentsInChildren<Transform>().Any(t=>t.name=="ShotOrigin"));bounds.Expand(1);Assert.IsTrue(bounds.Contains(d.ShotOrigin),expected+" muzzle proximity");}
            var detailed=Resources.Load<Texture2D>("Detailed/TransitEnemies/Textures/"+expected+"/"+(kind==EnemyKind.Hunter?"m07":"m00")+"_Albedo");Assert.That(detailed.width,Is.GreaterThanOrEqualTo(1024),expected+" source texture detail must not collapse to a flat-color resolution");
            foreach(var m in skin.sharedMaterials){Assert.IsNotNull(m);Assert.IsNotNull(m.mainTexture);Assert.IsTrue(m.shader.isSupported);}
            Object.Destroy(a);Object.Destroy(b);Object.Destroy(d.gameObject);yield return null;
        }
    }
    [UnityTest]public IEnumerator ShieldDamageAndMeleeCompatibilityUseSharedRules()
    {
        var heavy=game.SpawnDrone(Vector3.up,EnemyKind.Hunter);heavy.enabled=false;heavy.SetLevel(1);Assert.IsTrue(heavy.IsTransitHeavy);Assert.IsFalse(heavy.IsLabHeavy);Assert.AreEqual(90,heavy.Shield);float health=heavy.Health;heavy.Damage(40);Assert.AreEqual(health,heavy.Health);heavy.Damage(70);Assert.AreEqual(health-20,heavy.Health);Assert.AreEqual(0,heavy.Shield);
        int kills=game.Mission.Kills;heavy.Damage(heavy.HealthMaximum);heavy.Damage(1);Assert.AreEqual(kills+1,game.Mission.Kills);Assert.IsTrue(heavy.Motion.Dead);
        var melee=game.SpawnDrone(Vector3.up,false);melee.enabled=false;Assert.AreEqual(EnemyKind.Rusher,melee.Kind);Assert.IsFalse(melee.Ranged);yield return null;
    }
    [UnityTest]public IEnumerator InitialAndReinforcementPopulationStayTransitAndTearDown()
    {
        Assert.IsTrue(Object.FindObjectsByType<Drone>(FindObjectsSortMode.None).All(d=>d.Motion.name.StartsWith("Transit")));foreach(var d in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))Object.Destroy(d.gameObject);yield return null;
        var center=game.Layout[0];game.Player.Teleport(new Vector3(center.X*20,1,center.Y*20));int spawned=game.Director.SpawnWave();Assert.Greater(spawned,0);Assert.IsTrue(Object.FindObjectsByType<Drone>(FindObjectsSortMode.None).All(d=>d.Motion.name.StartsWith("Transit")));
        game.Mission.Abandon();game.BuildHub();yield return null;Assert.IsFalse(Object.FindObjectsByType<Drone>(FindObjectsSortMode.None).Any());
    }
}
