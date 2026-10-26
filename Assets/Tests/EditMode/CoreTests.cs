using System;
using System.Linq;
using NUnit.Framework;
using VideoGame.Core;

public sealed class CoreTests
{
    [Test] public void FullMissionCycleRewardsOnlyOnceAndCanRepeat()
    {
        var mission = new Mission(2);
        Assert.IsFalse(mission.ClaimReward());
        mission.RegisterKill(); Assert.AreEqual(0, mission.Kills);
        Assert.IsTrue(mission.Accept()); Assert.IsFalse(mission.Accept());
        mission.RegisterKill(); Assert.AreEqual(MissionStage.Active, mission.Stage);
        mission.RegisterKill(); Assert.AreEqual(MissionStage.Complete, mission.Stage);
        mission.RegisterKill(); Assert.AreEqual(2, mission.Kills);
        Assert.IsTrue(mission.ClaimReward()); Assert.AreEqual(100, mission.Credits);
        Assert.IsFalse(mission.ClaimReward());
        Assert.IsTrue(mission.Accept()); Assert.AreEqual(0, mission.Kills);
    }
    [Test] public void FailureDiscardsProgressWithoutReward()
    {
        var mission = new Mission(2); mission.Accept(); mission.RegisterKill(); mission.Abandon();
        Assert.AreEqual(MissionStage.Available, mission.Stage);
        Assert.AreEqual(0, mission.Kills); Assert.AreEqual(0, mission.Credits);
        Assert.IsFalse(mission.ClaimReward()); Assert.IsTrue(mission.Accept());
    }
    [Test] public void InvalidInputsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Mission(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => MapLayout.Generate(1, 0));
    }
    [Test] public void ProceduralMapsAreRepeatableUniqueAndConnected()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            var map = MapLayout.Generate(seed);
            Assert.AreEqual(8, map.Count);
            Assert.AreEqual(8, map.Select(r => (r.X, r.Y)).Distinct().Count());
            CollectionAssert.AreEqual(map.Select(r => (r.X, r.Y, r.Size, r.Theme)), MapLayout.Generate(seed).Select(r => (r.X, r.Y, r.Size, r.Theme)));
            for (int i = 1; i < map.Count; i++)
                Assert.IsTrue(map.Take(i).Any(parent => MapLayout.Adjacent(parent, map[i])));
        }
    }
}
