using System;
using ConvNetSharp;
using ConvNetSharp.Layers;
using ConvNetSharp.Training;
using System.Text;

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
            Net.AddLayer(new FullyConnLayer(164));
            Net.AddLayer(new ReluLayer());
            Net.AddLayer(new FullyConnLayer(150));
            Net.AddLayer(new ReluLayer());
            Net.AddLayer(new FullyConnLayer(numActions));
            Net.AddLayer(new RegressionLayer());

            Trainer = new SgdTrainer(Net) { LearningRate = 0.001, Momentum = 0.0, BatchSize = 64, L2Decay = 0.01 };

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
            sb.Append(")");

            return sb.ToString();
        }

        public void Train()
        {
            var avLoss = 0.0;
            var lastLoss = 0.0;
            var epochs = 1000;
            var gamma = 0.9f;
            var epsilon = 1f;
            var gameMoves = 0;
            var totalMoves = 0;
            var totalGames = 0;

            for (var i = 0; i < epochs; i++)
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

                    //# Feedback the score of the new state
                    Trainer.Train(newState, updatedReward);
                    avLoss += Trainer.Loss;
                }

                if (epsilon > 0.1f) epsilon -= (1f / epochs);
            }
            lastLoss = Trainer.Loss;
            avLoss /= totalMoves * epochs;
            Console.WriteLine($"Avg. Loss: {avLoss}. Last: {lastLoss}");
        }

        private int MaxValueIndex(IVolume qVal)
        {
            var max = double.MinValue;
            var maxI = 0;

            var i = 0;
            foreach (var r in qVal)
            {
                i++;
                if (r > max)
                {
                    max = r;
                    maxI = i;
                }
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
