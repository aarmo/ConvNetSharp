using System;
using ConvNetSharp;
using ConvNetSharp.Layers;
using ConvNetSharp.Training;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// Based on: http://outlace.com/Reinforcement-Learning-Part-3/
/// </summary>
namespace GridWorldDemo
{
    public class Brain
    {
        public Net Net;
        public SgdTrainer Trainer;
        public GridWorld World;
        private int _numInputs;
        private int _numActions;

        public Brain(int numInputs, int numActions)
        {
            _numInputs = numInputs;
            _numActions = numActions;

            // network
            Net = new Net();
            Net.AddLayer(new InputLayer(1, 1, numInputs));
            Net.AddLayer(new FullyConnLayer(numInputs*2));
            Net.AddLayer(new ReluLayer());
            Net.AddLayer(new FullyConnLayer(numInputs));
            Net.AddLayer(new ReluLayer());
            Net.AddLayer(new FullyConnLayer(numActions));
            Net.AddLayer(new RegressionLayer());

            Trainer = new SgdTrainer(Net) { LearningRate = 0.01, Momentum = 0.0, BatchSize = 1, L2Decay = 0.001 };

            World = GridWorld.StandardState();
        }

        public Volume GetInputs()
        {
            var i = 0;
            var j = 0;
            var k = 0;
            var input = new Volume(1, 1, _numInputs);
            for (var d = 0; d < input.Depth; d++)
            {   
                var v = World.WorldState[i, j, k];
                input.Set(0, 0, d, v);
                
                k++;
                if (k >= GridWorld.GridDepth)
                {
                    k = 0;
                    j++;
                    if (j >= GridWorld.GridSize)
                    {
                        j = 0;
                        i++;
                    }
                }
            }

            return input;
        }
        
        public string DisplayOutput(Volume inputs)
        {
            var result = Net.Forward(inputs);

            var sb = new StringBuilder();

            sb.Append("Outputs: (");
            foreach (var r in result)
            {
                sb.Append($"{r} ");
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append(")");

            return sb.ToString();
        }

        public void Train(int iterations)
        {
            var avLoss = 0.0;
            var lastLoss = 0.0;
            var gamma = 0.9f;
            var epsilon = 1f;
            var gameMoves = 0;
            var totalMoves = 0;
            var totalGames = 0;
            var startTime = DateTime.Now;

            for (var i = 0; i < iterations; i++)
            {
                World = GridWorld.StandardState();

                double updatedReward;
                var gameRunning = true;
                gameMoves = 0;
                while (gameRunning)
                {
                    //# We are in state S
                    //# Let's run our Q function on S to get Q values for all possible actions
                    var state = GetInputs();
                    var qVal = Net.Forward(state);
                    var action = 0;

                    if (Util.Rnd.NextDouble() < epsilon)
                    {
                        //# Choose random action
                        action = Util.Rnd.Next(_numActions);
                    }
                    else
                    {
                        //# Choose best action from Q(s,a) values
                        action = MaxValueIndex(qVal);
                    }

                    //# Take action, observe new state S'
                    World.MovePlayer(action);
                    gameMoves++;
                    totalMoves++;
                    var newState = GetInputs();

                    //# Observe reward
                    var reward = World.GetReward();

                    //# Get max_Q(S',a)
                    var newQ = Net.Forward(newState);
                    var y = GetValues(newQ);
                    var maxQ = MaxValue(newQ);

                    if (reward == -1)
                    {
                        //# Non-terminal state
                        updatedReward = (reward + (gamma * maxQ));
                    }
                    else
                    {
                        //# Terminal state
                        updatedReward = reward;
                        gameRunning = false;
                        totalGames++;
                        Console.WriteLine($"Game: {totalGames}. Moves: {gameMoves}. {(reward == 10 ? "WIN!" : "")}");
                    }

                    //# Target output
                    y[action] = updatedReward;

                    //# Feedback what the score would be for this action
                    Trainer.Train(state, y);
                    avLoss += Trainer.Loss;
                }

                //# Slowly reduce the chance of choosing a random action
                if (epsilon > 0.05f) epsilon -= (1f / iterations);
            }
            lastLoss = Trainer.Loss;
            avLoss /= totalMoves;
            Console.WriteLine($"Avg loss: {avLoss}. Last: {lastLoss}");
            Console.WriteLine($"Training duration: {DateTime.Now - startTime}");
        }

        private double[] GetValues(IVolume qVal)
        {
            var l = new List<double>();

            foreach (var r in qVal)
            {
                l.Add(r);
            }

            return l.ToArray();
        }

        private int MaxValueIndex(IVolume qVal)
        {
            var max = double.MinValue;
            var maxI = 0;

            var i = 0;
            foreach (var r in qVal)
            {
                if (r > max)
                {
                    max = r;
                    maxI = i;
                }
                i++;
            }

            return maxI;
        }

        private double MaxValue(IVolume qVal)
        {
            var max = double.MinValue;
            
            foreach (var r in qVal)
            {
                if (r > max)
                {
                    max = r;
                }
            }

            return max;
        }
    }
}
