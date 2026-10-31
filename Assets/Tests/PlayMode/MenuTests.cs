using System;
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

public sealed class MenuTests
{
    Prototype game;
    string directory;
    Keyboard keyboard;Gamepad pad;Mouse mouse;
    InputSettings.BackgroundBehavior background;
#if UNITY_EDITOR
    InputSettings.EditorInputBehaviorInPlayMode editorInput;
#endif
    [UnitySetUp] public IEnumerator Setup()
    {
        var old=Object.FindFirstObjectByType<Prototype>();if(old!=null)Object.Destroy(old.gameObject);yield return null;
        background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        keyboard=InputSystem.AddDevice<Keyboard>();pad=InputSystem.AddDevice<Gamepad>();mouse=InputSystem.AddDevice<Mouse>();
        directory=Path.Combine(Path.GetTempPath(),"VideoGameMenuTests-"+Guid.NewGuid());
        game=new GameObject("Menu test").AddComponent<Prototype>();game.Menus.UseStorage(directory);yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(pad!=null)InputSystem.RemoveDevice(pad);if(mouse!=null)InputSystem.RemoveDevice(mouse);
        InputSystem.settings.backgroundBehavior=background;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
#endif
        if(game!=null)Object.Destroy(game.gameObject);yield return null;
        string full=Path.GetFullPath(directory);
        if(full.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase) && Directory.Exists(full))Directory.Delete(full,true);
    }
    [UnityTest] public IEnumerator GalaxyConsoleHasSolidBaseClearApproachAndSurvivesHubRebuild()
    {
        for(int visit=0;visit<2;visit++)
        {
            if(visit>0){game.BuildHub();yield return null;}
            var console=game.GetComponentsInChildren<Transform>().Single(t=>t.name=="GalaxyConsole(Clone)");
            var blocker=console.GetComponent<BoxCollider>();Assert.IsNotNull(blocker);
            Physics.SyncTransforms();
            Assert.IsTrue(Physics.CheckSphere(blocker.bounds.center,.1f,1<<9),"Pedestal must block walking through its base");
            Assert.IsFalse(Physics.CheckCapsule(game.Terminal+Vector3.back*2-Vector3.up*.5f,game.Terminal+Vector3.back*2+Vector3.up*.5f,.4f,1<<9),"Mission interaction approach must stay clear");
            var animation=console.GetComponentInChildren<Animation>();Assert.IsTrue(animation.isPlaying);
            var before=animation.transform.rotation;yield return new WaitForSeconds(.1f);
            Assert.Greater(Quaternion.Angle(before,animation.transform.rotation),.1f,"Galaxy rotation must play in the live hub");
            Assert.Less(blocker.bounds.max.y,animation.transform.position.y,"Projection must float above physical console");
            game.Player.Teleport(game.Terminal+Vector3.back*2);game.Interact();
            Assert.AreEqual(MenuPage.Deployment,game.Menus.Page);
            game.Menus.Back();
            Assert.AreEqual(MenuPage.Closed,game.Menus.Page);
        }
    }
    [UnityTest] public IEnumerator CargoServicesHaveClearApproachesAndKeyboardOpensStash()
    {
        var services=game.GetComponentsInChildren<ShipService>();Assert.AreEqual(2,services.Length);
        Physics.SyncTransforms();
        foreach(var service in services)
        {
            var p=service.transform.TransformPoint(service.Approach);
            Assert.IsTrue(Physics.Raycast(p,Vector3.down,2,1<<8));
            Assert.IsFalse(Physics.CheckCapsule(p-Vector3.up*.5f,p+Vector3.up*.5f,.4f,1<<9),"Blocked service approach: "+service.Kind);
        }
        var stash=services.Single(s=>s.Kind==ShipServiceKind.Stash);
        Assert.IsFalse(stash.Open(game),"Cannot use stash remotely");
        game.Player.Teleport(stash.transform.TransformPoint(stash.Approach));
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));yield return null;yield return null;
        Assert.AreEqual(MenuPage.Equipment,game.Menus.Page);Assert.AreEqual(CharacterStorage.Stash,game.GetComponent<CharacterScreen>().Storage);
        Assert.AreEqual(0,Time.timeScale);Assert.IsFalse(game.InMission);
    }
    [UnityTest] public IEnumerator CargoSellingRequiresConfirmationAndPaysOnlyOnce()
    {
        var sell=game.GetComponentsInChildren<ShipService>().Single(s=>s.Kind==ShipServiceKind.Sell);
        game.Player.Teleport(sell.transform.TransformPoint(sell.Approach));
        int credits=game.Equipment.Wallet;
        InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
        var screen=game.GetComponent<CharacterScreen>();Assert.IsTrue(screen.Selling);Assert.AreEqual(MenuPage.Equipment,game.Menus.Page);
        Assert.AreEqual(credits,game.Equipment.Wallet,"Opening sell console must not transact");
        InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
        var item=game.Equipment.Profile.Bag.First();Assert.IsTrue(screen.SelectItem(item.Id));
        Assert.IsTrue(screen.ConfirmSellSelected());Assert.AreEqual(MenuPage.Confirm,game.Menus.Page);
        game.Menus.Back();Assert.AreEqual(credits,game.Equipment.Wallet);Assert.IsTrue(game.Equipment.Profile.Bag.Contains(item));
        Assert.IsTrue(screen.ConfirmSellSelected());game.Menus.Activate("confirm");
        Assert.AreEqual(credits+item.Sale,game.Equipment.Wallet);Assert.IsFalse(game.Equipment.Profile.Bag.Contains(item));
        Assert.IsFalse(screen.ConfirmSellSelected());Assert.AreEqual(credits+item.Sale,game.Equipment.Wallet);
        game.Menus.Resume();yield return null;yield return null;game.StartMission();
        Assert.IsTrue(game.InMission);Assert.IsNull(game.NearbyShipService);Assert.IsFalse(sell.Open(game));
    }
    [UnityTest] public IEnumerator CargoStashTransfersPersistAcrossCheckpointLoad()
    {
        var stash=game.GetComponentsInChildren<ShipService>().Single(s=>s.Kind==ShipServiceKind.Stash);
        game.Player.Teleport(stash.transform.TransformPoint(stash.Approach));Assert.IsTrue(stash.Open(game));
        var item=game.Equipment.Profile.Bag.First();string id=item.Id;
        Assert.IsTrue(game.Equipment.Profile.Transfer(id,true,false));game.Equipment.Changed();
        Assert.IsTrue(game.Menus.Save(1));Assert.IsTrue(game.Menus.Load(1));yield return null;
        Assert.IsTrue(game.Equipment.Profile.Stash.Any(i=>i.Id==id));Assert.IsFalse(game.Equipment.Profile.Bag.Any(i=>i.Id==id));
        Assert.IsTrue(game.Equipment.Profile.Transfer(id,false,false));game.Equipment.Changed();
        Assert.IsTrue(game.Equipment.Profile.Bag.Any(i=>i.Id==id));
    }
    [UnityTest] public IEnumerator StarshipRoomsHaveContinuousDeckClearDoorwaysAndSealedAftHull()
    {
        var doors=game.GetComponentsInChildren<ShipDoor>();
        foreach(var door in doors){door.enabled=false;door.Blocker.enabled=false;}
        Physics.SyncTransforms();
        var routes=new[]{new Vector3(0,0,-12.5f),new Vector3(0,0,7.1f),new Vector3(-4,0,0),new Vector3(4,0,0)};
        foreach(var end in routes)for(int step=0;step<=40;step++)
        {
            var p=Vector3.Lerp(Vector3.zero,end,step/40f);
            Assert.IsTrue(Physics.Raycast(p+Vector3.up*2,Vector3.down,3,1<<8),"Deck missing: "+p);
            Assert.IsFalse(Physics.CheckCapsule(p+Vector3.up*.5f,p+Vector3.up*1.5f,.4f,1<<9),"Route obstructed: "+p);
        }
        Assert.IsTrue(Physics.Raycast(new Vector3(0,1,-12.5f),Vector3.back,3,1<<9),"Sealed aft expansion must block exit");
        foreach(var door in doors)door.enabled=true;
        game.Player.Teleport(new Vector3(0,1,4));
        yield return new WaitForSeconds(.6f);
        var controller=game.Player.GetComponent<CharacterController>();
        for(int i=0;i<60;i++){controller.Move(new Vector3(0,-.02f,.05f));yield return null;}
        Assert.Greater(game.Player.transform.position.z,6.8f,"Player must physically cross bridge doorway");
        yield return null;
    }
    [UnityTest] public IEnumerator ShipDoorsOpenFromBothSidesRemainSafeAndCloseAfterDeparture()
    {
        var door=game.GetComponentsInChildren<ShipDoor>().Single(d=>Mathf.Abs(d.transform.position.z-5)<.1f);
        game.Player.Teleport(new Vector3(0,1,3));yield return new WaitForSeconds(.65f);
        Assert.Greater(door.Openness,.99f);Assert.IsFalse(door.Blocker.enabled);
        game.Player.Teleport(new Vector3(0,1,5));yield return new WaitForSeconds(1.5f);
        Assert.IsFalse(door.Blocker.enabled,"Standing in the doorway must hold it open");
        game.Player.Teleport(new Vector3(0,1,-3));yield return new WaitForSeconds(1.9f);
        Assert.Less(door.Openness,.01f);Assert.IsTrue(door.Blocker.enabled);
        game.Player.Teleport(new Vector3(0,1,7));yield return new WaitForSeconds(.65f);
        Assert.Greater(door.Openness,.99f,"Door must open from bridge side too");
    }
    [UnityTest] public IEnumerator OccupiedRoomsRevealTheirShellAndOtherRoomsStayClosed()
    {
        var points=new[]{new Vector3(0,1,11),new Vector3(-5,1,-4),new Vector3(5,1,-1),new Vector3(0,1,-10),new Vector3(0,1,2)};
        var shells=game.GetComponentsInChildren<ShipRoomShell>();Assert.AreEqual(5,shells.Length);
        for(int room=0;room<points.Length;room++)
        {
            game.Player.Teleport(points[room]);yield return new WaitForSeconds(.5f);
            Assert.AreEqual(room,game.GetComponentInChildren<StarshipLife>().OccupiedRoom);
            foreach(var shell in shells)Assert.AreEqual(shell.RoomId!=room,shell.GetComponent<Renderer>().enabled,"Room shell visibility: "+shell.RoomId);
        }
        Assert.AreEqual(2,game.GetComponentsInChildren<ShipCrewIdle>().Length);
    }
    [UnityTest] public IEnumerator TerminalListsEveryBiomeAndLaunchesSelectedDestinationWithRandomSize()
    {
        game.Menus.Settings.Biome=0;game.Menus.Settings.SectorSize=6;
        var sizes=new System.Collections.Generic.HashSet<int>();
        foreach(BiomeId biome in Enum.GetValues(typeof(BiomeId)))
        {
            game.Player.Teleport(game.Terminal+Vector3.back*2);
            game.Interact();Assert.AreEqual(MenuPage.Deployment,game.Menus.Page);Assert.IsFalse(game.InMission);
            Assert.AreEqual(7,game.Menus.Items().Count);Assert.AreEqual(0,Time.timeScale);
            foreach(BiomeId destination in Enum.GetValues(typeof(BiomeId)))
                Assert.IsTrue(game.Menus.Items().Any(i=>i.Id=="mission-"+(int)destination));
            int seed=game.Seed;game.Menus.Activate("mission-"+(int)biome);
            Assert.IsFalse(game.InMission,"Launch must wait for input handoff");
            yield return null;yield return null;yield return null;
            Assert.IsTrue(game.InMission);Assert.AreEqual(biome,game.ActiveBiome);Assert.AreEqual(seed+1,game.Seed);
            Assert.AreEqual(new Deployment(seed+1,(int)biome,0).Rooms,game.Layout.Count);
            sizes.Add(game.Layout.Count);game.Director.enabled=false;
            foreach(var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))enemy.enabled=false;
            game.Menus.OpenDeployment();Assert.AreEqual(MenuPage.Closed,game.Menus.Page);
            game.Mission.Abandon();game.BuildHub();yield return null;
        }
        Assert.Greater(sizes.Count,1,"Saved fixed size leaked into terminal missions");
        Assert.AreEqual(6,game.Menus.Settings.SectorSize,"One-off missions must not rewrite saved/debug preferences");
    }
    [UnityTest] public IEnumerator TerminalCancelAndRangeGuardKeepNexusAndSeedUnchanged()
    {
        int seed=game.Seed;game.Menus.OpenDeployment();Assert.AreEqual(MenuPage.Closed,game.Menus.Page);
        game.Player.Teleport(game.Terminal+Vector3.back*2);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));yield return null;yield return null;
        Assert.AreEqual(MenuPage.Deployment,game.Menus.Page);Assert.IsFalse(game.InMission);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;yield return null;
        Assert.AreEqual(MenuPage.Closed,game.Menus.Page);Assert.IsFalse(game.InMission);Assert.AreEqual(seed,game.Seed);
        Assert.AreEqual(1,Time.timeScale);Assert.AreEqual(MissionStage.Available,game.Mission.Stage);
    }
    [UnityTest] public IEnumerator ControllerChoosesBiomeAndHeldConfirmDoesNotLaunchOrFireEarly()
    {
        game.Player.Teleport(game.Terminal+Vector3.back*2);
        InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
        Assert.AreEqual(MenuPage.Deployment,game.Menus.Page);Assert.IsFalse(game.InMission);
        InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
        InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.DpadDown));yield return null;
        Assert.AreEqual(1,game.Menus.Selected);
        InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
        InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
        Assert.IsFalse(game.InMission,"Held confirm must not deploy until released");
        InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;yield return null;yield return null;
        Assert.IsTrue(game.InMission);Assert.AreEqual(BiomeId.RuinedFactory,game.ActiveBiome);
        game.Director.enabled=false;Assert.AreEqual(0,game.Player.DashRemaining);
    }
    [UnityTest] public IEnumerator MouseSelectsTerminalDestination()
    {
        game.Player.Teleport(game.Terminal+Vector3.back*2);game.Interact();
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
        Vector2 point=new Vector2((Screen.width-1280*scale)/2+200*scale,(Screen.height-720*scale)/2+(720-(204+3*47+16))*scale);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});yield return null;yield return null;
        Assert.IsFalse(game.InMission);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;yield return null;yield return null;
        Assert.IsTrue(game.InMission);Assert.AreEqual(BiomeId.BiotechLab,game.ActiveBiome);game.Director.enabled=false;
    }
    [UnityTest] public IEnumerator MainMenuRequiresNewGameAndControllerConfirmCannotTriggerGameplay()
    {
        game.Menus.OpenMain(false);
        Assert.AreEqual(0,Time.timeScale);Assert.IsFalse(game.Menus.Items().Single(i=>i.Id=="continue").Enabled);
        InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
        Assert.IsTrue(game.Menus.HasSession);Assert.AreEqual(MenuPage.Closed,game.Menus.Page);
        Assert.AreEqual(MissionStage.Available,game.Mission.Stage,"Confirm must not accept a terminal mission");
        Assert.IsTrue(game.Menus.BlocksGameplay,"Held A must not leak into gameplay");
        InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;yield return null;
        Assert.IsFalse(game.Menus.BlocksGameplay);Assert.IsNotNull(game.Menus.Store.Read(0).Data);
    }
    [UnityTest] public IEnumerator MouseCanOpenOptionsAndAdjustVolume()
    {
        game.Menus.OpenMain(false);
        Vector2 Point(float x,float y)
        {
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            return new Vector2((Screen.width-1280*scale)/2+x*scale,(Screen.height-720*scale)/2+(720-y)*scale);
        }
        InputSystem.QueueStateEvent(mouse,new MouseState{position=Point(160,380),buttons=1});yield return null;yield return null;
        Assert.AreEqual(MenuPage.Options,game.Menus.Page);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=Point(160,225)});yield return null;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=Point(160,225),buttons=1});yield return null;yield return null;
        Assert.AreEqual(MenuPage.Audio,game.Menus.Page);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=Point(688,225)});yield return null;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=Point(688,225),buttons=1});yield return null;yield return null;
        Assert.That(game.Menus.Draft.Master,Is.EqualTo(.9f).Within(.001f));
    }
    [UnityTest] public IEnumerator HiddenTerminalPromptDoesNotInterceptGameplayInput()
    {
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
        Vector2 point=new Vector2((Screen.width-1280*scale)/2+600*scale,(Screen.height-720*scale)/2+170*scale);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});yield return null;yield return null;
        Assert.Greater(Vector3.Distance(game.Player.transform.position,game.Terminal),3.5f);
        Assert.IsFalse(game.Menus.BlocksPlayerInput,"A hidden terminal button must not create an input dead zone");
        game.Player.Teleport(game.Terminal+Vector3.back*2);yield return null;
        Assert.IsTrue(game.Menus.BlocksPlayerInput,"The visible terminal button should intercept clicks");
        game.Player.Teleport(Vector3.up);yield return null;
        Assert.IsFalse(game.Menus.BlocksPlayerInput);
    }
    [UnityTest] public IEnumerator IdleMouseOverToolbarDoesNotBlockControllerMovement()
    {
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
        Vector2 point=new Vector2((Screen.width-1280*scale)/2+280*scale,(Screen.height-720*scale)/2+620*scale);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point});
        InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=Vector2.up});
        Vector3 start=game.Player.transform.position;yield return new WaitForSeconds(.2f);
        Assert.IsFalse(game.Menus.BlocksPlayerInput,"An idle cursor over UI must not disable the controller");
        Assert.Greater(Vector3.Distance(start,game.Player.transform.position),.5f);
    }
    [UnityTest] public IEnumerator EscapeAndStartPauseAndResumeWithoutDashOrInteraction()
    {
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;yield return null;
        Assert.AreEqual(MenuPage.Pause,game.Menus.Page);Assert.AreEqual(0,Time.timeScale);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
        InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.Start));yield return null;yield return null;
        Assert.AreEqual(MenuPage.Closed,game.Menus.Page);Assert.AreEqual(1,Time.timeScale);
        InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
        Assert.AreEqual(0,game.Player.DashRemaining);
    }
    [UnityTest] public IEnumerator PauseFreezesEnemiesProjectilesHazardsAndReinforcements()
    {
        game.StartMission();game.Director.enabled=false;
        foreach(var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))enemy.enabled=false;
        game.Player.Teleport(new Vector3(0,1,1.8f));
        var wall=game.SpawnDestructible(Vector3.zero,true);wall.Damage(40,new Vector3(0,1,-2),Vector3.forward);
        var bolt=new GameObject("Paused bolt");bolt.transform.SetParent(game.transform);bolt.transform.position=new Vector3(0,1,-2);bolt.AddComponent<EnemyBolt>().Direction=Vector3.forward;
        yield return new WaitForSeconds(.12f);
        game.Menus.Pause();Vector3 position=bolt.transform.position;Quaternion rotation=wall.transform.rotation;float health=game.Player.Health;
        yield return new WaitForSecondsRealtime(.9f);
        Assert.AreEqual(position,bolt.transform.position);Assert.Less(Quaternion.Angle(rotation,wall.transform.rotation),.01f);
        Assert.AreEqual(health,game.Player.Health);Assert.AreEqual(0,game.Director.SpawnWave());
        Assert.IsFalse(game.Menus.CanSave);Assert.IsTrue(game.Menus.SaveHint.Contains("Nexus"));Assert.IsFalse(game.Menus.Save(1));
        bolt.SetActive(false);Object.Destroy(bolt);
        game.Menus.Resume();yield return new WaitForSeconds(.8f);
        Assert.AreEqual(health-35,game.Player.Health,"Pausing during a fall must not consume its crushing hit");
    }
    [UnityTest] public IEnumerator ManualCheckpointRestoresSeedDifficultyAndPendingRewardWithoutDuplicatingIt()
    {
        game.StartMission();game.Director.enabled=false;
        foreach(var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))enemy.Damage(100);
        game.Interact();Assert.IsFalse(game.InMission);Assert.AreEqual(MissionStage.Complete,game.Mission.Stage);
        game.Menus.SetDifficulty(Difficulty.Hard);int seed=game.Seed;Assert.IsTrue(game.Menus.Save(1));
        game.Interact();Assert.AreEqual(100,game.Mission.Credits);
        Assert.IsTrue(game.Menus.Load(1));yield return null;yield return null;
        Assert.IsFalse(game.InMission);Assert.AreEqual(seed,game.Seed);Assert.AreEqual(Difficulty.Hard,game.Menus.Difficulty);
        Assert.AreEqual(MissionStage.Complete,game.Mission.Stage);Assert.AreEqual(0,game.Mission.Credits);
        game.Interact();Assert.AreEqual(100,game.Mission.Credits);
        Assert.IsFalse(game.Mission.ClaimReward());Assert.IsTrue(game.Menus.Save(2));
        Assert.IsTrue(game.Menus.Load(2));Assert.IsFalse(game.Mission.ClaimReward());Assert.AreEqual(100,game.Mission.Credits);
    }
    [UnityTest] public IEnumerator InvalidLoadKeepsCurrentSessionAndReturnToMainRequiresConfirmation()
    {
        game.StartMission();int seed=game.Seed;game.Menus.Pause();
        Assert.IsFalse(game.Menus.Load(3));Assert.IsTrue(game.InMission);Assert.AreEqual(seed,game.Seed);
        game.Menus.Activate("main");Assert.AreEqual(MenuPage.Confirm,game.Menus.Page);
        game.Menus.Activate("cancel");Assert.AreEqual(MenuPage.Pause,game.Menus.Page);Assert.IsTrue(game.InMission);
        game.Menus.Activate("main");game.Menus.Activate("confirm");
        Assert.AreEqual(MenuPage.Main,game.Menus.Page);Assert.IsFalse(game.Menus.HasSession);Assert.IsFalse(game.InMission);
        Assert.AreEqual(0,Time.timeScale);yield return null;
    }
    [UnityTest] public IEnumerator OptionsPersistAndDisplayPreviewCanBeRevertedWhilePaused()
    {
        game.Menus.Pause();game.Menus.Activate("options");game.Menus.Activate("audio");
        game.Menus.Activate("master"); // cycle at upper bound should remain valid
        game.Menus.Draft.Master=.4f;game.Menus.Draft.Music=.3f;game.Menus.Draft.Effects=.2f;
        game.Menus.Activate("mute");
        game.Menus.Back();game.Menus.Activate("apply");
        Assert.AreEqual(.4f,game.Menus.Store.LoadSettings().Master);Assert.AreEqual(0,Time.timeScale);
        Assert.IsFalse(game.Music.Enabled);Assert.IsTrue(game.Menus.Store.LoadSettings().MusicMuted);
        game.Menus.Activate("display");game.Menus.Draft.WindowMode=1;game.Menus.Back();game.Menus.Activate("apply");
        Assert.AreEqual(MenuPage.KeepDisplay,game.Menus.Page);game.Menus.Back();
        Assert.AreEqual(0,game.Menus.Settings.WindowMode);Assert.AreEqual(0,game.Menus.Store.LoadSettings().WindowMode);
        game.Menus.Activate("controls");game.Menus.Activate("assist");game.Menus.Back();game.Menus.Activate("apply");
        Assert.IsFalse(game.Menus.Settings.AimAssist);Assert.IsFalse(game.Menus.Store.LoadSettings().AimAssist);
        yield return new WaitForSecondsRealtime(1.6f);
        Assert.That(game.Music.Nexus.volume,Is.EqualTo(.22f*.4f*.3f).Within(.005f));
    }
    [UnityTest] public IEnumerator DifficultyChangesDamageImmediatelyAndPersistsAtTheNexus()
    {
        game.Menus.SetDifficulty(Difficulty.Story);game.Player.Damage(20);Assert.AreEqual(90,game.Player.Health);
        game.Menus.SetDifficulty(Difficulty.Nightmare);game.Player.Damage(20);Assert.AreEqual(40,game.Player.Health);
        Assert.IsTrue(game.Menus.Save(1));game.Menus.SetDifficulty(Difficulty.Standard);
        game.Menus.Load(1);Assert.AreEqual(Difficulty.Nightmare,game.Menus.Difficulty);Assert.AreEqual(100,game.Player.Health);
        yield return null;
    }
    [UnityTest] public IEnumerator UnconfirmedDisplayChangesAutomaticallyRevertUsingRealTime()
    {
        game.Menus.Pause();game.Menus.Activate("options");
        game.Menus.Draft.Width=1600;game.Menus.Draft.Height=900;game.Menus.Activate("apply");
        Assert.AreEqual(MenuPage.KeepDisplay,game.Menus.Page);Assert.AreEqual(0,Time.timeScale);
        yield return new WaitForSecondsRealtime(15.2f);
        Assert.AreEqual(MenuPage.Options,game.Menus.Page);Assert.AreEqual(1280,game.Menus.Settings.Width);
        Assert.AreEqual(1280,game.Menus.Store.LoadSettings().Width);
    }
    [UnityTest] public IEnumerator ClickingPauseDoesNotFireAndNewGameKeepsManualSlots()
    {
        Assert.IsTrue(game.Menus.Save(1));
        game.Menus.OpenMain(false);game.Menus.Activate("new");
        Assert.AreEqual(MenuPage.Confirm,game.Menus.Page);game.Menus.Activate("confirm");
        yield return null;yield return null;
        Assert.IsNotNull(game.Menus.Store.Read(1).Data);
        game.StartMission();game.Director.enabled=false;
        foreach(var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))enemy.enabled=false;
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
        Vector2 point=new Vector2((Screen.width-1280*scale)/2+280*scale,(Screen.height-720*scale)/2+615*scale);
        int feedbackBefore=game.Feedback.EventCount;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});yield return null;
        Assert.IsTrue(game.Menus.BlocksPlayerInput);
        Assert.AreEqual(feedbackBefore,game.Feedback.EventCount,"Clicking pause must not generate a shot");
    }
    [UnityTest] public IEnumerator StorageFailureDoesNotTrapTheDisplayConfirmationOrDestroyTheSession()
    {
        Directory.CreateDirectory(directory);string blocked=Path.Combine(directory,"blocked");File.WriteAllText(blocked,"not a directory");
        game.Menus.UseStorage(blocked);game.Menus.Pause();game.Menus.Activate("options");
        game.Menus.Draft.Width=1600;game.Menus.Draft.Height=900;game.Menus.Activate("apply");
        Assert.AreEqual(MenuPage.KeepDisplay,game.Menus.Page);game.Menus.Activate("keep");
        Assert.AreEqual(MenuPage.Options,game.Menus.Page);Assert.IsTrue(game.Menus.Notice.Contains("could not"));
        game.Menus.Back();Assert.AreEqual(MenuPage.Pause,game.Menus.Page);
        Assert.IsFalse(game.Menus.Save(1));Assert.IsFalse(game.Menus.Load(1));
        Assert.IsFalse(game.InMission);Assert.AreEqual(MissionStage.Available,game.Mission.Stage);
        yield return null;
    }
    [UnityTest] public IEnumerator InventoryHotkeyOwnsInputBeforeMovementDashAndFire()
    {
        game.StartMission();yield return null;
        foreach(var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))enemy.enabled=false;
        var before=game.Player.transform.position;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.I,Key.D,Key.Space));
        InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1});
        yield return null;yield return null;
        Assert.AreEqual(MenuPage.Equipment,game.Menus.Page);Assert.AreEqual(0,Time.timeScale);
        Assert.AreEqual(before,game.Player.transform.position);Assert.AreEqual(0,game.Player.Hands.MainShots);Assert.AreEqual(0,game.Player.DashRemaining);
    }
    [UnityTest] public IEnumerator ControllerNavigationAndSubmenuBackPreservePause()
    {
        game.Menus.Pause();InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.DpadDown));yield return null;yield return null;
        Assert.AreEqual("save",game.Menus.Items()[game.Menus.Selected].Id);
        InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
        game.Menus.Activate("options");game.Menus.Activate("audio");
        InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.East));yield return null;yield return null;
        Assert.AreEqual(MenuPage.Options,game.Menus.Page);Assert.AreEqual(0,Time.timeScale);
    }
}
