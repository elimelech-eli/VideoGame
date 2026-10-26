using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using VideoGame;

public sealed class InputTests
{
    Keyboard keyboard;
    Gamepad pad;
    Mouse mouse;
    Prototype game;
    InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
    InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif
    [UnitySetUp] public IEnumerator Setup()
    {
        var existing = Object.FindFirstObjectByType<Prototype>();
        if (existing != null) Object.Destroy(existing.gameObject);
        yield return null;
        previousBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        keyboard = InputSystem.AddDevice<Keyboard>();
        pad = InputSystem.AddDevice<Gamepad>();
        mouse = InputSystem.AddDevice<Mouse>();
        game = new GameObject("Input Test").AddComponent<Prototype>();
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        if (pad != null) InputSystem.RemoveDevice(pad);
        if (mouse != null) InputSystem.RemoveDevice(mouse);
        InputSystem.settings.backgroundBehavior = previousBackground;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
#endif
        if (game != null) Object.Destroy(game.gameObject);
        yield return null;
    }
    [UnityTest] public IEnumerator KeyboardAndControllerMoveAndDash()
    {
        Vector3 start = game.Player.transform.position;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
        yield return new WaitForSeconds(.2f);
        Assert.IsTrue(keyboard.dKey.isPressed, "Virtual keyboard state was reset");
        Assert.AreSame(keyboard, Keyboard.current, "Native keyboard replaced test device");
        Assert.IsTrue(Physics.Raycast(game.Player.transform.position + Vector3.up * 2, Vector3.down, 5, 1 << 8), "Player has no floor below it");
        Assert.Greater(game.Player.transform.position.x, start.x + .5f, "Start=" + start + " end=" + game.Player.transform.position + " overlaps=" + string.Join(",", System.Array.ConvertAll(Physics.OverlapSphere(game.Player.transform.position, 2), c => c.name)));
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        Vector3 beforeController = game.Player.transform.position;
        InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = Vector2.up });
        yield return new WaitForSeconds(.2f);
        Assert.Greater(game.Player.transform.position.z, beforeController.z + .5f);
        Vector3 beforeDash = game.Player.transform.position;
        InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = Vector2.up }.WithButton(GamepadButton.East));
        yield return new WaitForSeconds(.16f);
        Assert.Greater(game.Player.transform.position.z, beforeDash.z + 1.8f);
    }
    [UnityTest] public IEnumerator MouseClickMovesAndMouseAimFires()
    {
        Vector3 start = game.Player.transform.position;
        Vector2 point = game.Player.View.WorldToScreenPoint(new Vector3(3, 0, 3));
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 2 });
        yield return new WaitForSeconds(.2f);
        Assert.Greater(Vector3.Distance(start, game.Player.transform.position), .5f, "Right click did not move");
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        yield return null;
        game.StartMission();
        yield return new WaitForSeconds(.1f);
        var enemies = Object.FindObjectsByType<Drone>(FindObjectsSortMode.None);
        foreach (var enemy in enemies) enemy.enabled = false;
        enemies[0].transform.position = new Vector3(4, 1, 4);
        point = game.Player.View.WorldToScreenPoint(new Vector3(4, 0, 4));
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 1 });
        yield return WaitForKills(1);
        Assert.IsTrue(mouse.leftButton.isPressed, "Mouse button state reset");
        Assert.AreSame(mouse, Mouse.current, "Native mouse replaced test mouse");
        Assert.GreaterOrEqual(game.Mission.Kills, 1, "Mouse aimed shot missed; player=" + game.Player.transform.position + " forward=" + game.Player.transform.forward + " mouse=" + mouse.position.ReadValue() + " target=" + point);
    }
    [UnityTest] public IEnumerator ControllerGunAndGrenadeKillActualEnemies()
    {
        game.StartMission();
        yield return null;
        var enemies = Object.FindObjectsByType<Drone>(FindObjectsSortMode.None);
        foreach (var enemy in enemies) enemy.enabled = false;
        enemies[0].transform.position = new Vector3(5, 1, 5);
        InputSystem.QueueStateEvent(pad, new GamepadState { rightStick = Vector2.up, rightTrigger = 1 });
        yield return WaitForKills(1);
        Assert.GreaterOrEqual(game.Mission.Kills, 1, "Gun did not hit the aimed-at drone");
        InputSystem.QueueStateEvent(pad, new GamepadState { rightStick = Vector2.up }.WithButton(GamepadButton.RightShoulder));
        enemies[1].transform.position = new Vector3(5, 1, 5);
        yield return WaitForKills(2);
        Assert.GreaterOrEqual(game.Mission.Kills, 2, "Grenade did not damage nearby drone");
        Assert.Greater(game.Player.GrenadeRemaining, 0);
    }
    IEnumerator WaitForKills(int expected)
    {
        // CI uses a software renderer: elapsed time alone doesn't guarantee
        // enough Update calls for the weapon's three separate shots.
        float deadline = Time.realtimeSinceStartup + 5;
        while (game.Mission.Kills < expected && Time.realtimeSinceStartup < deadline) yield return null;
    }
}
