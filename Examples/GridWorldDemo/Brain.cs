using System;
using ConvNetSharp;
using ConvNetSharp.Layers;
using ConvNetSharp.Training;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Based on: http://outlace.com/Reinforcement-Learning-Part-3/
/// </summary>
namespace GridWorldDemo
{
    public class Brain
    {
        public DateTime CreatedDate;
        public TimeSpan TrainingTime;
        public ulong TotalTrainingGames;
        public ulong TotalTrainingMoves;
        public double TotalLoss;
        public double LastLoss;
        public int NumInputs;
        public int NumActions;

        public GridWorld World;
        public string NetJson;

        [JsonIgnore]
        public Net Net;
        
        private SgdTrainer _trainer;

        public object Utils { get; private set; }

        public Brain(int numInputs, int numActions)
        {
            CreatedDate = DateTime.Now;
            TrainingTime = new TimeSpan();

            NumInputs = numInputs;
            NumActions = numActions;

            // network
            var layer1N = (numInputs + numActions)/2;
            Net = new Net();
            Net.AddLayer(new InputLayer(1, 1, numInputs));
            Net.AddLayer(new FullyConnLayer(layer1N));
            Net.AddLayer(new ReluLayer());
            Net.AddLayer(new FullyConnLayer(numActions));
            Net.AddLayer(new RegressionLayer());
            
            World = GridWorld.StandardState();
        }

