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
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;yield return null;
        Assert.Greater(Vector3.Distance(game.Player.transform.position,game.Terminal),3.5f);
        Assert.IsFalse(game.Menus.BlocksPlayerInput,"A hidden terminal button must not create an input dead zone");
        game.Player.Teleport(game.Terminal+Vector3.back*2);yield return null;
        Assert.IsTrue(game.Menus.BlocksPlayerInput,"The visible terminal button should intercept clicks");
        game.Player.Teleport(Vector3.up);yield return null;
        Assert.IsFalse(game.Menus.BlocksPlayerInput);
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
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});yield return null;
        Assert.IsTrue(game.Menus.BlocksPlayerInput);
        Assert.AreEqual(0,Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None).Count(line=>line.gameObject.name=="Tracer"));
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
