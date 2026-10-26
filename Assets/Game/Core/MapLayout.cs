using System;
using System.Collections.Generic;

namespace VideoGame.Core
{
    public readonly struct Room
    {
        public readonly int X, Y, Size, Theme;
        public Room(int x, int y, int size = 14, int theme = 0) { X = x; Y = y; Size = size; Theme = theme; }
    }

    public static class MapLayout
    {
        public const int Spacing = 20;
        public const int PassageWidth = 4;
        static readonly int[] DX = { 1, 0, -1, 0 }, DY = { 0, 1, 0, -1 };
        // Grow a frontier rather than a walk: every new room has a parent, while
        // adjacent branches reconnect naturally into loops. All choices use this seed.
        public static List<Room> Generate(int seed, int count = 8)
        {
            if (count < 1 || count > 64) throw new ArgumentOutOfRangeException(nameof(count));
            var random = new Random(seed);
            var result = new List<Room> { new Room(0, 0, 14, 0) };
            var occupied = new HashSet<(int, int)> { (0, 0) };
            while (result.Count < count)
            {
                var frontier = new List<(int, int)>();
                var unique = new HashSet<(int, int)>();
                foreach (var room in result)
                    for (int side = 0; side < 4; side++)
                    {
                        var cell = (room.X + DX[side], room.Y + DY[side]);
                        if (!occupied.Contains(cell) && unique.Add(cell)) frontier.Add(cell);
                    }
                var next = frontier[random.Next(frontier.Count)];
                occupied.Add(next);
                result.Add(new Room(next.Item1, next.Item2, 12 + random.Next(3) * 2, random.Next(3)));
            }
            return result;
        }
        public static bool Adjacent(Room a, Room b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) == 1;
    }
}
