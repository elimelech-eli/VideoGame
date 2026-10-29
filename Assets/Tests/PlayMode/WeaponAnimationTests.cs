using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using VideoGame;
using VideoGame.Core;
using Object=UnityEngine.Object;

public sealed class WeaponAnimationTests
{
    Prototype game;
    Mouse mouse;
    string directory;
    [UnitySetUp] public IEnumerator Setup()
    {
        var old=Object.FindFirstObjectByType<Prototype>();if(old!=null)Object.Destroy(old.gameObject);yield return null;
        mouse=InputSystem.AddDevice<Mouse>();game=new GameObject("Weapon animation test").AddComponent<Prototype>();
        directory=Path.Combine(Path.GetTempPath(),"WeaponAnimation-"+System.Guid.NewGuid());game.Menus.UseStorage(directory);yield return null;
        game.Menus.Settings.Biome=0;game.StartMission();game.Director.enabled=false;game.Player.enabled=false;
        foreach(var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))enemy.gameObject.SetActive(false);
        game.Player.transform.rotation=Quaternion.Euler(0,225,0);game.Player.Motion.Player["Idle"].speed=0;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=game.Player.View.WorldToScreenPoint(game.Player.transform.position+game.Player.transform.forward*6)});yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if(game!=null)Object.Destroy(game.gameObject);if(mouse!=null)InputSystem.RemoveDevice(mouse);yield return null;
        if(Directory.Exists(directory))Directory.Delete(directory,true);
    }
    void Loadout(string main,string off)
    {
        var p=game.Equipment.Profile;
        p.Equipped[3]=main==null?null:GearCatalog.Roll(new System.Random(1),1,GearQuality.White,main);
        p.Equipped[4]=off==null?null:GearCatalog.Roll(new System.Random(2),1,GearQuality.White,off);
        game.Equipment.RefreshVisuals();game.Player.ResetHands();
    }
    void Capture(string name)
    {
        if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
        Directory.CreateDirectory("TestResults");float size=game.Player.View.orthographicSize;
        game.Player.View.orthographicSize=3;game.CaptureFrame("TestResults/weapon-"+name+".png");game.Player.View.orthographicSize=size;
    }
    [UnityTest] public IEnumerator EveryLoadoutFollowsTheAnimatedHandsWithoutMovingTheActor()
    {
        foreach(var main in new[]{"rifle","pistol","sword",null})foreach(var off in new[]{"pistol","sword","shield",null})
        {
            if(main=="rifle"&&off!=null)continue;Loadout(main,off);yield return null;
            for(int frame=0;frame<3;frame++)
            {
                game.Player.transform.position+=game.Player.transform.forward*(4*Time.deltaTime);var position=game.Player.transform.position;yield return null;
                Assert.AreEqual(position,game.Player.transform.position,"Arm presentation must not add root displacement.");
                for(int h=0;h<2;h++)
                {
                    var weapon=game.Player.Weapons.Weapon(h);var definition=h==0?main:off;
                    if(definition==null){Assert.IsNull(weapon);continue;}
                    Assert.IsNotNull(weapon);Assert.Greater(weapon.GetComponentsInChildren<MeshRenderer>().Count(r=>r.enabled),0);
                    var hand=game.Player.Motion.GetComponentsInChildren<Transform>().Single(t=>t.name==(h==0?"hand_R":"hand_L"));
                    Assert.Less(Vector3.Distance(hand.position,weapon.position),.001f);Assert.IsFalse(float.IsNaN(weapon.rotation.x));
                }
            }
        }
    }
    [UnityTest] public IEnumerator HandgunsRecoilIndependentlyAndShieldPoseTracksRealGuardState()
    {
        Loadout("pistol","pistol");yield return null;yield return null;
        var rig=game.Player.Weapons;Quaternion a=rig.Weapon(0).rotation,b=rig.Weapon(1).rotation;
        game.Player.Hands.Tick(true,false,.01f);yield return null;
        Assert.Greater(Quaternion.Angle(a,rig.Weapon(0).rotation),1);Assert.Less(Quaternion.Angle(b,rig.Weapon(1).rotation),.8f);
        Capture("dual-pistol-fire");yield return new WaitForSeconds(.3f);
        a=rig.Weapon(0).rotation;b=rig.Weapon(1).rotation;game.Player.Hands.Tick(false,true,.01f);yield return null;
        Assert.Greater(Quaternion.Angle(b,rig.Weapon(1).rotation),1);Assert.Less(Quaternion.Angle(a,rig.Weapon(0).rotation),.8f);
        Loadout("pistol","shield");yield return null;Capture("shield-lowered");
        game.Player.Hands.Tick(false,true,.1f);yield return new WaitForSeconds(.16f);
        Assert.IsTrue(game.Player.Hands.Guarding);Assert.Greater(rig.GuardWeight,.9f);Vector3 raised=rig.Weapon(1).position;Capture("shield-guard");
        float health=game.Player.Health;game.Player.Damage(20,game.Player.transform.position+game.Player.transform.forward*3);yield return null;
        Assert.AreEqual(health,game.Player.Health);Assert.Greater(Vector3.Distance(raised,rig.Weapon(1).position),.01f);Capture("shield-block");
        game.Player.Hands.Tick(false,false,.01f);yield return new WaitForSeconds(.2f);Assert.Less(rig.GuardWeight,.01f);
        game.Player.Hands.Tick(false,true,4);yield return new WaitForSeconds(.2f);Assert.IsFalse(game.Player.Hands.Guarding);Assert.Less(rig.GuardWeight,.01f);
        Loadout("rifle",null);yield return null;Capture("rifle-ready");
        game.Player.Hands.Tick(false,true,.01f);yield return new WaitForSeconds(.12f);Assert.IsTrue(game.Player.Hands.Bracing);Assert.Greater(rig.BraceWeight,.8f);Capture("rifle-brace");
        yield return new WaitForSeconds(.3f);Assert.IsFalse(game.Player.Hands.Bracing);Assert.Less(rig.BraceWeight,.1f);
    }
    [UnityTest] public IEnumerator SwordContactHasWindupHitsOnceAndPausesWithItsAnimation()
    {
        Loadout("sword","shield");yield return null;
        var enemy=game.SpawnDrone(game.Player.transform.position+game.Player.transform.forward*2,false);enemy.enabled=false;Physics.SyncTransforms();
        float health=enemy.Health;game.Player.Hands.Tick(true,false,.01f);Assert.AreEqual(health,enemy.Health);Assert.IsTrue(game.Player.Hands.HasPendingMelee(0));
        yield return new WaitForSeconds(.07f);Capture("sword-windup");game.Equipment.Open();Quaternion pose=game.Player.Weapons.Weapon(0).rotation;
        yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(health,enemy.Health);Assert.AreEqual(pose,game.Player.Weapons.Weapon(0).rotation);
        game.Menus.Resume();yield return new WaitForSeconds(.1f);game.Player.Hands.Tick(false,false,.01f);yield return null;Capture("sword-contact");
        Assert.Less(enemy.Health,health);float after=enemy.Health;game.Player.Hands.Tick(false,false,.01f);Assert.AreEqual(after,enemy.Health);Assert.IsFalse(game.Player.Hands.HasPendingMelee(0));
        yield return new WaitForSeconds(.6f);game.Player.Hands.Tick(true,false,.01f);Assert.IsTrue(game.Player.Hands.HasPendingMelee(0));
        game.Player.Damage(10000);game.Player.Hands.Tick(false,false,.01f);Assert.IsFalse(game.Player.Hands.HasPendingMelee(0));Assert.AreEqual(after,enemy.Health);
    }
    [UnityTest] public IEnumerator DisablingPresentationDoesNotDisableAuthoritativeAttacks()
    {
        Loadout("sword",null);game.Player.Weapons.enabled=false;
        var enemy=game.SpawnDrone(game.Player.transform.position+game.Player.transform.forward*2,false);enemy.enabled=false;Physics.SyncTransforms();
        float health=enemy.Health;game.Player.Hands.Tick(true,false,.01f);yield return new WaitForSeconds(.18f);game.Player.Hands.Tick(false,false,.01f);
        Assert.Less(enemy.Health,health);
    }
    [UnityTest] public IEnumerator ReloadAnimatesOneHandWithoutPreventingTheOtherGunFromFiring()
    {
        Loadout("pistol","pistol");yield return null;
        for(int n=0;n<12;n++){game.Player.Hands.Tick(true,false,.01f);yield return new WaitForSeconds(.29f);}
        Assert.AreEqual(12,game.Player.Hands.MainShots);Quaternion ready=game.Player.Weapons.Weapon(0).rotation;
        game.Player.Hands.Tick(true,true,.01f);Assert.IsTrue(game.Player.Hands.IsReloading(0));Assert.AreEqual(1,game.Player.Hands.OffShots);
        yield return new WaitForSeconds(.65f);Assert.Greater(Quaternion.Angle(ready,game.Player.Weapons.Weapon(0).rotation),5);Capture("handgun-reload");
        Assert.IsTrue(game.Player.Hands.IsReloading(0));yield return new WaitForSeconds(.95f);game.Player.Hands.Tick(false,false,.01f);
        Assert.IsFalse(game.Player.Hands.IsReloading(0));Assert.That(game.Player.Hands.State(0),Does.StartWith("12 /"));
    }
    [UnityTest] public IEnumerator MixedAndDualMeleeActionsKeepIndependentHandPhases()
    {
        Loadout("pistol","sword");yield return null;var rig=game.Player.Weapons;Quaternion before=rig.Weapon(1).rotation;
        game.Player.Hands.Tick(false,true,.01f);yield return new WaitForSeconds(.07f);
        Assert.Greater(Quaternion.Angle(before,rig.Weapon(1).rotation),5);game.Player.Hands.Tick(true,false,.01f);yield return null;Capture("mixed-attack");
        Assert.AreEqual(1,game.Player.Hands.MainShots);Assert.AreEqual(1,game.Player.Hands.OffShots);Assert.Less(rig.SwingAge(1),PlayerWeaponAnimation.SwingDuration);
        Loadout("sword","sword");yield return null;game.Player.Hands.Tick(true,true,.01f);
        Assert.IsTrue(game.Player.Hands.HasPendingMelee(0));Assert.IsTrue(game.Player.Hands.HasPendingMelee(1));yield return new WaitForSeconds(.18f);
        game.Player.Hands.Tick(false,false,.01f);yield return null;Capture("dual-sword-contact");
        Assert.IsFalse(game.Player.Hands.HasPendingMelee(0));Assert.IsFalse(game.Player.Hands.HasPendingMelee(1));
        Assert.AreEqual(1,game.Player.Hands.MainShots);Assert.AreEqual(1,game.Player.Hands.OffShots);
        if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)yield return CaptureDemo();
    }
    IEnumerator CaptureDemo()
    {
        // Feature-demo capture only: isolated profile, explicit loadout changes,
        // disabled AI, fixed 30 Hz capture clock, zoomed real gameplay camera.
        string frames="TestResults/weapon-demo";Directory.CreateDirectory(frames);
        int previous=Time.captureFramerate;float size=game.Player.View.orthographicSize;
        try
        {
            Time.captureFramerate=30;game.Player.View.orthographicSize=3;
            var enemy=game.SpawnDrone(game.Player.transform.position+game.Player.transform.forward*2,false);enemy.SetLevel(20);enemy.enabled=false;
            int frame=0;
            foreach(var main in new[]{"sword","pistol"})
            {
                Loadout(main,"shield");yield return null;
                for(int n=0;n<30;n++,frame++)
                {
                    game.Player.Hands.Tick(main=="pistol" || n==2 || n==20,true,1/30f);
                    yield return null;game.CaptureFrame(frames+"/"+frame.ToString("D3")+".png");
                }
            }
        }
        finally{Time.captureFramerate=previous;game.Player.View.orthographicSize=size;}
    }
    [UnityTest] public IEnumerator StationaryArmCorrectionDoesNotAccumulateDriftOrJitter()
    {
        Loadout("pistol","shield");var rig=game.Player.Weapons;
        var hand=game.Player.Motion.GetComponentsInChildren<Transform>().Single(t=>t.name=="hand_R");int baseline=0;
        for(int pass=0;pass<2;pass++)
        {
            rig.enabled=pass==1;for(int n=0;n<4;n++)yield return null;
            Vector3 first=hand.position,a=first,b=first,previousAcceleration=Vector3.zero;int reversals=0;float drift=0;
            for(int n=0;n<24;n++)
            {
                yield return null;Vector3 p=hand.position,acceleration=p-2*b+a;
                if(acceleration.magnitude>.00001f&&previousAcceleration.magnitude>.00001f&&Vector3.Dot(acceleration,previousAcceleration)<0)reversals++;
                previousAcceleration=acceleration;drift=Mathf.Max(drift,Vector3.Distance(first,p));a=b;b=p;
            }
            Assert.Less(drift,.002f,"A frozen source pose must remain stable with either presentation mode.");
            if(pass==0)baseline=reversals;else Assert.LessOrEqual(reversals,baseline+1,"Correction added alternating acceleration jitter.");
        }
    }
}
