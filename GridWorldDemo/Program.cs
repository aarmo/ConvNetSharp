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

            Console.WriteLine(brain.World.DisplayGrid());

            // Initial Output:
            var input1 = brain.GetInputs();
            Console.WriteLine(brain.DisplayOutput(input1));

            brain.Train();

            // Trained Output:
            brain.World = GridWorld.StandardState();
            var input2 = brain.GetInputs();
            Console.WriteLine(brain.DisplayOutput(input2));
            
            Console.ReadLine();
        }
    }
}
