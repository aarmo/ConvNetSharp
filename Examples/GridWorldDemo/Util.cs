using ConvNetSharp.Serialization;
using Newtonsoft.Json;
using System;
using System.IO;

namespace GridWorldDemo
{
    public static class Util
    {
        public static Random Rnd = new Random();

        public static Location RandLoc(int min, int max)
        {
            return new Location(Rnd.Next(min, max), Rnd.Next(min, max));
        }

        public static void SaveBrainToFile(Brain brain, string filename)
        {
            brain.NetJson = brain.Net.ToJSON();
            File.WriteAllText(filename, JsonConvert.SerializeObject(brain));
        }

        public static Brain ReadBrainFromFile(string filename)
        {
            var brain = JsonConvert.DeserializeObject<Brain>(File.ReadAllText(filename));
            brain.Net = SerializationExtensions.FromJSON(brain.NetJson);

            brain.NetJson = string.Empty;

            return brain;
        }
    }
}
