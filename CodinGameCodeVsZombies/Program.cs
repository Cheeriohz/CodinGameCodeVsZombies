using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Save humans, destroy zombies!
 **/
class Player
{
    static Dictionary<int, Zed> Zeds = new Dictionary<int, Zed>();
    static Dictionary<int, Human> Humans = new Dictionary<int, Human>();
    static void Main(string[] args)
    {
        bool initialized = false;
        int[] processOrder = Array.Empty<int>();
        Simulation sim = new Simulation();
        int pIndex = 0;
        string[] inputs;

        // game loop
        while(true)
        {
            List<(int id, HData data)> humans = new List<(int hId, HData hData)>();
            List<(int id, ZData data)> zeds = new List<(int hId, ZData zData)>();

            inputs = Console.ReadLine().Split(' ');
            humans.Add((Constants.AshId, new HData 
            { 
                X = int.Parse(inputs[0]), 
                Y = int.Parse(inputs[1]) 
            }));

            int humanCount = int.Parse(Console.ReadLine());            
            for(int i = 0; i < humanCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                humans.Add((int.Parse(inputs[0]), new HData 
                { 
                    X = int.Parse(inputs[1]), 
                    Y = int.Parse(inputs[2]) 
                }));
            }
            int zombieCount = int.Parse(Console.ReadLine());
            for(int i = 0; i < zombieCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                zeds.Add((int.Parse(inputs[0]), new ZData
                {
                    X = int.Parse(inputs[1]),
                    Y = int.Parse(inputs[2]),
                    ZXNext = int.Parse(inputs[3]),
                    ZYNext = int.Parse(inputs[4])
                }));
            }

            if(!initialized)
            {
                processOrder = zeds.Select(z => z.id).ToArray();
                sim = new Simulation(zeds, humans, processOrder, true);
                //int expectedScore = new Simulation(zeds, humans, processOrder).RunSimulation();
                //Console.Error.WriteLine($"Expected Score: {expectedScore}");
                initialized = true;
            }

            sim.RunNextSimRound();

            Zeds = zeds.ToDictionary(kvp => kvp.id, kvp => new Zed { Data = kvp.data });
            Humans = humans.ToDictionary(kvp => kvp.id, kvp => new Human { Data = kvp.data });

            bool moveDetermined = false;
            while(!moveDetermined)
            {
                int zId = processOrder[pIndex];
                if(Zeds.ContainsKey(zId))
                {
                    ZedUtil.MoveAsh(Zeds[zId].ZXNext, Zeds[zId].ZYNext, Humans[Constants.AshId]);
                    Console.WriteLine($"{Humans[Constants.AshId].X} {Humans[Constants.AshId].Y}");
                    moveDetermined = true;
                }
                else
                {
                    pIndex++;
                }
            }
        }
    }
}



public class Simulation
{
    Dictionary<int, Zed> Zeds;
    Dictionary<int, Human> Humans;
    int round = 1;
    int score = 0;
    int pIndex = 0;
    int[] ProcessOrder;
    bool shouldLog = false;

    public Simulation() 
    {
        this.Zeds = new Dictionary<int, Zed>();
        this.Humans = new Dictionary<int, Human>();
        this.ProcessOrder = Array.Empty<int>();
    }
    public Simulation(IEnumerable<(int id, ZData data)> zeds, IEnumerable<(int id, HData data)> humans, IEnumerable<int> processOrder, bool shouldLog = false)
    {
        this.Zeds = zeds.ToDictionary(kvp => kvp.id, kvp => new Zed { Data = kvp.data });
        this.Humans = humans.ToDictionary(kvp => kvp.id, kvp => new Human { Data = kvp.data });
        this.ProcessOrder = processOrder.ToArray();
        this.shouldLog = shouldLog;
    }

    public int RunSimulation()
    {
        while(this.Zeds.Any())
        {
            for(this.pIndex = 0; this.pIndex < this.ProcessOrder.Length; this.pIndex++)
            {
                int zId = this.ProcessOrder[this.pIndex];
                if(this.Zeds.ContainsKey(zId))
                {
                    ZedUtil.MoveZombies(this.Zeds, this.Humans);
                    ZedUtil.MoveAsh(this.Zeds[zId].X, this.Zeds[zId].Y, this.Humans[Constants.AshId]);
                    this.score += ZedUtil.ProcessRoundDeaths(this.Zeds, this.Humans, this.shouldLog);
                    if(this.shouldLog) this.LogState();
                    this.round++;
                }
            }
        }
        return this.score;
    }

