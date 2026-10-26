using System;
using System.Collections.Generic;

namespace VideoGame.Core
{
    public readonly struct Room
    {
        public readonly int X;
        public readonly int Y;
        public Room(int x, int y) { X = x; Y = y; }
    }

    public static class MapLayout
    {
        // A monotonic walk creates unique rooms connected by full-width doorways.
        public static List<Room> Generate(int seed, int count = 6)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            var random = new Random(seed);
            var result = new List<Room> { new Room(0, 0) };
            int x = 0, y = 0;
            for (int i = 1; i < count; i++)
            {
                if (random.Next(2) == 0) x++; else y++;
                result.Add(new Room(x, y));
            }
            return result;
        }
    }
}
