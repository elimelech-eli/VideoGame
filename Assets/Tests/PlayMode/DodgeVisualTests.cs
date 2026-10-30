using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;
using VideoGame.Core;

public sealed class DodgeVisualTests
{
    [UnityTest]
    public IEnumerator DirectionalEvadeReplacesJumpRecoversAndFreezesWithPause()
    {
        var old = Object.FindFirstObjectByType<Prototype>();
        if (old != null) Object.Destroy(old.gameObject);
        yield return null;
        var game = new GameObject("Evade test").AddComponent<Prototype>();
        yield return null;
        game.StartMission(); game.Director.enabled=false;
        foreach(var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))enemy.gameObject.SetActive(false);
        int captureRate = Time.captureFramerate;
        Time.captureFramerate = 60;
        try
        {
            var pilot = game.Player; pilot.enabled = false;
            Vector3 origin=pilot.transform.position;
            var motion = pilot.Motion;
            var bones = motion.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name,t=>t);
            foreach (var loadout in new[]{("rifle",(string)null),("pistol",(string)null),("pistol","pistol"),("sword","shield"),("pistol","sword"),("sword","sword")})
            foreach (Vector3 direction in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right})
            {
                var profile = game.Equipment.Profile;
                profile.Equipped[3] = GearCatalog.Roll(new System.Random(1),1,GearQuality.White,loadout.Item1);
                profile.Equipped[4] = loadout.Item2==null ? null : GearCatalog.Roll(new System.Random(2),1,GearQuality.White,loadout.Item2);
                game.Equipment.RefreshVisuals();
                pilot.Teleport(origin);
                motion.Dash(direction);
                for(int frame=0;frame<6;frame++)
                {
                    pilot.transform.position+=direction*(22f/60);
                    yield return null;
                }
                var visual = motion.GetComponent<DodgeVisual>();
                Assert.AreEqual("Dash",motion.CurrentState);
                Assert.IsFalse(motion.Player.IsPlaying("Dash"),"The jump clip must never play during an evade");
                Assert.AreEqual(direction,visual.Direction);
                Assert.Greater(visual.Weight,.8f);
                Assert.IsFalse(float.IsNaN(bones["hand_R"].position.x));
                for(int hand=0;hand<2;hand++)
                    if(pilot.Weapons.Weapon(hand)!=null)Assert.Less(Vector3.Distance(pilot.Weapons.Grip(hand),pilot.Weapons.Weapon(hand).position),.001f);
                if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    float size=pilot.View.orthographicSize;pilot.View.orthographicSize=2.4f;
                    game.CaptureFrame("TestResults/evade-"+loadout.Item1+"-"+loadout.Item2+"-"+direction+".png");
                    pilot.View.orthographicSize=size;
                }
                float weight = visual.Weight;
                Time.timeScale = 0;
                yield return null; yield return null;
                Assert.AreEqual(weight,visual.Weight,"Pause must freeze presentation");
                Time.timeScale = 1;
                yield return new WaitForSeconds(.28f);
                Assert.IsFalse(visual.Active);
                Assert.AreEqual(0,visual.Weight);
                Assert.AreEqual("Idle",motion.CurrentState);
                Assert.IsTrue(visual.GetComponentsInChildren<TrailRenderer>().All(t=>!t.emitting));
            }
        }
        finally { Time.timeScale = 1; Time.captureFramerate = captureRate; Object.Destroy(game.gameObject); }
        yield return null;
    }
}
