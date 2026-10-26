using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;

public sealed class CombatFeedbackTests
{
    Prototype game;
    [UnitySetUp] public IEnumerator Setup()
    {
        var old = Object.FindFirstObjectByType<Prototype>();
        if (old != null) Object.Destroy(old.gameObject);
        yield return null;
        game = new GameObject("Feedback test").AddComponent<Prototype>();
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        Time.timeScale = 1; AudioListener.pause = false;
        Object.Destroy(game.gameObject); yield return null;
    }
    [UnityTest] public IEnumerator FeedbackCannotDamageAndChainBurstsStayBoundedThenCleanUp()
    {
        var enemy = game.SpawnDrone(new Vector3(3, 1, 0), false); enemy.enabled = false;
        int sources = game.GetComponentsInChildren<AudioSource>(true).Length;
        int renderers = game.GetComponentsInChildren<ParticleSystem>(true).Length;
        for (int i = 0; i < 80; i++)
        {
            game.Feedback.Blast(Vector3.up, 4);
            game.Feedback.Contact(enemy.transform.position, true);
            game.Feedback.Shot(Vector3.up, Vector3.forward * 3 + Vector3.up, Color.cyan);
        }
        yield return null;
        Assert.AreEqual(100, game.Player.Health); Assert.AreEqual(60, enemy.Health);
        Assert.LessOrEqual(game.Feedback.ActiveEffects, CombatFeedback.EffectCap);
        Assert.LessOrEqual(game.Feedback.ActiveVoices, CombatFeedback.VoiceCap);
        int particles = 0;
        foreach (var system in game.GetComponentsInChildren<ParticleSystem>()) particles += system.particleCount;
        Assert.Greater(particles, 0, "Confirmed events must emit real particles");
        Assert.AreEqual(sources, game.GetComponentsInChildren<AudioSource>(true).Length);
        Assert.AreEqual(renderers, game.GetComponentsInChildren<ParticleSystem>(true).Length);
        int poolLights = 0;
        foreach (var light in game.GetComponentsInChildren<Light>())
            if (light.name == "Pooled combat effect" && light.enabled) poolLights++;
        Assert.LessOrEqual(poolLights, 2);
        yield return new WaitForSeconds(2);
        Assert.AreEqual(0, game.Feedback.ActiveEffects); Assert.AreEqual(0, game.Feedback.ActiveVoices);
        game.Feedback.Blast(Vector3.up, 4); game.BuildHub(); yield return null;
        Assert.AreEqual(0, game.Feedback.ActiveEffects); Assert.AreEqual(0, game.Feedback.ActiveVoices);
        Assert.AreEqual(0, game.Feedback.Shake);
    }
    [UnityTest] public IEnumerator PauseFreezesPresentationAndReducedSettingsPreserveGameplay()
    {
        game.Feedback.Blast(Vector3.up, 4); yield return null;
        float shake = game.Feedback.Shake; int events = game.Feedback.EventCount;
        game.Menus.Pause();
        game.Feedback.Blast(Vector3.up, 4);
        yield return new WaitForSecondsRealtime(.25f);
        Assert.AreEqual(events, game.Feedback.EventCount); Assert.AreEqual(shake, game.Feedback.Shake);
        Assert.AreEqual(Vector3.zero, game.Feedback.CameraOffset(game.Player.View));
        game.Menus.Resume(); yield return null;
        game.Feedback.ResetAll();
        game.Menus.Settings.ReducedEffects = true;
        game.Menus.Settings.ScreenShake = 0;
        game.Menus.Settings.Effects = 0;
        game.Feedback.Blast(Vector3.up, 4); yield return null;
        Assert.AreEqual(0, game.Feedback.Shake);
        foreach (var source in game.GetComponentsInChildren<AudioSource>())
            if (source.name == "Combat voice") Assert.AreEqual(0, source.volume);
        foreach (var light in game.GetComponentsInChildren<Light>())
            if (light.name == "Pooled combat effect") Assert.IsFalse(light.enabled);
        Assert.AreEqual(100, game.Player.Health);
        game.Feedback.Enabled = false; yield return null;
        Assert.AreEqual(0, game.Feedback.ActiveEffects); Assert.AreEqual(0, game.Feedback.ActiveVoices);
    }
    [Test] public void SynthesizedCuesAreFiniteAndBoundedWithCompatibleSettingsDefaults()
    {
        foreach (CombatCue cue in System.Enum.GetValues(typeof(CombatCue)))
        {
            var clip = CombatFeedback.Synthesize(cue, 0);
            var samples = new float[clip.samples]; clip.GetData(samples, 0);
            double energy = 0;
            foreach (float sample in samples)
            {
                Assert.IsFalse(float.IsNaN(sample) || float.IsInfinity(sample));
                Assert.LessOrEqual(Mathf.Abs(sample), .9f); energy += sample * sample;
            }
            Assert.Greater(energy / samples.Length, .001, "Cue must contain an audible signal");
            Object.DestroyImmediate(clip);
        }
        var old = JsonUtility.FromJson<GameSettings>("{\"Version\":1,\"Effects\":0.4}");
        Assert.AreEqual(.5f, old.ScreenShake); Assert.IsFalse(old.ReducedEffects);
    }
}