        public Volume GetInputs()
        {
            // Load the world into an input volume

            var i = 0;
            var j = 0;
            var k = 0;
            var input = new Volume(1, 1, NumInputs);

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

        public int GetNextAction()
        {
            var qVal = Net.Forward(GetInputs());
            return MaxValueIndex(qVal);
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

        public void Train(int numGames, float initialRandomChance)
        {
            var gamma = 0.9f;
            _trainer = new SgdTrainer(Net) { LearningRate = 0.01, Momentum = 0.0, BatchSize = 1, L2Decay = 0.001 };
            var startTime = DateTime.Now;
            
            for (var i = 0; i < numGames; i++)
            {
                World = GridWorld.StandardState();

                double updatedReward;
                var gameRunning = true;
                var gameMoves = 0;
                while (gameRunning)
                {
                    //# We are in state S
                    //# Let's run our Q function on S to get Q values for all possible actions
                    var state = GetInputs();
                    var qVal = Net.Forward(state);
                    var action = 0;

                    if (Util.Rnd.NextDouble() < initialRandomChance)
                    {
                        //# Choose random action
                        action = Util.Rnd.Next(NumActions);
                    }
                    else
                    {
                        //# Choose best action from Q(s,a) values
                        action = MaxValueIndex(qVal);
                    }

                    //# Take action, observe new state S'
                    World.MovePlayer(action);
                    gameMoves++;
                    TotalTrainingMoves++;
                    var newState = GetInputs();

                    //# Observe reward
                    var reward = World.GetReward();
                    gameRunning = !World.GameOver();

                    //# Get max_Q(S',a)
                    var newQ = Net.Forward(newState);
                    var y = GetValues(newQ);
                    var maxQ = MaxValue(newQ);

                    if (gameRunning)
                    {
                        //# Non-terminal state
                        updatedReward = (reward + (gamma * maxQ));
                    }
                    else
                    {
                        //# Terminal state
                        updatedReward = reward;
                        TotalTrainingGames++;
                        Console.WriteLine($"Game: {TotalTrainingGames}. Moves: {gameMoves}. {(reward == 10 ? "WIN!" : "")}");
                    }

                    //# Target output
                    y[action] = updatedReward;

                    //# Feedback what the score would be for this action
                    _trainer.Train(state, y);
                    TotalLoss += _trainer.Loss;
                }

                //# Slowly reduce the chance of choosing a random action
                if (initialRandomChance > 0.05f) initialRandomChance -= (1f / numGames);
            }
            var duration = (DateTime.Now - startTime);

            LastLoss = _trainer.Loss;
            TrainingTime += duration;

            Console.WriteLine($"Avg loss: {TotalLoss / TotalTrainingMoves}. Last: {LastLoss}");
            Console.WriteLine($"Training duration: {duration}. Total: {TrainingTime}");
        }

        public void TrainWithExperienceReplay(int numGames, int batchSize, float initialRandomChance, bool degradeRandomChance = true, string saveToFile = null)
        {
            var gamma = 0.975f;
            var buffer = batchSize * 2;
            var h = 0;

            //# Stores tuples of (S, A, R, S')
            var replay = new List<object[]>();

            _trainer = new SgdTrainer(Net) { LearningRate = 0.01, Momentum = 0.0, BatchSize = batchSize, L2Decay = 0.001 };

            var startTime = DateTime.Now;
            var batches = 0;

            for (var i = 0; i < numGames; i++)
            {
                World = GridWorld.RandomPlayerState();
                var gameMoves = 0;

                double updatedReward;
                var gameRunning = true;
                while (gameRunning)
                {
                    //# We are in state S
                    //# Let's run our Q function on S to get Q values for all possible actions
                    var state = GetInputs();
                    var qVal = Net.Forward(state);
                    var action = 0;

                    if (Util.Rnd.NextDouble() < initialRandomChance)
                    {
                        //# Choose random action
                        action = Util.Rnd.Next(NumActions);
                    }
                    else
                    {
                        //# Choose best action from Q(s,a) values
                        action = MaxValueIndex(qVal);
                    }

                    //# Take action, observe new state S'
                    World.MovePlayer(action);
                    gameMoves++;
                    TotalTrainingMoves++;
                    var newState = GetInputs();
                    
                    //# Observe reward, limit turns
                    var reward = World.GetReward();
                    gameRunning = !World.GameOver();

                    //# Experience replay storage
                    if (replay.Count < buffer)
                    {
                        replay.Add(new[] { state, (object)action, (object)reward, newState });
                    }
                    else
                    {
                        h = (h < buffer - 1) ? h + 1 : 0;
                        replay[h] = new[] { state, (object)action, (object)reward, newState };
                        batches++;
                        var batchInputValues = new Volume[batchSize];
                        var batchOutputValues = new List<double>();

                        //# Randomly sample our experience replay memory
                        for (var b = 0; b < batchSize; b++)
                        {
                            var memory = replay[Util.Rnd.Next(buffer)];
                            var oldState = (Volume)memory[0];
                            var oldAction = (int)memory[1];
                            var oldReward = (int)memory[2];
                            var oldNewState = (Volume)memory[3];

                            //# Get max_Q(S',a)
                            var newQ = Net.Forward(oldNewState);
                            var y = GetValues(newQ);
                            var maxQ = MaxValue(newQ);

                            if (oldReward == GridWorld.ProgressScore)
                            {
                                //# Non-terminal state
                                updatedReward = (oldReward + (gamma * maxQ));
                            }
                            else
                            {
                                //# Terminal state
                                updatedReward = oldReward;
                            }

                            //# Target output
                            y[action] = updatedReward;

                            //# Store batched states
                            batchInputValues[b] = oldState;
                            batchOutputValues.AddRange(y);
                        }
                        Console.Write(".");

                        //# Train in batches with multiple scores and actions
                        _trainer.Train(batchOutputValues.ToArray(), batchInputValues);
                        TotalLoss += _trainer.Loss;
                    }
                }
                Console.WriteLine($"{(World.GetReward() == GridWorld.WinScore ? " WON!" : string.Empty)}");
                Console.Write($"Game: {i + 1}");
                TotalTrainingGames++;

                // Save every 10 games...
                if (!string.IsNullOrEmpty(saveToFile) && (i % 10 == 0))
                    Util.SaveBrainToFile(this, saveToFile);

                //# Optinoally: slowly reduce the chance of choosing a random action
                if (degradeRandomChance && initialRandomChance > 0.05f) initialRandomChance -= (1f / numGames);
            }
            var duration = (DateTime.Now - startTime);

            LastLoss = _trainer.Loss;
            TrainingTime += duration;

            if (!string.IsNullOrEmpty(saveToFile)) Util.SaveBrainToFile(this, saveToFile);

            Console.WriteLine($"\nAvg loss: {TotalLoss / TotalTrainingMoves}. Last: {LastLoss}");
            Console.WriteLine($"Training duration: {duration}. Total: {TrainingTime}");
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
