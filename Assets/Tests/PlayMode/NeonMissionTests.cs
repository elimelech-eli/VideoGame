using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;
using VideoGame.Core;

public sealed class NeonMissionTests
{
    Prototype game;
    [UnitySetUp]public IEnumerator Setup(){var old=Object.FindFirstObjectByType<Prototype>();if(old)Object.Destroy(old.gameObject);yield return null;game=new GameObject("Neon integration test").AddComponent<Prototype>();yield return null;}
    [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;AudioListener.pause=false;if(game)Object.Destroy(game.gameObject);yield return null;}
    [Test]public void SeededRecipesAndWholeVolumesRespectTheEntireGraph()
    {
        foreach(int count in new[]{6,10,20})foreach(int seed in new[]{1,73,104,1183,-26})
        {
            var rooms=MapLayout.Generate(seed,count);Assert.AreEqual(3,Enumerable.Range(0,count).Select(i=>NeonBiome.Recipe(i,seed)).Distinct().Count());
            var plan=NeonBiome.Plan(rooms,seed);var repeat=NeonBiome.Plan(rooms,seed);Assert.AreEqual(plan.Count,repeat.Count);
            for(int i=0;i<plan.Count;i++)
            {
                Assert.AreEqual(plan[i].Asset,repeat[i].Asset);Assert.AreEqual(plan[i].Position,repeat[i].Position);Assert.AreEqual(plan[i].Size,repeat[i].Size);
                var b=plan[i].Bounds;var footprint=new Rect(b.min.x-.2f,b.min.z-.2f,b.size.x+.4f,b.size.z+.4f);
                Assert.IsFalse(NeonBiome.ProtectedAreas(rooms).Any(a=>a.Overlaps(footprint)),"Whole building intersects room/passage");
                Assert.Greater(b.size.y,10,"Whole building proportions were miniaturized");
                Assert.IsFalse(NeonBiome.BlocksGameplayView(b,rooms),"Whole building hides another room/passage at gameplay camera");
                for(int j=i+1;j<plan.Count;j++)Assert.IsFalse(b.Intersects(plan[j].Bounds),"Whole building reservations overlap");
            }
        }
    }
    [UnityTest]public IEnumerator MissionSizesKeepRoutesClearAndAllThreeRecipes()
    {
        foreach(int count in new[]{6,10,20})
        {
            if(game.InMission){game.Mission.Abandon();game.BuildHub();yield return null;}
            game.StartMission((int)BiomeId.NeonStreets,count);game.Director.enabled=false;game.Player.enabled=false;
            foreach(var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None)){enemy.enabled=false;foreach(var collider in enemy.GetComponentsInChildren<Collider>())collider.enabled=false;}
            yield return null;Physics.SyncTransforms();Assert.AreEqual(count,game.Layout.Count);BiomeVisualEvidence.Capture(game,BiomeId.NeonStreets,count);
            var adapter=game.GetComponent<NeonBiome>();Assert.IsNotNull(adapter,"Shared mission must call NeonBiome.Begin");
            foreach(var recipe in new[]{NeonRoomRecipe.SlumCourtyard,NeonRoomRecipe.CorporatePlaza,NeonRoomRecipe.TransitionStreet})
                Assert.IsTrue(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t=>t.name.StartsWith("Neon room / "+recipe)),"Missing recipe "+recipe);
            foreach(var a in game.Layout)foreach(var b in game.Layout)
            {
                if(!MapLayout.Adjacent(a,b))continue;var start=new Vector3(a.X*20,1,a.Y*20);var end=new Vector3(b.X*20,1,b.Y*20);var across=Vector3.Cross(Vector3.up,(end-start).normalized);
                for(int step=0;step<=20;step++)foreach(float lane in new[]{-1.3f,0,1.3f})
                {var p=Vector3.Lerp(start,end,step/20f)+across*lane;Assert.IsTrue(Physics.Raycast(p,Vector3.down,2,1<<8),"Neon floor seam "+p);Assert.IsFalse(Physics.CheckCapsule(p-Vector3.up*.5f,p+Vector3.up*.5f,.4f,1<<9),"Neon route obstruction "+p);}
            }
            foreach(var placed in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Where(c=>c.name.StartsWith("Neon whole building / ")))
            {
                Assert.Greater(placed.bounds.size.y,10);Assert.That(placed.bounds.min.y,Is.InRange(-.02f,.02f));
                var b=placed.bounds;var rect=new Rect(b.min.x,b.min.z,b.size.x,b.size.z);Assert.IsFalse(NeonBiome.ProtectedAreas(game.Layout).Any(p=>p.Overlaps(rect)));
            }
        }
    }
    [UnityTest]public IEnumerator LowFacadesHaveNoInvisibleTallCollision()
    {
        var root=new GameObject("Neon isolated collision fixture");var adapter=root.AddComponent<NeonBiome>();var room=new Room(250,250,14);adapter.Begin(new[]{room},104,root.transform);
        // Isolate walls from the whole-building reservation near z5008.
        var p=new Vector3(5000,0,4900);adapter.Wall(p,Vector3.right,5,Vector2Int.up,root.transform);adapter.Wall(p+Vector3.forward*8,Vector3.right,5,Vector2Int.down,root.transform);yield return null;Physics.SyncTransforms();
        foreach(float x in new[]{-2.4f,-1,0,1,2.4f})Assert.IsTrue(Physics.Raycast(p+new Vector3(x,1.5f,-2),Vector3.forward,4,1<<9));
        Assert.IsFalse(Physics.Raycast(p+new Vector3(0,1.5f,6),Vector3.forward,4,1<<9));Assert.IsTrue(Physics.Raycast(p+new Vector3(0,.3f,6),Vector3.forward,4,1<<9));Object.Destroy(root);
    }
}
