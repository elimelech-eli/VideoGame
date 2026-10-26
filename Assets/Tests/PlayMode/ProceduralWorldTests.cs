using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;
using VideoGame.Core;

public sealed class ProceduralWorldTests
{
    Prototype game;
    [UnitySetUp] public IEnumerator Setup()
    {
        var old=Object.FindFirstObjectByType<Prototype>();if(old!=null)Object.Destroy(old.gameObject);
        yield return null;game=new GameObject("Procedural world test").AddComponent<Prototype>();yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(game!=null)Object.Destroy(game.gameObject);yield return null;}
    [UnityTest] public IEnumerator SeededRoomsHaveTraversableDoorwaysAndPassages()
    {
        for(int seed=0;seed<4;seed++)
        {
            game.StartMission();game.Director.enabled=false;game.Player.enabled=false;
            foreach(var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))enemy.enabled=false;
            Physics.SyncTransforms();
            var rooms=game.Layout;Assert.AreEqual(8,rooms.Count);
            foreach(var a in rooms)foreach(var b in rooms)
            {
                if(!MapLayout.Adjacent(a,b))continue;
                Vector3 start=new Vector3(a.X*MapLayout.Spacing,1,a.Y*MapLayout.Spacing);
                Vector3 end=new Vector3(b.X*MapLayout.Spacing,1,b.Y*MapLayout.Spacing);
                for(int step=0;step<=40;step++)
                {
                    Vector3 point=Vector3.Lerp(start,end,step/40f);
                    Assert.IsTrue(Physics.Raycast(point,Vector3.down,2,1<<8),"Missing floor at "+point+" seed "+game.Seed);
                    Assert.IsFalse(Physics.CheckCapsule(point-Vector3.up*.5f,point+Vector3.up*.5f,.42f,1<<9),"Blocked passage at "+point+" seed "+game.Seed);
                }
            }
            Assert.IsFalse(Physics.CheckSphere(game.Terminal + Vector3.forward * 2 + Vector3.up * .5f,.6f,1<<9),"Extraction must be clear");
            game.Mission.Abandon();game.BuildHub();yield return null;
        }
    }
    [UnityTest] public IEnumerator PursuitTraversesTheRoomGraphWithoutCrossingWalls()
    {
        game.StartMission();game.Director.enabled=false;
        foreach(var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))enemy.enabled=false;
        Physics.SyncTransforms();
        foreach(var room in game.Layout)
        {
            Vector3 point=new Vector3(room.X*MapLayout.Spacing+1.5f,1,room.Y*MapLayout.Spacing+1.5f);
            int steps=0;
            while(Vector3.Distance(point,Vector3.up)>.4f && steps++<1000)
            {
                Vector3 destination=game.PursuitTarget(point,Vector3.up);
                point=Vector3.MoveTowards(point,destination,.3f);
                Assert.IsTrue(Physics.Raycast(point,Vector3.down,2,1<<8),"Pursuit left the deck");
                Assert.IsFalse(Physics.CheckSphere(point,.45f,1<<9),"Pursuit crossed a solid wall");
            }
            Assert.Less(steps,1000,"Pursuit could not reach the player room");
        }
        yield return null;
    }
    [UnityTest] public IEnumerator RangedEnemyMovesWhenPermanentCoverBlocksItsShot()
    {
        game.StartMission();game.Director.enabled=false;game.Player.enabled=false;
        foreach(var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))enemy.enabled=false;
        game.Player.Teleport(new Vector3(0,1,4));
        var cover=GameObject.CreatePrimitive(PrimitiveType.Cube);cover.transform.position=new Vector3(0,1,2);
        cover.transform.localScale=new Vector3(4,2,.3f);cover.layer=9;
        var gunner=game.SpawnDrone(Vector3.up,EnemyKind.Gunner);Physics.SyncTransforms();
        yield return new WaitForSeconds(.3f);
        Assert.Greater(gunner.transform.position.z,.2f,"A gunner in range should pursue a doorway rather than attack a permanent wall");
        Assert.IsFalse(gunner.WindingUp);
        Object.Destroy(cover);yield return null;
    }
    [UnityTest] public IEnumerator RotatedWallVisibleTipAndCrushVolumeContinueBeyondImpact()
    {
        game.StartMission();game.Director.enabled=false;game.Player.enabled=false;
        foreach(var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))enemy.enabled=false;
        foreach(float yaw in new[]{0f,90f,180f,270f})
        {
            Vector3 direction=Quaternion.Euler(0,yaw,0)*Vector3.forward;
            game.Player.Teleport(direction*1.8f+Vector3.up);game.Player.Health=100;
            var wall=game.SpawnDestructible(Vector3.zero,true);wall.transform.rotation=Quaternion.Euler(0,yaw,0);
            var mesh=wall.GetComponentInChildren<MeshFilter>();
            Vector3 tip=Vector3.zero;int count=0;
            foreach(var vertex in mesh.sharedMesh.vertices)
            {
                Vector3 point=mesh.transform.TransformPoint(vertex);
                if(point.y>2.25f){tip+=point;count++;}
            }
            Assert.Greater(count,0);tip=wall.transform.InverseTransformPoint(tip/count);
            Physics.SyncTransforms();
            for(int shot=0;shot<4;shot++)
            {
                var bolt=new GameObject("Directional wall regression shot");bolt.transform.position=-direction*2+Vector3.up;
                bolt.AddComponent<EnemyBolt>().Direction=direction;yield return new WaitForSeconds(.3f);
            }
            yield return new WaitForSeconds(.8f);
            Vector3 fallenTip=wall.transform.TransformPoint(tip)-wall.transform.position;fallenTip.y=0;
            Assert.Greater(Vector3.Dot(fallenTip.normalized,direction),.99f);
            Assert.AreEqual(65,game.Player.Health,"Player beyond the hit must take one crushing hit");
            Object.Destroy(wall.gameObject);yield return null;
        }
    }
}