    public void RunNextSimRound()
    {
        while(this.Zeds.Any())
        {
            for(this.pIndex = this.pIndex; this.pIndex < this.ProcessOrder.Length; this.pIndex++)
            {
                int zId = this.ProcessOrder[this.pIndex];
                if(this.Zeds.ContainsKey(zId))
                {
                    ZedUtil.MoveZombies(this.Zeds, this.Humans);
                    ZedUtil.MoveAsh(this.Zeds[zId].X, this.Zeds[zId].Y, this.Humans[Constants.AshId]);
                    this.score += ZedUtil.ProcessRoundDeaths(this.Zeds, this.Humans, this.shouldLog);
                    if(this.shouldLog) this.LogState();
                    this.round++;
                    return;
                }
            }
        }
    }

    public void LogState()
    {
        Console.Error.WriteLine($"Logging Round {this.round}");
        foreach(KeyValuePair<int, Zed> zed in this.Zeds)
        {
            Console.Error.WriteLine($"Zed {zed.Key} ({zed.Value.X},{zed.Value.Y})");
        }

        foreach(KeyValuePair<int, Human> human in this.Humans)
        {
            Console.Error.WriteLine($"Human {human.Key} ({human.Value.X},{human.Value.Y})");
        }
    }
}

public class Human
{
    public HData Data { get; set; }

    public int X  => this.Data.X;
    public int Y => this.Data.Y;

}

public struct HData
{
    public int X;
    public int Y;
}

public class Zed
{ 
    public ZData Data { get; set; }
    public int X => this.Data.X;
    public int Y => this.Data.Y;

    public int ZXNext => this.Data.ZXNext;
    public int ZYNext => this.Data.ZYNext;
}


public struct ZData
{
    public int X;
    public int Y;
    public int ZXNext;
    public int ZYNext;
}


public static class Constants
{
    public const int AshId = -5;
}


public static class ZedUtil
{ 
    public static void MoveZombies(Dictionary<int, Zed> zeds, Dictionary<int, Human> humans)
    {
        foreach(Zed zed in zeds.Values)
        {
            (int nZX, int nZY) = DetermineAndMoveToClosestHuman(zed.X, zed.Y, humans.Values.Select(h => h.Data));
            zed.Data = new ZData { X = nZX, Y = nZY };
        }
    }

    public static void MoveAsh(int zX, int zY, Human ash)
    {
        int distanceToZed = Utility.CalculateDistanceAsInt(ash.X, ash.Y, zX, zY);
        if(distanceToZed <= 1000)
        {
            ash.Data = new HData { X = zX, Y = zY };
        }
        else
        {
            (int nX, int nY) = Utility.ProjectDistanceFromPointToPoint(ash.X, ash.Y, zX, zY, 1000);
            ash.Data = new HData { X = nX, Y = nY };
        }
    }

    public static (int X, int Y) DetermineAndMoveToClosestHuman(int zX, int zY, IEnumerable<HData> humans)
    {
        return humans.Select(h => (h.X, h.Y, Utility.CalculateDistanceAsInt(zX, zY, h.X, h.Y)))
                     .OrderBy(((int X, int Y, int D) tr) => tr.D)
                     .Select(((int X, int Y, int D) tr) => tr.D <= 400 
                        ? (tr.X, tr.Y) 
                        : Utility.ProjectDistanceFromPointToPoint(zX, zY, tr.X, tr.Y, 400)).First();
    }

    public static int ProcessRoundDeaths(Dictionary<int, Zed> zeds, Dictionary<int, Human> humans, bool shouldLog)
    {
        int score = 0;
        Fibonacci fib = new Fibonacci();

        Human ash = humans[Constants.AshId];

        foreach(KeyValuePair<int, Zed> zed in zeds)
        {
            if(Utility.CalculateDistanceAsInt(zed.Value.X, zed.Value.Y, ash.X, ash.Y ) <= 2000)
            {
                int killValue = ZedUtil.ScoreZedKill(fib, humans.Count - 1);
                score += killValue;
                if(shouldLog) Console.Error.WriteLine($"--SIM-- Zombie {zed.Key} at ({zed.Value.X},{zed.Value.Y} worth {killValue}) ");
                zeds.Remove(zed.Key);
            }
        }

        List<int> deadHumans = new List<int>();

        foreach(KeyValuePair<int, Human> human in humans)
        {
            foreach(Zed zed in zeds.Values)
            {
                if(zed.X == human.Value.X && zed.Y == human.Value.Y)
                {
                    humans.Remove(human.Key);
                    if(shouldLog) Console.Error.WriteLine($"--SIM-- Human {human.Key} at ({human.Value.X},{human.Value.Y}");
                }
            }
        }


        return score;
    }

