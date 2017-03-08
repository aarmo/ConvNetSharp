using System;
using System.Collections.Generic;

namespace GridWorldDemo
{
    public class Location
    {
        public int X;
        public int Y;

        public Location(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object a)
        {
            var loc = a as Location;
            if (loc == null) return false;

            return loc == this;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator == (Location a, Location b)
        {
            return (a.X == b.X && a.Y == b.Y);
        }

        public static bool operator !=(Location a, Location b)
        {
            return !(a == b);
        }
    }
}
