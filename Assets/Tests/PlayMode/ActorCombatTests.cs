using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;
using VideoGame.Core;

public sealed class ActorCombatTests
{
    Prototype game;
    [UnitySetUp] public IEnumerator Setup()
    {
        var old = Object.FindFirstObjectByType<Prototype>();
        if (old != null) Object.Destroy(old.gameObject);
        yield return null;
        game = new GameObject("Actor combat test").AddComponent<Prototype>(); yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if (game != null) Object.Destroy(game.gameObject); yield return null;
    }
    [UnityTest] public IEnumerator AnimationClipsDeformVisibleMeshAndMovementSelectsLocomotion()
    {
        var motion = game.Player.Motion;
        Assert.AreEqual(11, motion.Player.GetClipCount());
        motion.Player.Play("Idle"); motion.Player.Sample();
        foreach (var foot in motion.GetComponentsInChildren<Transform>())
            if (foot.name == "foot_L" || foot.name == "foot_R")
                foreach (var toe in foot.GetComponentsInChildren<Transform>())
                    if (toe.name.StartsWith("toe_")) Assert.Greater(Vector3.Dot(Vector3.ProjectOnPlane(toe.position - foot.position,Vector3.up).normalized, game.Player.transform.forward), .5f, "Feet must face the same way as the actor and weapon");
        var skin = motion.GetComponentInChildren<SkinnedMeshRenderer>();
        Assert.IsNotNull(skin, "Actor needs a skin bound to the animated skeleton");
        var a = new Mesh(); var b = new Mesh();
        motion.Player.Play("RunForward"); motion.Player["RunForward"].normalizedTime = .15f; motion.Player.Sample(); skin.BakeMesh(a);
        motion.Player["RunForward"].normalizedTime = .65f; motion.Player.Sample(); skin.BakeMesh(b);
        float difference = 0;
        var av = a.vertices; var bv = b.vertices;
        for (int i = 0; i < av.Length; i++) difference = Mathf.Max(difference, Vector3.Distance(av[i], bv[i]));
        Assert.Greater(difference, .05f, "Moving bones must deform the visible mesh, not just an unused rig");
        foreach (string side in new[] {"L", "R"})
        {
            motion.Player["RunForward"].normalizedTime = side == "L" ? .25f : .75f; motion.Player.Sample();
            Transform thigh = null, knee = null, ankle = null;
            foreach (var bone in motion.GetComponentsInChildren<Transform>())
            {
                if (bone.name == "thigh_" + side) thigh = bone;
                if (bone.name == "shin_" + side) knee = bone;
                if (bone.name == "foot_" + side) ankle = bone;
            }
            Vector3 chord = ankle.position - thigh.position;
            Vector3 closest = thigh.position + chord * Vector3.Dot(knee.position - thigh.position, chord) / chord.sqrMagnitude;
            Assert.Greater(Vector3.Dot(knee.position - closest, game.Player.transform.forward), .03f, "Knee must bend forward, not invert behind the leg");
        }
        Object.Destroy(a); Object.Destroy(b);
        game.Player.enabled = false; game.Player.transform.rotation = Quaternion.identity; motion.Revive();
        for (int i = 0; i < 8; i++) { game.Player.transform.position += Vector3.forward * (4.5f * Time.deltaTime); yield return null; }
        Assert.AreEqual("RunForward", motion.CurrentState);
        game.Player.enabled = true;
    }
    [UnityTest] public IEnumerator ApprovedOperatorKeepsWeaponAttachedAndSkinFiniteAcrossActions()
    {
        var motion = game.Player.Motion;
        Assert.That(motion.name, Does.StartWith(motion.GetComponent<PlayerRig>()!=null?"OperativeAsuna":"OperativeV03"));
        Transform hand = null, forearm = null, socket = null;
        foreach (var bone in motion.GetComponentsInChildren<Transform>())
        {
            if (bone.name == "hand_R") hand = bone;
            if (bone.name == "forearm_R") forearm = bone;
            if (bone.name == "SOCKET_weapon_R") socket = bone;
        }
        Assert.IsNotNull(socket); Assert.AreSame(hand, socket.parent);
        Assert.IsNotNull(game.Player.Weapons.Weapon(0).GetComponentInChildren<MeshRenderer>(), "Equipped weapon must render");
        motion.enabled = false; motion.Player.enabled = false;
        var skin = motion.GetComponentInChildren<SkinnedMeshRenderer>(); var baked = new Mesh();
        foreach (string action in new[] {"Idle","RunForward","RunBack","RunLeft","RunRight","Fire","Melee","Cast","Dash","Hit","Death"})
        {
            foreach (float phase in new[] {.15f,.65f})
            {
                motion.Player.Stop(); motion.Player.Play("Idle"); motion.Player["Idle"].normalizedTime = 0; motion.Player.Sample();
                motion.Player.Play(action); motion.Player[action].normalizedTime = phase; motion.Player.Sample();
                Assert.Less(Vector3.Distance(hand.position, socket.position), .15f, action + " weapon detached");
                // Native clips may lower the gun. The runtime aim layer owns
                // aim and support-hand contact rather than the raw clip pose.
                game.Player.Weapons.Refresh(game.Equipment.Profile);
                Assert.Less(Vector3.Distance(game.Player.Weapons.Weapon(0).position,game.Player.Weapons.Grip(0)),.01f,action+" weapon lost its palm attachment");
                skin.BakeMesh(baked);
                Assert.IsFalse(float.IsNaN(baked.bounds.size.sqrMagnitude), action + " produced nonfinite skin bounds");
                Assert.Less(baked.bounds.size.magnitude, 5, action + " exploded the bound armor");
            }
        }
        Object.Destroy(baked); yield return null;
    }
    [UnityTest] public IEnumerator NativeLocomotionMovesBodyAndLoopsContinuously()
    {
        var motion = game.Player.Motion; motion.enabled = false;
        Transform chest = null, pelvis = null;
        foreach (var bone in motion.GetComponentsInChildren<Transform>())
        {
            if (bone.name == "chest") chest = bone;
            if (bone.name == "hips") pelvis = bone;
        }
        foreach (string clip in new[] {"RunForward","RunBack","RunLeft","RunRight"})
        {
            motion.Player.Stop(); motion.Player.Play(clip);
            motion.Player[clip].normalizedTime = 0; motion.Player.Sample();
            Vector3 neutral=chest.position;float bodyMotion=0;
            for(int phase=1;phase<8;phase++){motion.Player[clip].normalizedTime=phase/8f;motion.Player.Sample();bodyMotion=Mathf.Max(bodyMotion,Vector3.Distance(neutral,chest.position));}
            Assert.Greater(bodyMotion,.01f,clip+" body is frozen above moving legs");
            motion.Player[clip].normalizedTime = 0; motion.Player.Sample();
            Vector3 start = chest.position; Quaternion rotation = chest.rotation;
            motion.Player[clip].normalizedTime = .9999f; motion.Player.Sample();
            Assert.Less(Vector3.Distance(start, chest.position), .002f, clip + " position pops at loop seam");
            Assert.Less(Quaternion.Angle(rotation, chest.rotation), .2f, clip + " rotation pops at loop seam");
        }
        yield return null;
    }
    [UnityTest] public IEnumerator MedicalCacheDropsOneCappedHealAndPreservesFullHealthPickup()
    {
        var cache = game.SpawnMedicalCache(new Vector3(4, 0, 0));
        game.Player.Health = 85;
        Physics.SyncTransforms(); cache.Damage(40); cache.Damage(100);
        var drops = Object.FindObjectsByType<HealthPickup>(FindObjectsSortMode.None);
        Assert.AreEqual(1, drops.Length);
        Assert.AreEqual(85, game.Player.Health, "Breaking a supply cache must not explode");
        Assert.IsTrue(drops[0].TryCollect(game.Player)); Assert.AreEqual(100, game.Player.Health);
        Assert.IsFalse(drops[0].TryCollect(game.Player));
        var spare = game.SpawnHealthPickup(new Vector3(5, .3f, 0));
        Assert.IsFalse(spare.TryCollect(game.Player)); Assert.IsFalse(spare.Consumed);
        game.Player.Health = 40; game.Player.Teleport(new Vector3(5, 1, 0));
        yield return null; yield return null;
        Assert.AreEqual(70, game.Player.Health, "Nearby medkits should collect automatically");
    }
    [UnityTest] public IEnumerator PlayerDiesBeforeRecallAndCannotActOrHealUntilRevived()
    {
        game.StartMission(); game.Director.enabled = false;
        foreach (var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None)) enemy.enabled = false;
        game.Player.Teleport(new Vector3(3, 1, 3));
        game.Player.Damage(100);
        Assert.IsTrue(game.InMission); Assert.IsTrue(game.Player.IsDead);
        Assert.AreEqual("Death", game.Player.Motion.CurrentState);
        Assert.IsFalse(game.Player.GetComponent<CharacterController>().enabled);
        Assert.IsFalse(game.Player.Heal(100));
        game.Interact(); game.StartMission();
        yield return new WaitForSeconds(.7f);
        Assert.IsTrue(game.InMission, "Recall must wait for the death animation");
        Assert.IsTrue(game.Player.Motion.Player.IsPlaying("Death"));
        yield return new WaitForSeconds(1.1f);
        Assert.IsFalse(game.InMission); Assert.IsFalse(game.Player.IsDead);
        Assert.AreEqual(100, game.Player.Health); Assert.AreEqual(MissionStage.Available, game.Mission.Stage);
        Assert.AreEqual(0, game.Mission.Credits); Assert.AreEqual(0, game.Mission.Kills);
        Assert.Less(Vector3.Distance(game.Player.transform.position, Vector3.up), .2f);
        game.StartMission(); Assert.IsTrue(game.InMission);
    }
    [UnityTest] public IEnumerator EnemyShotsCollapseCoverOntoPlayerBehindIt()
    {
        game.Player.Teleport(new Vector3(0, 1, 5.5f));
        var wall = game.SpawnDestructible(new Vector3(0, 0, 4), true);
        Physics.SyncTransforms();
        for (int i = 0; i < 4; i++)
        {
            var bolt = new GameObject("Cover test shot"); bolt.transform.position = new Vector3(0, 1, 2);
            bolt.AddComponent<EnemyBolt>().Direction = Vector3.forward;
            yield return new WaitForSeconds(.3f);
        }
        Assert.IsTrue(wall.IsDestroyed);
        yield return new WaitForSeconds(.85f);
        Assert.AreEqual(65, game.Player.Health, "Enemy-hit cover must fall away from the shooter and crush the player behind it");
    }
    [UnityTest] public IEnumerator RusherClosesFastAndMeleeCanBeDodgedByLeavingRange()
    {
        game.StartMission(); game.Director.enabled = false;
        foreach (var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None)) enemy.enabled = false;
        var rusher = game.SpawnDrone(new Vector3(0, 1, 5), EnemyKind.Rusher);
        var hunter = game.SpawnDrone(new Vector3(4, 1, 5), EnemyKind.Hunter);
        Vector3 rushStart = rusher.transform.position, huntStart = hunter.transform.position;
        yield return new WaitForSeconds(.25f);
        Assert.Greater(Vector3.Distance(rushStart, rusher.transform.position), Vector3.Distance(huntStart, hunter.transform.position) * 1.4f);
        hunter.enabled = false;
        rusher.transform.position = new Vector3(0, 1, 1.5f); Physics.SyncTransforms();
        yield return null; yield return null;
        Assert.IsTrue(rusher.WindingUp);
        Assert.AreEqual("Melee", rusher.Motion.CurrentState);
        game.Player.Teleport(new Vector3(0, 1, -4));
        yield return new WaitForSeconds(.35f); Assert.AreEqual(100, game.Player.Health);
    }
    [UnityTest] public IEnumerator BombardierMarksFixedBlastThatCanBeEscapedAndBreaksProps()
    {
        game.StartMission(); game.Director.enabled = false;
        foreach (var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None)) enemy.enabled = false;
        var bombardier = game.SpawnDrone(new Vector3(0, 1, 6), EnemyKind.Bombardier);
        var cache = game.SpawnMedicalCache(new Vector3(1, 0, 0));
        Physics.SyncTransforms();
        float deadline = Time.realtimeSinceStartup + 5;
        EnemyMortar mortar = null;
        while (mortar == null && Time.realtimeSinceStartup < deadline) { mortar = Object.FindFirstObjectByType<EnemyMortar>(); yield return null; }
        Assert.IsNotNull(mortar); Vector3 target = mortar.Target;
        Assert.AreEqual(100, game.Player.Health, "Warning must precede damage");
        bombardier.enabled = false;
        game.Player.Teleport(new Vector3(-5, 1, 0));
        yield return new WaitForSeconds(EnemyMortar.WarningDuration + .15f);
        Assert.IsTrue(mortar == null || mortar.Detonated);
        Assert.AreEqual(100, game.Player.Health); Assert.IsTrue(cache == null || cache.IsDestroyed);
        var direct = new GameObject("Direct mortar test").AddComponent<EnemyMortar>();
        direct.transform.SetParent(game.transform); direct.Initialize(Vector3.up * 5, game.Player.transform.position);
        yield return new WaitForSeconds(EnemyMortar.WarningDuration + .15f);
        Assert.AreEqual(76, game.Player.Health, "A player who stays in the marker must take AOE damage");
    }
}
