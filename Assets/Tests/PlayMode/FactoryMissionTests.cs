using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;
using VideoGame.Core;
public sealed class FactoryMissionTests
{
    Prototype game;
    [UnitySetUp]public IEnumerator Setup(){var old=Object.FindFirstObjectByType<Prototype>();if(old)Object.Destroy(old.gameObject);yield return null;game=new GameObject("Factory integration test").AddComponent<Prototype>();yield return null;}
    [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;AudioListener.pause=false;if(game)Object.Destroy(game.gameObject);yield return null;}
    void Quiet(){game.Director.enabled=false;game.Player.enabled=false;foreach(var d in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None)){d.enabled=false;foreach(var c in d.GetComponentsInChildren<Collider>())c.enabled=false;}}
    void Capture(string name,Vector3 focus,float size)
    {
        if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;string dir=System.Environment.GetEnvironmentVariable("FACTORY_CAPTURE_DIR");if(string.IsNullOrEmpty(dir))return;Directory.CreateDirectory(dir);
        var cam=game.Player.View;cam.transform.rotation=Quaternion.Euler(50,45,0);cam.transform.position=focus-cam.transform.forward*30;cam.orthographicSize=size;
        var rt=new RenderTexture(1440,1000,24){antiAliasing=4};cam.targetTexture=rt;cam.Render();var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1440,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,1000),0,0);image.Apply();File.WriteAllBytes(Path.Combine(dir,name+".png"),image.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=prior;rt.Release();Object.Destroy(image);Object.Destroy(rt);
    }
    [UnityTest]public IEnumerator GeneratedFactoryRoutesAndSourceKitAcrossSizes()
    {
        foreach(int count in new[]{6,10,20})
        {
            if(game.InMission){game.Mission.Abandon();game.BuildHub();yield return null;}
            game.StartMission((int)BiomeId.RuinedFactory,count);Quiet();yield return null;Physics.SyncTransforms();Assert.AreEqual(count,game.Layout.Count);
            Assert.AreEqual(count,Object.FindObjectsByType<BiomeMotion>(FindObjectsSortMode.None).Count(m=>m.name.StartsWith("Factory room / ")));
            foreach(var a in game.Layout)foreach(var b in game.Layout)
            {
                if(!MapLayout.Adjacent(a,b))continue;var start=new Vector3(a.X*20,1,a.Y*20);var end=new Vector3(b.X*20,1,b.Y*20);var across=Vector3.Cross(Vector3.up,(end-start).normalized);
                for(int step=0;step<=20;step++)foreach(float lane in new[]{-1.3f,0,1.3f}){var point=Vector3.Lerp(start,end,step/20f)+across*lane;Assert.IsTrue(Physics.Raycast(point,Vector3.down,2,1<<8),"Factory floor seam "+point);Assert.IsFalse(Physics.CheckCapsule(point-Vector3.up*.5f,point+Vector3.up*.5f,.4f,1<<9),"Factory route obstruction "+point);}
            }
            Assert.Greater(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name.StartsWith("Factory / ")),count*12);
            foreach(var solid in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Where(c=>c.name.StartsWith("Factory / ")&&!c.name.Contains("Floor")&&!c.name.Contains("Wall"))){var hits=Physics.OverlapBox(solid.bounds.center,solid.bounds.extents*.96f,Quaternion.identity,1<<9);Assert.IsFalse(hits.Any(c=>c!=solid),"Factory furniture overlaps scenery/hazard: "+solid.name);}
            Capture(count+"-arrival",new Vector3(0,.5f,0),7.5f);var room=game.Layout[count/2];Capture(count+"-room",new Vector3(room.X*20,.5f,room.Y*20),9);
        }
    }
    [UnityTest]public IEnumerator DedicatedFactoryRosterAnimatesAndShieldAbsorbsDamage()
    {
        game.StartMission((int)BiomeId.RuinedFactory,6);Quiet();yield return null;
        foreach(var d in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))Object.Destroy(d.gameObject);yield return null;
        game.Player.Teleport(new Vector3(-4,1,-4));
        foreach(var kind in new[]{EnemyKind.Rusher,EnemyKind.Gunner,EnemyKind.Bombardier,EnemyKind.Hunter})
        {
            var d=game.SpawnDrone(new Vector3(0,1,0),kind);d.enabled=false;d.transform.rotation=Quaternion.LookRotation(new Vector3(-1,0,-1));var motion=d.Motion;Assert.IsNotNull(motion);Assert.IsTrue(motion.name.StartsWith("Factory"));motion.enabled=false;motion.Player.enabled=false;
            foreach(string state in new[]{"Idle","RunForward","Melee","Fire","Cast","Hit","Death"})Assert.IsNotNull(motion.Player[state]);
            var skin=motion.GetComponentInChildren<SkinnedMeshRenderer>();Assert.IsNotNull(skin);var a=new Mesh();var b=new Mesh();motion.Player.Play("RunForward");motion.Player["RunForward"].normalizedTime=.2f;motion.Player.Sample();skin.BakeMesh(a);motion.Player["RunForward"].normalizedTime=.7f;motion.Player.Sample();skin.BakeMesh(b);
            Assert.Greater(a.vertices.Zip(b.vertices,(x,y)=>Vector3.Distance(x,y)).Max(),.002f,"Weighted Factory skin is static: "+kind);
            var bounds=Art.RenderedBounds(motion.gameObject);Assert.That(bounds.size.y,Is.InRange(.3f,6));Assert.Less(bounds.size.magnitude,10,"Exploded skin/bind pose: "+kind);
            Capture(kind+"-run",bounds.center,3.2f);motion.Player.Play("Idle");motion.Player["Idle"].normalizedTime=.3f;motion.Player.Sample();Capture(kind+"-idle",Art.RenderedBounds(motion.gameObject).center,3.2f);
            if(kind==EnemyKind.Hunter){Assert.IsTrue(d.IsFactoryHeavy);Assert.IsTrue(d.Ranged);float h=d.Health,s=d.Shield;d.Damage(20);Assert.AreEqual(h,d.Health);Assert.AreEqual(s-20,d.Shield);}else Assert.AreEqual(0,d.Shield);
            d.Damage(d.Health+d.Shield+1);Assert.IsTrue(motion.Dead);Object.Destroy(a);Object.Destroy(b);Object.Destroy(d.gameObject);yield return null;
        }
    }
    [UnityTest]public IEnumerator FactoryWallStopsFireWithoutInvisibleCutawayHeight()
    {
        var root=new GameObject("Factory collision fixture");root.transform.position=new Vector3(3000,0,3000);
        FactoryBiome.Wall(root.transform.position,Vector3.right,5,Vector2Int.up,root.transform);FactoryBiome.Wall(root.transform.position+Vector3.forward*8,Vector3.right,5,Vector2Int.down,root.transform);yield return null;Physics.SyncTransforms();
        foreach(float x in new[]{-2.4f,-1f,0,1,2.4f})Assert.IsTrue(Physics.Raycast(root.transform.position+new Vector3(x,1.5f,-2),Vector3.forward,4,1<<9));
        Assert.IsFalse(Physics.Raycast(root.transform.position+new Vector3(0,1.5f,6),Vector3.forward,4,1<<9));Assert.IsTrue(Physics.Raycast(root.transform.position+new Vector3(0,.3f,6),Vector3.forward,4,1<<9));Object.Destroy(root);
    }
}
