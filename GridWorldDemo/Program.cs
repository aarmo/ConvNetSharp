using ConvNetSharp;
using System;

namespace GridWorldDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            var numInputs = GridWorld.GridSize * GridWorld.GridSize * GridWorld.GridDepth;
            var numActions = 4;
            var brain = new Brain(numInputs, numActions);

            // Initial output:
            var initialOutput = brain.DisplayOutput(brain.GetInputs());

            Console.WriteLine("Training...");
            brain.Train(2000);

            // Trained output:
            brain.World = GridWorld.StandardState();
            var trainedOutput = brain.DisplayOutput(brain.GetInputs());

            // Show results:
            Console.WriteLine(brain.World.DisplayGrid());
            Console.WriteLine("Actions=(UP, DOWN, LEFT, RIGHT)");
            Console.WriteLine($"Initial output: {initialOutput}");
            Console.WriteLine($"Trained output: {trainedOutput}");

            Console.ReadLine();

            // Play a game:


            Console.ReadLine();
        }
    }
}
