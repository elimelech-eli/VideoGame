using System;
using System.Linq;
using VideoGame.Core;

static class Checks
{
    static int assertions;
    static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }
    static void Main()
    {
        var mission = new Mission(8);
        mission.RegisterKill(); Check(mission.Kills == 0, "Inactive kills count");
        Check(!mission.ClaimReward(), "Premature reward");
        Check(mission.Accept(), "Cannot accept"); Check(!mission.Accept(), "Duplicate acceptance");
        for (int i = 0; i < 7; i++) mission.RegisterKill();
        Check(mission.Stage == MissionStage.Active, "Completed too early");
        mission.RegisterKill(); Check(mission.Stage == MissionStage.Complete, "Not completed");
        mission.RegisterKill(); Check(mission.Kills == 8, "Kill count exceeds objective");
        Check(mission.ClaimReward() && mission.Credits == 100, "Reward missing");
        Check(!mission.ClaimReward() && mission.Credits == 100, "Duplicate reward");
        Check(mission.Accept() && mission.Kills == 0, "Cannot replay");
        mission.RegisterKill(); mission.Abandon();
        Check(mission.Stage == MissionStage.Available && mission.Kills == 0 && mission.Credits == 100, "Failure state corrupt");
        for (int seed = 0; seed < 1000; seed++)
        {
            var rooms = MapLayout.Generate(seed);
            Check(rooms.Count == 6, "Wrong room count");
            Check(rooms.Select(r => (r.X, r.Y)).Distinct().Count() == 6, "Overlapping rooms");
            Check(rooms.Select(r => (r.X, r.Y)).SequenceEqual(MapLayout.Generate(seed).Select(r => (r.X, r.Y))), "Nondeterministic seed");
            for (int i = 1; i < rooms.Count; i++)
                Check(Math.Abs(rooms[i].X - rooms[i-1].X) + Math.Abs(rooms[i].Y - rooms[i-1].Y) == 1, "Disconnected map");
        }
        Console.WriteLine($"PASS: {assertions} assertions; mission lifecycle, replay, failure, and 1,000 procedural seeds.");
    }
}
