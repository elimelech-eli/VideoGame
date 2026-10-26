using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;
using VideoGame.Core;

public sealed class CombatWorldTests
{
    Prototype game;
    [UnitySetUp] public IEnumerator Setup()
    {
        var old = Object.FindFirstObjectByType<Prototype>();
        if (old != null) Object.Destroy(old.gameObject);
        yield return null;
        game = new GameObject("World combat test").AddComponent<Prototype>();
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if (game != null) Object.Destroy(game.gameObject);
        yield return null;
    }
    [UnityTest] public IEnumerator EnemyBarRefreshesOnHitAndHidesWithoutHealing()
    {
        var enemy = game.SpawnDrone(new Vector3(4, 1, 0), false); enemy.enabled = false;
        Assert.IsFalse(enemy.HealthBarVisible);
        enemy.Damage(10);
        Assert.AreEqual(50, enemy.Health); Assert.IsTrue(enemy.HealthBarVisible);
        yield return new WaitForSeconds(1.1f);
        enemy.Damage(10);
        yield return new WaitForSeconds(1.1f);
        Assert.IsTrue(enemy.HealthBarVisible, "Second hit must restart the two-second window");
        Assert.AreEqual(40, enemy.Health);
        yield return new WaitForSeconds(1.05f);
        Assert.IsFalse(enemy.HealthBarVisible); Assert.AreEqual(40, enemy.Health, "Hiding a bar must not heal");
    }
    [UnityTest] public IEnumerator ReinforcementsContinueAfterObjectiveAndRespectCap()
    {
        game.StartMission(); yield return null;
        foreach (var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None)) enemy.Damage(100);
        Assert.AreEqual(MissionStage.Complete, game.Mission.Stage);
        yield return new WaitForSeconds(EncounterDirector.SpawnInterval + .15f);
        Assert.Greater(Object.FindObjectsByType<Drone>(FindObjectsSortMode.None).Length, 0, "Completed contract must still get reinforcements");
        for (int i = 0; i < 10; i++) game.Director.SpawnWave();
        Assert.LessOrEqual(Object.FindObjectsByType<Drone>(FindObjectsSortMode.None).Length, EncounterDirector.PopulationCap);
        Assert.AreEqual(game.Mission.Target, game.Mission.Kills, "Extra kills must not change completed objective");
        var director = game.Director;
        game.BuildHub();
        Assert.AreEqual(0, director.SpawnWave());
        Assert.AreEqual(0, Object.FindObjectsByType<Drone>(FindObjectsSortMode.None).Length);
    }
    [UnityTest] public IEnumerator BarrelsChainReactDamageEnemiesAndCannotExplodeTwice()
    {
        var a = game.SpawnDestructible(new Vector3(2, 0, 0), false);
        var b = game.SpawnDestructible(new Vector3(4, 0, 0), false);
        var target = game.SpawnDrone(new Vector3(3, 1, 1), false); target.enabled = false;
        Physics.SyncTransforms();
        a.Damage(40);
        Assert.IsTrue(a.IsDestroyed); Assert.IsTrue(b.IsDestroyed);
        Assert.AreEqual(0, target.Health); Assert.AreEqual(30, game.Player.Health);
        a.Damage(100); Assert.AreEqual(30, game.Player.Health);
        yield return null;
    }
    [UnityTest] public IEnumerator DamagedBulkheadTopplesAndCrushesEnemy()
    {
        var wall = game.SpawnDestructible(new Vector3(0, 0, 4), true);
        var target = game.SpawnDrone(new Vector3(0, 1, 5.5f), false); target.enabled = false;
        Physics.SyncTransforms(); wall.Damage(100);
        Assert.IsTrue(wall.IsDestroyed);
        yield return new WaitForSeconds(.85f);
        Assert.IsTrue(target == null || target.Health == 0);
        Assert.Greater(Quaternion.Angle(Quaternion.identity, wall.transform.rotation), 60);
        Assert.AreEqual(100, game.Player.Health);
    }
    [UnityTest] public IEnumerator EnemyProjectilesCanDetonateFuel()
    {
        var fuel = game.SpawnDestructible(new Vector3(4, 0, 0), false);
        Physics.SyncTransforms();
        for (int i = 0; i < 4; i++)
        {
            var bolt = new GameObject("Test enemy projectile"); bolt.transform.position = new Vector3(2, 1, 0);
            bolt.AddComponent<EnemyBolt>().Direction = Vector3.right;
            yield return new WaitForSeconds(.3f);
        }
        Assert.IsTrue(fuel == null || fuel.IsDestroyed);
        Assert.AreEqual(65, game.Player.Health);
    }
}
