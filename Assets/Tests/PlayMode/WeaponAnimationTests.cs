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
                    Assert.Less(Vector3.Distance(game.Player.Weapons.Grip(h),weapon.position),.001f);Assert.Less(Vector3.Distance(hand.position,weapon.position),.16f);Assert.IsFalse(float.IsNaN(weapon.rotation.x));
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
        var enemy=game.SpawnDrone(game.Player.transform.position+game.Player.transform.forward*1.6f,false);enemy.enabled=false;Physics.SyncTransforms();
        float health=enemy.Health;game.Player.Hands.Tick(true,false,.01f);Assert.AreEqual(health,enemy.Health);Assert.IsTrue(game.Player.Hands.HasPendingMelee(0));
        yield return new WaitForSeconds(.07f);Capture("sword-windup");game.Equipment.Open();Quaternion pose=game.Player.Weapons.Weapon(0).rotation;
        yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(health,enemy.Health);Assert.AreEqual(pose,game.Player.Weapons.Weapon(0).rotation);
        game.Menus.Resume();yield return new WaitForSeconds(PlayerWeaponAnimation.SwordContactTime);game.Player.Hands.Tick(false,false,.01f);yield return null;Capture("sword-contact");
        Assert.Less(enemy.Health,health);float after=enemy.Health;game.Player.Hands.Tick(false,false,.01f);Assert.AreEqual(after,enemy.Health);Assert.IsFalse(game.Player.Hands.HasPendingMelee(0));
        yield return new WaitForSeconds(.6f);game.Player.Hands.Tick(true,false,.01f);Assert.IsTrue(game.Player.Hands.HasPendingMelee(0));
        game.Player.Damage(10000);game.Player.Hands.Tick(false,false,.01f);Assert.IsFalse(game.Player.Hands.HasPendingMelee(0));Assert.AreEqual(after,enemy.Health);
    }
    [UnityTest] public IEnumerator DisablingPresentationDoesNotDisableAuthoritativeAttacks()
    {
        Loadout("sword",null);game.Player.Weapons.enabled=false;
        var enemy=game.SpawnDrone(game.Player.transform.position+game.Player.transform.forward*1.6f,false);enemy.enabled=false;Physics.SyncTransforms();
        float health=enemy.Health;game.Player.Hands.Tick(true,false,.01f);yield return new WaitForSeconds(PlayerWeaponAnimation.SwordContactTime+.03f);game.Player.Hands.Tick(false,false,.01f);
        Assert.Less(enemy.Health,health);
    }
    [UnityTest] public IEnumerator AcceptedSwordTurnsTheLiveTorsoAndShiftsWeight()
    {
        Loadout("sword","shield");yield return null;yield return null;
        var bones=game.Player.Motion.GetComponentsInChildren<Transform>();var chest=bones.Single(t=>t.name=="chest");var hips=bones.Single(t=>t.name=="hips");
        Quaternion ready=chest.rotation;Vector3 pelvis=hips.position;
        game.Player.Hands.Tick(true,false,.01f);yield return new WaitForSeconds(PlayerWeaponAnimation.SwordContactTime);yield return null;
        Assert.Greater(Quaternion.Angle(ready,chest.rotation),25,"The rendered runtime must preserve the authored body turn.");Assert.Greater(Vector3.Distance(pelvis,hips.position),.03f);
    }
    [UnityTest] public IEnumerator SwordDoesNotDamageEnemiesBeyondVisibleReach()
    {
        Loadout("sword",null);var enemy=game.SpawnDrone(game.Player.transform.position+game.Player.transform.forward*2.6f,false);enemy.enabled=false;
        float health=enemy.Health;game.Player.Hands.Tick(true,false,.01f);yield return new WaitForSeconds(PlayerWeaponAnimation.SwordContactTime+.03f);game.Player.Hands.Tick(false,false,.01f);
        Assert.AreEqual(health,enemy.Health,"The old broad 2.8m query granted phantom sword hits.");
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
        Assert.IsTrue(game.Player.Hands.HasPendingMelee(0));Assert.IsTrue(game.Player.Hands.HasPendingMelee(1));yield return new WaitForSeconds(PlayerWeaponAnimation.SwordContactTime+.03f);
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
        var capture=game.gameObject.AddComponent<CombatReviewFrame>();
        var pad=InputSystem.AddDevice<Gamepad>();
        int previous=Time.captureFramerate;float size=game.Player.View.orthographicSize;
        try
        {
            Time.captureFramerate=30;game.Player.View.orthographicSize=3;
            game.Menus.Settings.AimAssist=false;
            // Stage a real controller aim before freezing player movement. Mouse
            // rays in this room hit foreground scenery, which is misleading in a
            // close-up intended to inspect the weapon animation itself.
            InputSystem.QueueStateEvent(pad,new GamepadState{rightStick=Vector2.down});game.Player.enabled=true;yield return null;game.Player.enabled=false;
            var enemy=game.SpawnDrone(game.Player.transform.position+game.Player.transform.forward*1.6f,false);enemy.SetLevel(20);enemy.enabled=false;
            int frame=0;
            foreach(var main in new[]{"sword","pistol","rifle","dual"})
            {
                Loadout(main=="dual"?"sword":main,main=="rifle"?null:main=="dual"?"sword":"shield");yield return null;
                Vector3 aim=game.Player.GunAimPoint(game.Player.transform.position+Vector3.up*.25f)-game.Player.transform.position;aim.y=0;
                Assert.Greater(Vector3.Dot(aim.normalized,game.Player.transform.forward),.98f);
                for(int n=0;n<30;n++,frame++)
                {
                    game.Player.Hands.Tick(main=="pistol" || main=="rifle" || n==2,main!="dual"||n==2,1/30f);
                    capture.Pending=()=>CaptureReviewFrame(frame);
                    yield return null;
                }
            }
        }
        finally{Time.captureFramerate=previous;game.Player.View.orthographicSize=size;Object.Destroy(capture);InputSystem.RemoveDevice(pad);}
    }
    void CaptureReviewFrame(int frame)
    {
        string frames="TestResults/weapon-demo";
            game.CaptureFrame(frames+"/"+frame.ToString("D3")+".png");
                    var cameraObject=new GameObject("Combat review front camera");var camera=cameraObject.AddComponent<Camera>();camera.CopyFrom(game.Player.View);camera.enabled=false;camera.orthographicSize=1.6f;camera.cullingMask=1<<29;camera.backgroundColor=new Color(.035f,.05f,.07f);
                    var rendered=game.Player.GetComponentsInChildren<Renderer>().Concat(game.Player.Weapons.Weapon(0)?.GetComponentsInChildren<Renderer>()??new Renderer[0]).Concat(game.Player.Weapons.Weapon(1)?.GetComponentsInChildren<Renderer>()??new Renderer[0]).Select(r=>r.gameObject).Distinct().ToArray();var layers=rendered.Select(o=>o.layer).ToArray();for(int i=0;i<rendered.Length;i++)rendered[i].layer=29;
                    camera.transform.position=game.Player.transform.position+game.Player.transform.forward*4+Vector3.up*.8f;camera.transform.LookAt(game.Player.transform.position);
                    var target=new RenderTexture(640,640,24);camera.targetTexture=target;camera.Render();var old=RenderTexture.active;RenderTexture.active=target;
                    var image=new Texture2D(640,640,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,640,640),0,0);image.Apply();Directory.CreateDirectory("TestResults/combat-front");File.WriteAllBytes("TestResults/combat-front/"+frame.ToString("D3")+".png",image.EncodeToPNG());
                    for(int i=0;i<rendered.Length;i++)rendered[i].layer=layers[i];
                    RenderTexture.active=old;camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(image);Object.Destroy(cameraObject);
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

// Test-only capture runs after the actual pose and effects updates. It does not
// step animation or manufacture poses, and does not claim a real-time FPS rate.
[DefaultExecutionOrder(1000)]
public sealed class CombatReviewFrame : MonoBehaviour
{
    public System.Action Pending;
    void LateUpdate(){var action=Pending;Pending=null;action?.Invoke();}
}
