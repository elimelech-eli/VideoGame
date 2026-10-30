using System;
using System.Collections.Generic;
namespace VideoGame.Core
{
    public enum StationRole { TicketHall, Concourse, Waiting, Maintenance, Platform }
    // v1: the railway is an exterior scenic reservation, never part of a room or corridor.
    public readonly struct Railway
    {
        public readonly int RoomIndex;
        public readonly float X,Z;
        public const float Width=32,Depth=6;
        public Railway(int roomIndex,float x,float z){RoomIndex=roomIndex;X=x;Z=z;}
    }
    public static class TransitLayout
    {
        public static Railway Reserve(IReadOnlyList<Room> rooms)
        {
            if(rooms==null||rooms.Count==0)throw new ArgumentException("A station needs rooms.");
            int pick=0;
            for(int i=1;i<rooms.Count;i++)
                if(rooms[i].Y>rooms[pick].Y||rooms[i].Y==rooms[pick].Y&&rooms[i].X>rooms[pick].X)pick=i;
            // Every legal room extends at most 8 m and corridors never leave the top row.
            // Railway starts 11 m north of that row, leaving a minimum 3 m separation.
            return new Railway(pick,rooms[pick].X*MapLayout.Spacing,rooms[pick].Y*MapLayout.Spacing+14);
        }
        public static StationRole Role(int index,int seed,Railway rail)
        {
            if(index==0)return StationRole.TicketHall;
            if(index==rail.RoomIndex)return StationRole.Platform;
            return (StationRole)(1+(int)((uint)(index+(seed%3)+3)%3));
        }
    }
}
