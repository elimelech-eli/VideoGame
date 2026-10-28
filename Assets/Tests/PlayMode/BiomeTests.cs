using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;
using VideoGame.Core;

public sealed class BiomeTests
{
    Prototype game;
    [UnitySetUp] public IEnumerator Setup()
    {
        var old=Object.FindFirstObjectByType<Prototype>();if(old!=null)Object.Destroy(old.gameObject);
        yield return null;game=new GameObject("Biome test").AddComponent<Prototype>();yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){Time.timeScale=1;AudioListener.pause=false;Object.Destroy(game.gameObject);yield return null;}
    void Quiet()
    {
        game.Director.enabled=false;game.Player.enabled=false;
        foreach(var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))enemy.enabled=false;
    }
    [UnityTest] public IEnumerator EveryBiomeAndSizeHasRealKitAndClearConnectedPassages()
    {
        for(int biome=0;biome<6;biome++)foreach(int size in new[]{6,10,20})
        {
            game.Menus.Settings.Biome=biome;game.Menus.Settings.SectorSize=size;
            game.StartMission();Quiet();yield return null;Physics.SyncTransforms();
            Assert.AreEqual(size,game.Layout.Count);Assert.AreEqual((BiomeId)biome,game.ActiveBiome);
            Assert.AreEqual((BiomeId)biome,game.Music.Biome);
            Assert.AreEqual(biome.ToString(),game.Music.Battle.clip.name);
            Assert.LessOrEqual(Object.FindObjectsByType<Drone>(FindObjectsSortMode.None).Count(e=>e.Health>0),EncounterDirector.PopulationCap);
            foreach(var a in game.Layout)foreach(var b in game.Layout)
            {
                if(!MapLayout.Adjacent(a,b))continue;
                Vector3 start=new Vector3(a.X*20,1,a.Y*20),end=new Vector3(b.X*20,1,b.Y*20);
                Vector3 across=Vector3.Cross(Vector3.up,(end-start).normalized);
                for(int step=0;step<=20;step++)foreach(float lane in new[]{-1.4f,0,1.4f})
                {
                    Vector3 point=Vector3.Lerp(start,end,step/20f)+across*lane;
                    Assert.IsTrue(Physics.Raycast(point,Vector3.down,2,1<<8),"Floor gap / biome "+biome+" "+point);
                    var blockers=Physics.OverlapCapsule(point-Vector3.up*.5f,point+Vector3.up*.5f,.42f,1<<9);
                    Assert.AreEqual(0,blockers.Length,"Door blocked / biome "+biome+" "+point+" "+string.Join(";",blockers.Select(c=>c.name+" "+c.bounds)));
                }
            }
            Assert.IsFalse(Physics.CheckSphere(game.Terminal+Vector3.forward*2+Vector3.up*.5f,.6f,1<<9));
            var floor=Resources.Load<GameObject>("Biomes/Prefabs/"+biome+"_Floor");
            var material=Resources.Load<Material>("Biomes/Materials/"+biome+"_Floor");
            Assert.IsNotNull(material.mainTexture,"Floor material lost its texture");
            Assert.IsNotNull(material.GetTexture("_BumpMap"),"Floor material lost its normal map");
            var bounds=Art.RenderedBounds(floor);Assert.That(bounds.size.x,Is.EqualTo(4).Within(.1f));Assert.That(bounds.size.z,Is.EqualTo(4).Within(.1f));
            var wall=Resources.Load<GameObject>("Biomes/Prefabs/"+biome+"_Wall");
            bounds=Art.RenderedBounds(wall);Assert.That(bounds.size.x,Is.EqualTo(4).Within(.1f));
            Assert.That(bounds.size.y,Is.EqualTo(new[]{4.2f,4.2f,3,3,3.7f,4.2f}[biome]).Within(.12f));
            foreach(string role in new[]{"WallB","Room","Landmark","Detail","Service","Vent","Volatile","Unstable"})
            {
                var asset=Resources.Load<GameObject>("Biomes/Prefabs/"+biome+"_"+role);
                Assert.IsNotNull(asset,"Incomplete biome kit: "+biome+" / "+role);
                Assert.Greater(Art.RenderedBounds(asset).size.sqrMagnitude,.05f);
            }
            // Dressing must coexist with the actual hazard and wall hulls, not just
            // leave the centre-to-centre graph clear. Ignore floor contact and self.
            foreach(var solid in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
            {
                if(!solid.name.StartsWith(biome+"_")||solid.name.Contains("Wall"))continue;
                var hit=Physics.OverlapBox(solid.bounds.center,solid.bounds.extents*.96f,Quaternion.identity,1<<9);
                Assert.IsFalse(hit.Any(other=>other!=solid),"Dressing overlap: "+solid.name+" / "+string.Join(",",hit.Where(other=>other!=solid).Select(other=>other.name)));
            }
            game.Mission.Abandon();game.BuildHub();yield return null;
        }
    }
    [UnityTest] public IEnumerator DressingReplaysAndMotionPausesWithoutTouchingGameplay()
    {
        game.Menus.Settings.Biome=0;game.Menus.Settings.SectorSize=6;
        game.NewSession();game.StartMission();Quiet();yield return null;
        int hash=game.Biomes.DressingHash;var clip=game.Music.Battle.clip;
        var motion=Object.FindObjectsByType<BiomeMotion>(FindObjectsSortMode.None).First(m=>m.Rotors.Count>0&&(m.transform.position-game.Player.transform.position).sqrMagnitude<100);
        var rotor=motion.Rotors[0];Quaternion pose=rotor.rotation;float health=game.Player.Health;
        yield return new WaitForSeconds(.15f);Assert.Greater(Quaternion.Angle(pose,rotor.rotation),1);
        game.Menus.Pause();pose=rotor.rotation;
        yield return new WaitForSecondsRealtime(.15f);Assert.AreEqual(pose,rotor.rotation);Assert.AreEqual(health,game.Player.Health);
        game.Menus.Resume();yield return null;yield return null;
        game.NewSession();game.StartMission();Quiet();yield return null;
        Assert.AreEqual(hash,game.Biomes.DressingHash);Assert.AreSame(clip,game.Music.Battle.clip);
        game.BuildHub();yield return null;
        Assert.AreEqual(0,Object.FindObjectsByType<BiomeMotion>(FindObjectsSortMode.None).Count(m=>m.Rotors.Count>0),"Mission motion leaked into Nexus");
    }
    [UnityTest] public IEnumerator BiomeMusicIsDistinctAndAllChannelsRespectMute()
    {
        var tracks=new System.Collections.Generic.HashSet<AudioClip>();
        for(int biome=0;biome<6;biome++)
        {
            game.Music.SetBiome((BiomeId)biome);tracks.Add(game.Music.Battle.clip);
            Assert.Greater(game.Music.Battle.clip.length,30);
        }
        Assert.AreEqual(6,tracks.Count);
        game.Music.SetEnabled(false);
        foreach(var source in game.GetComponentsInChildren<AudioSource>())if(source.name.StartsWith("Biome "))Assert.IsTrue(source.mute);
        game.Music.SetEnabled(true);yield return null;
    }
}
