using ConvNetSharp.Serialization;
using Newtonsoft.Json;
using System;
using System.IO;

namespace GridWorldDemo
{
    class Program
    {
        private const string BrainFile = @".\GridWorld.brain";
        private static string[] _actionNames = { "UP", "DOWN", "LEFT", "RIGHT" };

        static void Main(string[] args)
        {
            Console.WriteLine(" ----------------------- ");
            Console.WriteLine("| G.R.I.D --- W.O.R.L.D |");
            Console.WriteLine(" ----------------------- ");
            Console.WriteLine("Tutorial: http://outlace.com/Reinforcement-Learning-Part-3/\n");

            Brain brain;
            if (File.Exists(BrainFile))
            {
                brain = ReadBrainFromFile(BrainFile);

                Console.WriteLine("Brain loaded...");
                Console.WriteLine($"Created: {brain.CreatedDate}. Training Time: {brain.TrainingTime} ({brain.TotalTrainingGames} games)");
                Console.WriteLine($"Avg loss: {brain.TotalLoss / brain.TotalTrainingMoves}. Last: {brain.LastLoss}");
            }
            else
            {
                var numInputs = GridWorld.GridSize * GridWorld.GridSize * GridWorld.GridDepth;
                var numActions = 4;
                brain = new Brain(numInputs, numActions);
            }

            // Initial output:
            var initialOutput = brain.DisplayOutput(brain.GetInputs());

            //Console.WriteLine("Training...");
            //brain.Train(100, 1f);

            Console.WriteLine("Batch Training...");
            brain.TrainWithExperienceReplay(2900, 40, 0.5f);

            // Sample output:
            brain.World = GridWorld.RandomPlayerState();
            var trainedOutput = brain.DisplayOutput(brain.GetInputs());

            // Show results:
            Console.WriteLine(brain.World.DisplayGrid());
            Console.WriteLine($"Actions: ({_actionNames[0]} {_actionNames[1]} {_actionNames[2]} {_actionNames[3]})");
            Console.WriteLine($"Initial output: {initialOutput}");
            Console.WriteLine($"Sample output: {trainedOutput}");
            SaveBrainToFile(brain, BrainFile);

            Console.WriteLine("\nBrain saved...\nPress enter to play some games...");
            Console.ReadLine();

            // Play some games:
            do
            {
                Console.Clear();
                brain.World = GridWorld.RandomPlayerState();
                Console.WriteLine("Initial state:");
                Console.WriteLine(brain.World.DisplayGrid());

                var moves = 0;
                while (!brain.World.GameOver() && moves < 10)
                {
                    var action = brain.GetNextAction();
                    
                    Console.WriteLine($"\nMove: {++moves}. Taking action: {_actionNames[action]}");
                    brain.World.MovePlayer(action);
                    Console.WriteLine(brain.World.DisplayGrid());
                }

                if (moves >= 10)
                {
                    Console.WriteLine($"Game Over. Too many moves!");
                }
                else
                {
                    Console.WriteLine($"Game {(brain.World.GetReward() == 10 ? "WON!" : "LOST! :(")}");
                }

                Console.WriteLine("\nPress enter to play another game...");
                Console.ReadLine();
            } while (true);
        }

        private static void SaveBrainToFile(Brain brain, string filename)
        {
            brain.NetJson = brain.Net.ToJSON();
            File.WriteAllText(filename, JsonConvert.SerializeObject(brain));
        }

        private static Brain ReadBrainFromFile(string filename)
        {
            var brain = JsonConvert.DeserializeObject<Brain>(File.ReadAllText(filename));
            brain.Net = SerializationExtensions.FromJSON(brain.NetJson);

            brain.NetJson = string.Empty;

            return brain;
        }
    }
}
