using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;
using VideoGame.Core;

public sealed class LabMissionTests
{
    Prototype game;
    [UnitySetUp] public IEnumerator Setup()
    {
        var old=Object.FindFirstObjectByType<Prototype>();if(old!=null)Object.Destroy(old.gameObject);
        yield return null;game=new GameObject("Lab mission test").AddComponent<Prototype>();yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){Time.timeScale=1;AudioListener.pause=false;Object.Destroy(game.gameObject);yield return null;}
    void Quiet()
    {
        game.Director.enabled=false;game.Player.enabled=false;
        foreach(var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))
        {enemy.enabled=false;foreach(var collider in enemy.GetComponentsInChildren<Collider>())collider.enabled=false;}
    }
    [UnityTest] public IEnumerator SeededLabUsesSourceKitAndKeepsTraversalAndDressingValid()
    {
        foreach(int seed in new[]{71,404,104729})foreach(int count in new[]{6,10,20})
        {
            game.RestoreCheckpoint(new NexusCheckpoint{Format="nexus-checkpoint",Version=1,Id=System.Guid.NewGuid().ToString(),SavedUtc=System.DateTime.UtcNow.ToString("O"),Target=8,Seed=seed});
            game.Menus.Settings.Biome=(int)BiomeId.BiotechLab;game.Menus.Settings.SectorSize=count;
            game.StartMission();Quiet();yield return null;Physics.SyncTransforms();
            Assert.AreEqual(count,game.Layout.Count);
            var solids=Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Where(c=>c.name.StartsWith("Lab / ")).ToArray();
            Assert.Greater(solids.Length,count*20,"Production kit did not replace basic architecture");
            Assert.AreEqual(count,Object.FindObjectsByType<BiomeMotion>(FindObjectsSortMode.None).Count(m=>m.name.StartsWith("Lab workspace / ")));
            Assert.IsFalse(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t=>t.name.StartsWith("3_")&&!t.name.Contains("Volatile")&&!t.name.Contains("Unstable")),"Old lab art is still instantiated");
            foreach(var solid in solids)
            {
                Assert.AreEqual(Vector3.one,solid.transform.localScale,"Detailed source module was stretched");
                if(solid.name.Contains("Floor")||solid.name.Contains("Wall")||solid.name.Contains("Door"))continue;
                var hits=Physics.OverlapBox(solid.bounds.center,solid.bounds.extents*.96f,Quaternion.identity,1<<9);
                Assert.IsFalse(hits.Any(other=>other!=solid),solid.name+" overlaps "+string.Join(",",hits.Where(other=>other!=solid).Select(c=>c.name)));
            }
            foreach(var a in game.Layout)foreach(var b in game.Layout)
            {
                if(!MapLayout.Adjacent(a,b))continue;
                Vector3 start=new Vector3(a.X*20,1,a.Y*20),end=new Vector3(b.X*20,1,b.Y*20),across=Vector3.Cross(Vector3.up,(end-start).normalized);
                for(int step=0;step<=40;step++)foreach(float lane in new[]{-1.4f,0,1.4f})
                {
                    Vector3 point=Vector3.Lerp(start,end,step/40f)+across*lane;
                    Assert.IsTrue(Physics.Raycast(point,Vector3.down,2,1<<8),"Floor seam "+seed+" / "+point);
                    Assert.IsFalse(Physics.CheckCapsule(point-Vector3.up*.5f,point+Vector3.up*.5f,.42f,1<<9),"Blocked doorway "+seed+" / "+point);
                }
            }
            var first=game.Layout[0];var neighbor=game.Layout.First(r=>MapLayout.Adjacent(first,r));
            Vector3 destination=new Vector3(neighbor.X*20,1.15f,neighbor.Y*20);
            game.Player.Teleport(new Vector3(0,1.15f,0));var controller=game.Player.GetComponent<CharacterController>();
            Vector3 direction=(destination-game.Player.transform.position).normalized;
            for(int step=0;step<200;step++)controller.Move(direction*.1f);
            Assert.Less(Vector3.Distance(destination,game.Player.transform.position),.2f,"Actual controller cannot traverse room-to-room: "+game.Player.transform.position+" expected "+destination);
            int hash=game.Biomes.DressingHash;
            game.Mission.Abandon();game.BuildHub();yield return null;
            Assert.IsFalse(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t=>t.name.StartsWith("Lab / ")),"Lab geometry leaked into Nexus");
            Assert.AreEqual(UnityEngine.Rendering.AmbientMode.Flat,RenderSettings.ambientMode);
            Assert.AreNotEqual(17,hash);
        }
    }
    [UnityTest] public IEnumerator LabWallsStopShotsAndVisibleCutawaysHaveMatchingCollision()
    {
        var root=new GameObject("Wall fixture");root.transform.position=new Vector3(3000,0,3000);
        try
        {
            LabBiome.Wall(root.transform.position,Vector3.right,5,Vector2Int.up,root.transform);
            LabBiome.Wall(root.transform.position+Vector3.forward*8,Vector3.right,7,Vector2Int.down,root.transform);
            yield return null;Physics.SyncTransforms();
            foreach(float x in new[]{-2.4f,-1f,0,1f,2.4f})
                Assert.IsTrue(Physics.Raycast(root.transform.position+new Vector3(x,1.5f,-2),Vector3.forward,4,1<<9),"Full wall lets gunfire through at "+x+" / "+string.Join("; ",root.GetComponentsInChildren<BoxCollider>().Select(c=>c.name+" "+c.bounds+" layer="+c.gameObject.layer)));
            Assert.IsTrue(Physics.Raycast(root.transform.position+new Vector3(0,.4f,6),Vector3.forward,4,1<<9));
            Assert.IsFalse(Physics.Raycast(root.transform.position+new Vector3(0,1.5f,6),Vector3.forward,4,1<<9),"Invisible full-height cutaway collider");
        }
        finally{Object.Destroy(root);}
    }
}
