using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;
using VideoGame.Core;

public sealed class LoopTests
{
    [UnityTest] public IEnumerator MissionCanBeCompletedAndRewardedAtHub()
    {
        var game = Object.FindFirstObjectByType<Prototype>();
        if (game == null) game = new GameObject("Test Game").AddComponent<Prototype>();
        yield return null;
        Capture(game, "hub");
        game.StartMission();
        yield return null;
        Assert.IsTrue(game.InMission);
        var enemies = Object.FindObjectsByType<Drone>(FindObjectsSortMode.None);
        Assert.GreaterOrEqual(enemies.Length, game.Mission.Target);
        game.Player.Teleport(enemies[0].transform.position + new Vector3(0, 0, -4));
        yield return null;
        Capture(game, "field");
        for (int i = 0; i < game.Mission.Target; i++) enemies[i].Damage(enemies[i].HealthMaximum+enemies[i].Shield);
        Assert.AreEqual(MissionStage.Complete, game.Mission.Stage);
        game.Interact();
        Assert.IsFalse(game.InMission);
        Assert.AreEqual(0, game.Mission.Credits);
        game.Interact();
        Assert.AreEqual(100, game.Mission.Credits);
        game.StartMission();
        Assert.IsTrue(game.InMission);
        Assert.AreEqual(0, game.Mission.Kills);
        yield return null;
        Object.Destroy(game.gameObject);
    }
    static void Capture(Prototype game, string name)
    {
        if (System.Environment.GetEnvironmentVariable("POC_CAPTURE_PREVIEW") != "1" || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        var camera = game.Player.View;
        var target = new RenderTexture(1280, 720, 24);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        texture.Apply();
        System.IO.Directory.CreateDirectory("TestResults");
        System.IO.File.WriteAllBytes("TestResults/" + name + ".png", texture.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = null;
        Object.Destroy(target);
        Object.Destroy(texture);
    }
}
