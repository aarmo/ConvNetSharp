using System;
using System.Text;

namespace GridWorldDemo
{
    public class GridWorld
    {
        public const int GridSize = 4;
        public const int GridDepth = 4;

        public const int WallLayer = 0;
        public const int PitLayer = 1;
        public const int GoalLayer = 2;
        public const int PlayerLayer = 3;

        public int[,,] WorldState;
        public Location PlayerLocation;

        public static GridWorld StandardState()
        {
            var w = new GridWorld();
            w.WorldState = new int[GridSize, GridSize, GridDepth];
            w.PlayerLocation = new Location(0, 1);
            w.WorldState[0, 1, PlayerLayer] = 1;
            w.WorldState[2, 2, WallLayer] = 1;
            w.WorldState[1, 1, PitLayer] = 1;
            w.WorldState[3, 3, GoalLayer] = 1;            

            return w;
        }

        public static GridWorld RandomPlayerState()
        {
            var w = new GridWorld();
            w.WorldState = new int[GridSize, GridSize, GridDepth];

            w.WorldState[2, 2, WallLayer] = 1;
            w.WorldState[1, 1, PitLayer] = 1;
            w.WorldState[3, 3, GoalLayer] = 1;

            Location p;
            bool invalid = false;
            do
            {
                p = Util.RandLoc(0, GridWorld.GridSize);
                invalid = (w.WorldState[p.X, p.Y, WallLayer] != 0 || w.WorldState[p.X, p.Y, PitLayer] != 0 || w.WorldState[p.X, p.Y, GoalLayer] != 0);
            } while (invalid);

            w.PlayerLocation = p;
            w.WorldState[p.X, p.Y, PlayerLayer] = 1;

            return w;
        }

        public static GridWorld RandomState()
        {
            var w = new GridWorld();
            w.WorldState = new int[GridSize, GridSize, GridDepth];

            Location wall;
            Location pit;
            Location goal;
            Location player;

            var invalid = false;

            // Keep trying to get 4 unique random positions...
            do
            {
                wall = Util.RandLoc(0, GridWorld.GridSize);
                pit = Util.RandLoc(0, GridWorld.GridSize);
                goal = Util.RandLoc(0, GridWorld.GridSize);
                player = Util.RandLoc(0, GridWorld.GridSize);

                invalid = (wall == pit || wall == goal || wall == player
                    || pit == goal || pit == player
                    || goal == player);
            } while (invalid);

            w.WorldState[wall.X, wall.Y, WallLayer] = 1;
            w.WorldState[pit.X, pit.Y, PitLayer] = 1;
            w.WorldState[goal.X, goal.Y, GoalLayer] = 1;
            w.PlayerLocation = player;
            w.WorldState[player.X, player.Y, PlayerLayer] = 1;

            return w;
        }

        public void MovePlayer(int action)
        {
            //# up (row - 1)
            if (action == 0 && PlayerLocation.Y > 0)
            {
                var p = new Location(PlayerLocation.X, PlayerLocation.Y - 1);
                if (WorldState[p.X, p.Y, WallLayer] != 1)
                {
                    UpdatePlayerLocation(p);
                }
            }
            // #down (row + 1)
            else if (action == 1 && PlayerLocation.Y < GridSize - 1)
            {
                var p = new Location(PlayerLocation.X, PlayerLocation.Y + 1);
                if (WorldState[p.X, p.Y, WallLayer] != 1)
                {
                    UpdatePlayerLocation(p);
                }
            }
            // #left (column - 1)
            else if (action == 2 && PlayerLocation.X > 0)
            {
                var p = new Location(PlayerLocation.X - 1, PlayerLocation.Y);
                if (WorldState[p.X, p.Y, WallLayer] != 1)
                {
                    UpdatePlayerLocation(p);
                }
            }
            // #right (column + 1)
            else if (action == 3 && PlayerLocation.X < GridSize - 1)
            {
                var p = new Location(PlayerLocation.X + 1, PlayerLocation.Y);
                if (WorldState[p.X, p.Y, WallLayer] != 1)
                {
                    UpdatePlayerLocation(p);
                }
            }
        }

        private void UpdatePlayerLocation(Location p)
        {
            WorldState[PlayerLocation.X, PlayerLocation.Y, PlayerLayer] = 0;
            PlayerLocation = p;
            WorldState[p.X, p.Y, PlayerLayer] = 1;
        }

        public int GetReward()
        {
            if (WorldState[PlayerLocation.X, PlayerLocation.Y, PitLayer] == 1)
                return -10;

            if (WorldState[PlayerLocation.X, PlayerLocation.Y, GoalLayer] == 1)
                return 10;

            return -1;
        }

        public string DisplayGrid()
        {
            var sb = new StringBuilder();

            sb.AppendLine(" --- --- --- --- ");
            for (var y = 0; y < GridSize; y++)
            {
                for (var x = 0;x < GridSize; x++)
                {
                    sb.Append(" ");
                    if (PlayerLocation.X == x && PlayerLocation.Y == y)
                    {
                        sb.Append(" O ");
                    }
                    else if (WorldState[x, y, WallLayer] == 1)
                    {
                        sb.Append("---");
                    }
                    else if (WorldState[x, y, PitLayer] == 1)
                    {
                        sb.Append(" ! ");
                    }
                    else if (WorldState[x, y, GoalLayer] == 1)
                    {
                        sb.Append(" $ ");
                    }
                    else
                    {
                        sb.Append(" . ");
                    }
                }
                sb.AppendLine("");
            }
            sb.AppendLine(" --- --- --- --- ");

            return sb.ToString();
        }
    }

}