    public static int ScoreZedKill(Fibonacci fib, int humansAlive)
    {
        return (int)Math.Floor(Math.Pow(humansAlive, 2) * 10 * fib.Next());
    }

}

public class Fibonacci
{
    public Fibonacci() { this.a = 0; this.b = 1; }

    private int a;
    private int b;

    public int Next()
    {
        int temp = this.a;
        this.a = this.b;
        this.b = temp + this.b;

        return this.a;
    }
}


public static class Utility
{
    public static Double CalculateDistance(int x, int y, int x2, int y2) => Math.Sqrt(Math.Pow(x2 - x, 2) + Math.Pow(y2 - y, 2));
    public static int CalculateDistanceAsInt(int x, int y, int x2, int y2) => (int)Math.Floor(Utility.CalculateDistance(x, y, x2, y2));
    public static int CalculateMinimum180AngleBetweenTwo360Angles(int firstAngle, int secondAngle) => (secondAngle - firstAngle).NormalizeAngleTo180();
    public static bool IsCollisionImminent(int jx, int jy, int jvx, int jvy, int kx, int ky, int kvx, int kvy, int collisionDistance = 1000)
    {
        int jXPost = jx + jvx;
        int jYPost = jy + jvy;
        int kXPost = kx + kvx;
        int kYPost = ky + kvy;

        int distance = CalculateDistanceAsInt(jXPost, jYPost, kXPost, kYPost);

        //Console.Error.WriteLine($"({jx},{jy}) + ({jvx},{jvy}) => ({jXPost},{jYPost})");
        //Console.Error.WriteLine($"({kx},{ky}) + ({kvx},{kvy}) => ({kXPost},{kYPost})");
        //Console.Error.WriteLine($"Distance {distance} collision {(distance <= 850 ? "Imminent" : "Not Imminent")}");

        return distance <= collisionDistance;
    }
    public static int NormalizeAngleTo360(this int angle) => (angle + 360) % 360;
    public static int NormalizeAngleTo180(this int angle)
    {
        int threeSixtyAngle = NormalizeAngleTo360(angle);
        return threeSixtyAngle > 180 ? Math.Abs(threeSixtyAngle - 360) : threeSixtyAngle;
    }
    public static (int x, int y) GetShortVectorForRotateFromDegreesAngle(int x, int y, int angle)
    {
        double xAbsorb = Math.Cos(angle.ToRadian());
        double yAbsorb = Math.Sin(angle.ToRadian());

        return (x + (int)Math.Floor(100 * xAbsorb), y + (int)Math.Floor(100 * yAbsorb));

    }
    public static int GetSignedComplementOfAngle(int angle) => angle < 0 ? (-180 - angle) : (180 - angle);
    public static int ToDegrees(this double val) => (int)Math.Floor(val * (180 / Math.PI));
    public static double ToRadian(this int val) => (double)val / 180 * Math.PI;
    public static int DetermineRotationalAngleToPoint(int x, int y, int targetX, int targetY, int currentFacingAngle) => Math.Abs(currentFacingAngle - DeterminePositionAngleToPoint(x, y, targetX, targetY).NormalizeAngleTo360()).NormalizeAngleTo180();
    public static int DeterminePositionAngleToPoint(int x, int y, int targetX, int targetY) => Math.Atan2(targetY - y, targetX - x).ToDegrees();
    public static int BallparkTraversalValue(int x, int y, int tx, int ty)
    {
        return CalculateDistanceAsInt(x, y, tx, ty);
    }
    public static (int x, int y) ProjectDistanceFromPointToPoint(int x, int y, int tX, int tY, double skewLength)
    {
        double angleToCheckPoint = Math.Atan2(tY - y, tX - x);

        double projectedX = x + (skewLength * Math.Cos(angleToCheckPoint));
        double projectedY = y + (skewLength * Math.Sin(angleToCheckPoint));

        return ((int)Math.Floor(projectedX), (int)Math.Floor(projectedY));
    }
}