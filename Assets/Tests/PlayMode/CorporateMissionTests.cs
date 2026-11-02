using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;
using VideoGame.Core;

public sealed class CorporateMissionTests
{
    Prototype game;
    [UnitySetUp]public IEnumerator Setup(){var old=Object.FindFirstObjectByType<Prototype>();if(old)Object.Destroy(old.gameObject);yield return null;game=new GameObject("Corporate integration test").AddComponent<Prototype>();yield return null;}
    [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;AudioListener.pause=false;if(game)Object.Destroy(game.gameObject);yield return null;}
    [UnityTest]public IEnumerator GeneratedCorporateRoutesRemainClearAcrossAllSizes()
    {
        foreach(int count in new[]{6,10,20})
        {
            if(game.InMission){game.Mission.Abandon();game.BuildHub();yield return null;}
            game.StartMission((int)BiomeId.CorporateOffice,count);game.Director.enabled=false;game.Player.enabled=false;
            foreach(var d in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None)){d.enabled=false;foreach(var c in d.GetComponentsInChildren<Collider>())c.enabled=false;}
            yield return null;Physics.SyncTransforms();Assert.AreEqual(count,game.Layout.Count);BiomeVisualEvidence.Capture(game,BiomeId.CorporateOffice,count);
            Assert.AreEqual(count,Object.FindObjectsByType<BiomeMotion>(FindObjectsSortMode.None).Count(m=>m.name.StartsWith("Corporate room / ")));
            foreach(var a in game.Layout)foreach(var b in game.Layout)
            {
                if(!MapLayout.Adjacent(a,b))continue;Vector3 start=new Vector3(a.X*20,1,a.Y*20),end=new Vector3(b.X*20,1,b.Y*20),across=Vector3.Cross(Vector3.up,(end-start).normalized);
                for(int step=0;step<=20;step++)foreach(float lane in new[]{-1.3f,0,1.3f})
                {
                    var p=Vector3.Lerp(start,end,step/20f)+across*lane;
                    Assert.IsTrue(Physics.Raycast(p,Vector3.down,2,1<<8),"Corporate floor gap "+p);
                    Assert.IsFalse(Physics.CheckCapsule(p-Vector3.up*.5f,p+Vector3.up*.5f,.4f,1<<9),"Corporate blocked route "+p);
                }
            }
            foreach(var solid in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Where(c=>c.name.StartsWith("Corporate / ")&&!c.name.Contains("Floor")&&!c.name.Contains("Wall")&&!c.name.Contains("Corner")))
                Assert.IsFalse(Physics.OverlapBox(solid.bounds.center,solid.bounds.extents*.96f,Quaternion.identity,1<<9).Any(c=>c!=solid),"Corporate furniture overlaps scenery/hazard: "+solid.name);
        }
    }
    [UnityTest]public IEnumerator CorporateFullWallBlocksFireAndCutawayHasNoInvisibleHeight()
    {
        var root=new GameObject("Corporate collision fixture");root.transform.position=new Vector3(3000,0,3000);
        CorporateBiome.Wall(root.transform.position,Vector3.right,5,Vector2Int.up,root.transform);
        CorporateBiome.Wall(root.transform.position+Vector3.forward*8,Vector3.right,5,Vector2Int.down,root.transform);
        yield return null;Physics.SyncTransforms();
        foreach(float x in new[]{-2.4f,-1f,0,1,2.4f})Assert.IsTrue(Physics.Raycast(root.transform.position+new Vector3(x,1.5f,-2),Vector3.forward,4,1<<9));
        Assert.IsFalse(Physics.Raycast(root.transform.position+new Vector3(0,1.5f,6),Vector3.forward,4,1<<9));
        Assert.IsTrue(Physics.Raycast(root.transform.position+new Vector3(0,.3f,6),Vector3.forward,4,1<<9));Object.Destroy(root);
    }
}
