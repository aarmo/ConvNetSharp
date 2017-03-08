using System;

namespace GridWorldDemo
{
    public class Util
    {
        public static Random Rnd = new Random();

        public static Location RandLoc(int min, int max)
        {
            return new Location(Rnd.Next(min, max), Rnd.Next(min, max));
        }
    }
}
