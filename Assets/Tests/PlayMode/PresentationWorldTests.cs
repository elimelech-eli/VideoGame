using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;

public sealed class PresentationWorldTests
{
    Prototype game;
    [UnitySetUp] public IEnumerator Setup()
    {
        var old=Object.FindFirstObjectByType<Prototype>(); if(old!=null)Object.Destroy(old.gameObject);
        yield return null; game=new GameObject("Presentation world test").AddComponent<Prototype>(); yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup() { if(game!=null)Object.Destroy(game.gameObject); yield return null; }
    [UnityTest] public IEnumerator WallsUseIncomingShotDirectionEvenForOffCenterHits()
    {
        game.Player.Teleport(new Vector3(-7,1,-7)); game.Player.enabled=false;
        for(int i=0;i<8;i++)
        {
            Vector3 direction=Quaternion.Euler(0,i*45,0)*Vector3.forward;
            var wall=game.SpawnDestructible(Vector3.zero,true);
            // A near-edge impact position must not turn a straight shot sideways.
            wall.Damage(40,new Vector3(1,1,-.2f),direction);
            Assert.Greater(Vector3.Dot(wall.FallDirection,direction),.999f);
            yield return new WaitForSeconds(.8f);
            Vector3 fallenUp=wall.transform.up; fallenUp.y=0;
            Assert.Greater(Vector3.Dot(fallenUp.normalized,direction),.99f);
            Object.Destroy(wall.gameObject); yield return null;
        }
    }
    [UnityTest] public IEnumerator OppositeSideEnemyShotsCanCrushPlayerOnEitherSide()
    {
        game.StartMission(); game.Director.enabled=false;
        foreach(var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))enemy.enabled=false;
        foreach(float side in new[]{-1f,1f})
        {
            game.Player.Teleport(new Vector3(0,1,side*1.6f)); game.Player.Health=100; game.Player.enabled=false;
            var wall=game.SpawnDestructible(Vector3.zero,true); Physics.SyncTransforms();
            for(int i=0;i<4;i++)
            {
                var bolt=new GameObject("Opposite side shot"); bolt.transform.position=new Vector3(0,1,-side*2);
                bolt.AddComponent<EnemyBolt>().Direction=Vector3.forward*side;
                yield return new WaitForSeconds(.3f);
            }
            yield return new WaitForSeconds(.85f);
            Assert.IsTrue(wall.IsDestroyed); Assert.AreEqual(65,game.Player.Health,"Wall must crush the player behind it regardless of which face was hit");
            Assert.Greater(Vector3.Dot(wall.FallDirection,Vector3.forward*side),.99f);
            Object.Destroy(wall.gameObject); yield return null;
        }
    }
    [UnityTest] public IEnumerator EachEnemyHasDistinctAnimatedGeometryAndUsableCombatClips()
    {
        var meshes=new HashSet<Mesh>();
        meshes.Add(game.Player.Motion.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh);
        foreach(EnemyKind kind in System.Enum.GetValues(typeof(EnemyKind)))
        {
            var enemy=game.SpawnDrone(new Vector3(0,1,-2),kind); enemy.enabled=false;
            var animation=enemy.Motion; var skin=animation.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.IsNotNull(skin); Assert.IsTrue(meshes.Add(skin.sharedMesh),"Each role must use different geometry, not just tint or scale");
            foreach(string clip in new[]{"Idle","RunForward","Fire","Melee","Cast","Death"})Assert.IsNotNull(animation.Player[clip]);
            var a=new Mesh(); var b=new Mesh(); animation.enabled=false;
            animation.Player.Play("RunForward"); animation.Player["RunForward"].normalizedTime=.1f;animation.Player.Sample();skin.BakeMesh(a,true);
            Matrix4x4 firstPose=skin.localToWorldMatrix;
            animation.Player["RunForward"].normalizedTime=.6f;animation.Player.Sample();skin.BakeMesh(b,true);
            Matrix4x4 secondPose=skin.localToWorldMatrix;
            float changed=0; var av=a.vertices; var bv=b.vertices;
            for(int i=0;i<av.Length;i++)changed=Mathf.Max(changed,Vector3.Distance(firstPose.MultiplyPoint3x4(av[i]),secondPose.MultiplyPoint3x4(bv[i])));
            Assert.Greater(changed,.01f,kind+" locomotion must animate its actual mesh");
            Object.Destroy(a);Object.Destroy(b);Object.Destroy(enemy.gameObject);yield return null;
        }
        Assert.AreEqual(5,meshes.Count);
    }
    [UnityTest] public IEnumerator MusicCrossfadesBetweenZonesAndMuteAffectsBothChannels()
    {
        var music=game.Music;
        Assert.AreEqual("Nexus",music.Theme);Assert.AreNotSame(music.Nexus.clip,music.Battle.clip);
        Assert.Greater(music.Nexus.clip.length,30);Assert.Greater(music.Battle.clip.length,30);
        Assert.IsTrue(music.Nexus.loop);Assert.IsTrue(music.Battle.loop);
        game.StartMission();game.Director.enabled=false;
        foreach(var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))enemy.enabled=false;
        yield return new WaitForSeconds(Soundtrack.FadeSeconds+.2f);
        Assert.AreEqual("Battle",music.Theme);Assert.Greater(music.Battle.volume,.15f);Assert.AreEqual(0,music.Nexus.volume);
        music.Toggle();Assert.IsTrue(music.Nexus.mute);Assert.IsTrue(music.Battle.mute);
        music.Toggle();Assert.IsFalse(music.Nexus.mute);
        game.Player.Damage(100);yield return new WaitForSeconds(1.8f);
        Assert.AreEqual("Nexus",music.Theme);
        yield return new WaitForSeconds(Soundtrack.FadeSeconds+.2f);
        Assert.Greater(music.Nexus.volume,.2f);Assert.AreEqual(0,music.Battle.volume);
    }
}
