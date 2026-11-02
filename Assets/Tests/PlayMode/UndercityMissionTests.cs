using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;
using VideoGame.Core;

public sealed class UndercityMissionTests
{
    Prototype game;
    [UnitySetUp] public IEnumerator Setup()
    {
        var old=Object.FindFirstObjectByType<Prototype>();if(old)Object.Destroy(old.gameObject);
        yield return null;game=new GameObject("Undercity routed mission integration").AddComponent<Prototype>();yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {Time.timeScale=1;AudioListener.pause=false;if(game)Object.Destroy(game.gameObject);yield return null;}
    [UnityTest] public IEnumerator MissionRoutingUsesSourceKitAndRetainsTraversableConnections()
    {
        foreach(int count in new[]{6,10,20})
        {
            if(game.InMission){game.Mission.Abandon();game.BuildHub();yield return null;}
            game.StartMission((int)BiomeId.Undercity,count);game.Director.enabled=false;game.Player.enabled=false;
            foreach(var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))
            {enemy.enabled=false;foreach(var collider in enemy.GetComponentsInChildren<Collider>())collider.enabled=false;}
            yield return null;Physics.SyncTransforms();Assert.AreEqual(count,game.Layout.Count);BiomeVisualEvidence.Capture(game,BiomeId.Undercity,count);
            Assert.AreEqual(count,Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name.StartsWith("Undercity room / ")));
            Assert.Greater(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name.StartsWith("Undercity / ")),count*8);
            foreach(var a in game.Layout)foreach(var b in game.Layout)
            {
                if(!MapLayout.Adjacent(a,b))continue;
                var start=new Vector3(a.X*20,1,a.Y*20);var end=new Vector3(b.X*20,1,b.Y*20);var across=Vector3.Cross(Vector3.up,(end-start).normalized);
                for(int step=0;step<=20;step++)foreach(float lane in new[]{-1.3f,0,1.3f})
                {
                    var p=Vector3.Lerp(start,end,step/20f)+across*lane;
                    Assert.IsTrue(Physics.Raycast(p,Vector3.down,2,1<<8),"Routed Undercity floor seam "+p);
                    Assert.IsFalse(Physics.CheckCapsule(p-Vector3.up*.5f,p+Vector3.up*.5f,.4f,1<<9),"Routed Undercity obstruction "+p);
                }
            }
        }
    }
}
