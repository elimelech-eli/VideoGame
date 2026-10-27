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
            Check(rooms.Count == 8, "Wrong room count");
            Check(rooms.Select(r => (r.X, r.Y)).Distinct().Count() == 8, "Overlapping rooms");
            Check(rooms.Select(r => (r.X, r.Y, r.Size, r.Theme)).SequenceEqual(MapLayout.Generate(seed).Select(r => (r.X, r.Y, r.Size, r.Theme))), "Nondeterministic seed");
            Check(rooms.All(r => r.Size >= 12 && r.Size <= 16 && r.Size < MapLayout.Spacing && r.Theme >= 0 && r.Theme < 3), "Invalid room geometry/content");
            Check(rooms[0].X == 0 && rooms[0].Y == 0 && rooms[0].Size == 14, "Unsafe starting room");
            for (int i = 1; i < rooms.Count; i++)
                Check(rooms.Take(i).Any(parent => MapLayout.Adjacent(parent, rooms[i])), "Disconnected map");
        }
        foreach (int count in new[] {1,2,16,64}) Check(MapLayout.Generate(-123,count).Count == count,"Custom room count");
        var layouts = Enumerable.Range(0,100).Select(seed => string.Join(";", MapLayout.Generate(seed).Select(r => $"{r.X},{r.Y},{r.Size},{r.Theme}"))).Distinct().Count();
        Check(layouts > 95,"Seeds do not vary the map");
        Check(Enumerable.Range(0,100).Any(seed => MapLayout.Generate(seed).Any(room => MapLayout.Generate(seed).Count(other => MapLayout.Adjacent(room,other)) >= 3)), "No branching layouts");
        for (int seed = -50; seed < 50; seed++)
            for (int biome = 0; biome < 6; biome++)
                foreach (int size in new[] {6, 10, 20})
                {
                    var recipe = new Deployment(seed, biome, size);
                    var map = MapLayout.Generate(recipe.Seed, recipe.Rooms);
                    Check(map.Count == size && (int)recipe.Biome == biome, "Deployment recipe not honored");
                    Check(map.Select(r => (r.X,r.Y)).Distinct().Count() == size, "Biome map overlaps");
                    Check(map.Skip(1).All(r => map.Take(map.IndexOf(r)).Any(parent => MapLayout.Adjacent(parent,r))), "Biome map disconnected");
                }
        var randomRecipes = Enumerable.Range(0, 200).Select(seed => new Deployment(seed)).ToArray();
        Check(randomRecipes.Select(r => r.Biome).Distinct().Count() == 6, "Random biomes missing");
        Check(randomRecipes.Select(r => r.Rooms).Distinct().OrderBy(n => n).SequenceEqual(new[] {6,10,20}), "Random sizes missing");
        Check(randomRecipes.All(r => new Deployment(r.Seed).Rooms == r.Rooms && new Deployment(r.Seed).Biome == r.Biome), "Recipe not reproducible");
        Console.WriteLine($"PASS: {assertions} assertions; mission lifecycle, replay, failure, and 1,000 procedural seeds.");
    }
}
