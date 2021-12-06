using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

/**
 * Save humans, destroy zombies!
 **/
class Player
{
    static Dictionary<int, Zed> Zeds = new Dictionary<int, Zed>();
    static Dictionary<int, Human> Humans = new Dictionary<int, Human>();
    static void Main(string[] args)
    {
        
        string[] inputs;
        int[] processOrder = Array.Empty<int>();
        int pIndex = 0;
        int currentFinalScore = 0;
        int currentScore = 0;
        Simulation currentSim = new Simulation();

        // game loop
        while(true)
        {
            List<(int id, HData data)> humans = new List<(int hId, HData hData)>();
            List<(int id, ZData data)> zeds = new List<(int hId, ZData zData)>();

            inputs = Console.ReadLine().Split(' ');

            Stopwatch inputReadAndBaseBuild = Stopwatch.StartNew();

            humans.Add((Constants.AshId, new HData 
            { 
                X = int.Parse(inputs[0]), 
                Y = int.Parse(inputs[1]) 
            }));

            int humanCount = int.Parse(Console.ReadLine());            
            for(int i = 0; i < humanCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                humans.Add(((int.Parse(inputs[0]) * -1) - 1, new HData 
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

            inputReadAndBaseBuild.Stop();
            long currentProcessTime = inputReadAndBaseBuild.ElapsedTicks;
            Console.Error.WriteLine($"Input read and base build took {inputReadAndBaseBuild.ElapsedMilliseconds}ms");
            Console.Error.WriteLine($"Current Score is believed to be {currentScore} | Expect Final {currentFinalScore}");

            CVZEnvironmentData environmentData = new CVZEnvironmentData(zeds, humans);
            Population population = new Population(environmentData);
            (int[] testOrder, int testFinalScore) = population.GetFittestSubject(20, 6, 0.30, currentProcessTime, 90 - currentSim.round, false);

            //if(testFinalScore >= currentFinalScore - currentScore)
            if(processOrder == Array.Empty<int>())
            {
                processOrder = testOrder;
                currentFinalScore = testFinalScore + currentScore;

                currentSim = new Simulation(environmentData, processOrder, true);
                currentSim.OverrideScore(currentScore);
                new Simulation(environmentData, processOrder, true).RunSimulation();
                pIndex = 0;
            }
            
            currentSim.RunNextSimRound();
            currentScore = currentSim.Score;

            Console.Error.Write($"Next round score is expected to be {currentScore} | Expect Final {currentFinalScore} | Genome [");
            foreach(int v in processOrder)
            {
                Console.Error.Write(v.ToString());
            }
            Console.Error.WriteLine("]");

            Zeds = zeds.ToDictionary(kvp => kvp.id, kvp => new Zed { Data = kvp.data });
            Humans = humans.ToDictionary(kvp => kvp.id, kvp => new Human { Data = kvp.data });

            bool moveDetermined = false;
            while(!moveDetermined)
            {
                int tId = processOrder[pIndex];
                
                if(tId >= 0 && Zeds.ContainsKey(tId))
                {
                    ZedUtil.MoveAsh(Zeds[tId].ZXNext, Zeds[tId].ZYNext, Humans[Constants.AshId], false);
                    Console.WriteLine($"{Humans[Constants.AshId].X} {Humans[Constants.AshId].Y}");
                    moveDetermined = true;
                }    
                else if(Humans.ContainsKey(tId))
                {
                    ZedUtil.MoveAsh(Humans[tId].X, Humans[tId].Y, Humans[Constants.AshId], true);
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

public class Population
{
    private IEnvironmentData<IEnumerable<int>> EnvironmentData { get; set; }

    public Simulation[] CurrentGeneration { get; set; }

    public Population(IEnvironmentData<IEnumerable<int>> environmentData)
    {
        this.EnvironmentData = environmentData;
    }

    public (int[], int) GetFittestSubject(int initialPopulationSize, int survivorConstraintSize, double mutationChance, long currentProcessMs, int fullProcessTimeAllocationInMs, bool shouldLog = false)
    {
        Dictionary<int[], int> fitnessResults = new Dictionary<int[], int>();
        Stopwatch initialPopTime = new Stopwatch();
        initialPopTime.Start();

        this.CurrentGeneration 
            = this.CreateInitialPopulation(initialPopulationSize, new Random((int)DateTime.Now.Ticks))
                  .DetermineFitness(fitnessResults)
                  .DetermineSurvivors(survivorConstraintSize).ToArray();

        initialPopTime.Stop();
        currentProcessMs += initialPopTime.ElapsedTicks;
        Console.Error.WriteLine($"Initial population process completed at {currentProcessMs / 1000000}ms");
        int generationsProcessed = 0;

        //while(++generationsProcessed < numberOfGenerations)
        while(currentProcessMs < (fullProcessTimeAllocationInMs * 1000000))
        {
            generationsProcessed++;
            if(shouldLog) Console.Error.WriteLine($"Processing Generation {generationsProcessed}");
            Stopwatch generationTime = new Stopwatch();
            generationTime.Start();

            this.CurrentGeneration 
                = this.CurrentGeneration.GenerateOffSpring(this.EnvironmentData, mutationChance)
                      .DetermineFitness(fitnessResults)
                      .DetermineSurvivors(survivorConstraintSize).ToArray();

            generationTime.Stop();

            if(shouldLog) Console.Error.WriteLine($"Processing of Generation {generationsProcessed} took {generationTime.ElapsedTicks / 1000000}ms");
            currentProcessMs += generationTime.ElapsedTicks;
        }

        
        Console.Error.WriteLine($"Full processing of population evolution took {currentProcessMs / 1000000 }ms. Processed {generationsProcessed} generations");

        if(shouldLog)
        {
            Console.Error.WriteLine($"Final Fitness report for population");
            foreach(KeyValuePair<int[], int> kvp in fitnessResults)
            {
                if(kvp.Value != 0)
                {
                    foreach(int v in kvp.Key)
                    {
                        Console.Error.Write(v.ToString());
                    }

                    Console.Error.WriteLine($" results in a score of {kvp.Value}");
                }
            }
        }

        return fitnessResults.OrderBy(kvp => -kvp.Value).Select(kvp => (kvp.Key, kvp.Value)).First();
    }

    private IEnumerable<Simulation> CreateInitialPopulation(int populationSize, Random random, bool shouldLog = false)
    {
        foreach(IEnumerable<int> genome in this.EnvironmentData.BuildBasePopulationGenome(random, populationSize))
        {
            yield return new Simulation(this.EnvironmentData, genome, shouldLog);
        }
    }
}

public static class PopulationExtensions
{
    public static IEnumerable<int> Crossover(this ICollection<int> fitterGenome, ICollection<int> partnerGenome, bool shouldLog = false) => DavisOrderCrossover(fitterGenome, partnerGenome, shouldLog);

    private static IEnumerable<int> DavisOrderCrossover(this ICollection<int> fitterGenome, ICollection<int> partnerGenome, bool shouldLog = false)
    {
        if(shouldLog) Console.Error.WriteLine("Determining genome crossover");
        Random random = new Random((int)DateTime.Now.Ticks);

        int?[] result = new int?[fitterGenome.Count];
        HashSet<int> alreadyInSequence = new HashSet<int>();
        int stockSize = random.Next(0, fitterGenome.Count);
        int stockOffset = random.Next(0, fitterGenome.Count - stockSize);

        for(int i = stockOffset; i < stockOffset + stockSize; i++)
        {
            int allele = fitterGenome.ElementAt(i);
            alreadyInSequence.Add(allele);
            result[i] = allele;
        }

        int[] remainingGenomeFromPartner = partnerGenome.Where(allele => !alreadyInSequence.Contains(allele)).ToArray();

        int rInd = 0; 
        int pInd = 0;

        while(rInd < fitterGenome.Count && pInd < remainingGenomeFromPartner.Length)
        {
            if(result[rInd] == null)
            {
                result[rInd] = remainingGenomeFromPartner[pInd++];
            }
            rInd++;

            if(shouldLog)
            {
                foreach(int? v in result)
                {
                    Console.Error.Write(v?.ToString() ?? "_");
                }

                Console.Error.WriteLine();
            }
            
        }

        if(shouldLog) Console.Error.WriteLine($"Crossover Complete");
        return result.Cast<int>();
    }

    public static IEnumerable<int> Mutate(this IEnumerable<int> baseGenome, double mutationChance, bool shouldLog = false)
    {
        if(shouldLog) Console.Error.WriteLine("Determining mutation for a genome");
        Random random = new Random((int)DateTime.Now.Ticks);
        if(random.NextDouble() < mutationChance)
        {
            int[] finalGenome = baseGenome.ToArray();
            int mutateCount = 1;
            do
            {
                if(shouldLog) Console.Error.WriteLine("Mutating Genome");
                int swapA = random.Next(0, finalGenome.Length);
                int swapB = random.Next(0, finalGenome.Length);

                int temp = finalGenome[swapA];
                finalGenome[swapA] = finalGenome[swapB];
                finalGenome[swapB] = temp;
            } while(random.NextDouble() < mutationChance && mutateCount++ < finalGenome.Length / 2);

            if(shouldLog) Console.Error.WriteLine("Mutation Complete");
            return finalGenome;
        }
        else
        {
            if(shouldLog) Console.Error.WriteLine("Mutation Complete");
            return baseGenome;
        }

    }

    public static IEnumerable<Simulation> GenerateOffSpring(this ICollection<Simulation> currentPop, IEnvironmentData<IEnumerable<int>> environmentData, double mutationChance, bool shouldLog = false)
    {
        Stopwatch sw = Stopwatch.StartNew();
        if(shouldLog) Console.Error.WriteLine("Determining offspring for a generation");
        List<Simulation> aGroup = new List<Simulation>();
        List<Simulation> bGroup = new List<Simulation>();

        bool inAGroup = true;

        for(int i = 0; i < currentPop.Count; i++)
        {
            Simulation selectedSim = currentPop.ElementAt(i);
            yield return selectedSim;
            (inAGroup ? aGroup : bGroup).Add(selectedSim);
            inAGroup = !inAGroup;
        }

        if(aGroup.Count != bGroup.Count)
        {
            aGroup.RemoveAt(aGroup.Count - 1);
        }

        Random random = new Random((int)DateTime.Now.Ticks);
        int childCount = random.Next(2, aGroup.Count);
        bGroup = bGroup.OrderBy(_ => random.Next()).ToList();
        
        if(shouldLog) Console.Error.WriteLine($"  Time to first crossover {sw.ElapsedMilliseconds}ms");
        
        for(int i = 0; i < childCount; i++)
        {
            sw.Restart();
            yield return new Simulation(environmentData, aGroup[i].ProcessOrder.Crossover(bGroup[i].ProcessOrder, shouldLog).Mutate(mutationChance, shouldLog), shouldLog);
            if(shouldLog) Console.Error.WriteLine($"  Time for breedResult {sw.ElapsedMilliseconds}ms");
        }
    }

    public static IEnumerable<Simulation> DetermineFitness(this IEnumerable<Simulation> competitorsToEvaluate, Dictionary<int[], int> allResults,  bool shouldLog = false)
    {
        if(shouldLog) Console.Error.WriteLine("Determining a fitness for a generation");
        foreach(Simulation competitor in competitorsToEvaluate)
        {
            if(allResults.TryGetValue(competitor.ProcessOrder, out int preCalculatedResult))
            {
                if(shouldLog)
                {
                    Console.Error.Write($"Score of {preCalculatedResult} was already in cache for ");
                    foreach(int v in competitor.ProcessOrder)
                    {
                        Console.Error.Write(v.ToString());
                    }
                    Console.Error.WriteLine();
                }
                
                competitor.OverrideScore(preCalculatedResult);
            }
            else
            {
                competitor.RunSimulation();
                allResults[competitor.ProcessOrder] = competitor.Score;
            }
            yield return competitor;
        }
    }

    public static IEnumerable<Simulation> DetermineFitnessIterationLimited(this IEnumerable<Simulation> competitorsToEvaluate, int turnsToProcessTo, bool shouldLog = false)
    {
        if(shouldLog) Console.Error.WriteLine("Determining a fitness iteration limited for a generation");
        foreach(Simulation competitor in competitorsToEvaluate)
        {
            while(competitor.round < turnsToProcessTo && competitor.RunNextSimRound()) { }
            yield return competitor;
        }
    }

    public static IEnumerable<Simulation> DetermineSurvivors(this IEnumerable<Simulation> unselectedPopulation, int survivorConstraintSize, bool shouldLog = false)
    {
        if(shouldLog) Console.Error.WriteLine("Determining survivors of a generation");
        return unselectedPopulation.OrderBy(competitor => -competitor.Score).ThenBy(competitor => -competitor.round).Take(survivorConstraintSize);
    }
}

public interface IGenome<T>
{
    T GetGenome();

    void SetGenome(T genome);
}

public interface IEnvironmentData<T>
{
    object GetEnvironmentItem(string key);

    IEnumerable<T> BuildBasePopulationGenome(Random random, int populationSize);

}

public class CVZEnvironmentData : IEnvironmentData<IEnumerable<int>>
{
    public CVZEnvironmentData(ICollection<(int id, ZData data)> zeds, ICollection<(int id, HData data)> humans)
    {
        this.Zeds = zeds;
        this.Humans = humans;
    }

    public object GetEnvironmentItem(string key) => key switch
    {
        Constants.ZEDS => this.Zeds,
        Constants.HUMANS => this.Humans,
        _ => throw new NotImplementedException()
    };

    public IEnumerable<IEnumerable<int>> BuildBasePopulationGenome(Random random, int populationSize)
    {
        bool gauranteedZedFirstSeedGenerated = false;
        bool gauranteedHumanFirstSeedGenerated = false;
        for(int i = 0; i < populationSize; i++)
        {
            if(!gauranteedZedFirstSeedGenerated)
            {
                yield return
                    this.Zeds.OrderBy(_ => random.Next())
                             .Select(zed => zed.id)
                             .Union(this.Humans.OrderBy(_ => random.Next())
                                               .Where(h => h.id != Constants.AshId)
                                               .Select(h => h.id));
                    
                gauranteedZedFirstSeedGenerated = true;
            }
            else if(!gauranteedHumanFirstSeedGenerated)
            {
                yield return this.Humans.OrderBy(_ => random.Next())
                                .Where(h => h.id != Constants.AshId)
                                .Select(h => h.id)
                                .Union(this.Zeds.OrderBy(_ => random.Next())
                                                .Select(zed => zed.id));
                gauranteedHumanFirstSeedGenerated = true;
            }
            else
            {
                yield return this.Zeds.OrderBy(_ => random.Next())
                             .Select(zed => zed.id)
                             .Union(this.Humans.OrderBy(_ => random.Next())
                                               .Where(h => h.id != Constants.AshId)
                                               .Select(h => h.id))
                             .OrderBy(_ => random.Next());
                
            }
        }
    }

    private ICollection<(int id, ZData data)> Zeds { get; set; }
    private ICollection<(int id, HData data)> Humans { get; set; }



}


public class Simulation : IGenome<int[]>
{
    Dictionary<int, Zed> Zeds;
    Dictionary<int, Human> Humans;
    public int round = 1;

    public int Score => this.Humans.Values.Count == 1 ? 0 : this.score;
    private int score = 0;
    int pIndex = 0;
    public int[] ProcessOrder;
    bool shouldLog = false;

    public Simulation() 
    {
        this.Zeds = new Dictionary<int, Zed>();
        this.Humans = new Dictionary<int, Human>();
        this.ProcessOrder = Array.Empty<int>();
    }
    public Simulation(IEnvironmentData<IEnumerable<int>> environmentData, IEnumerable<int> processOrder, bool shouldLog = false)
    {
        this.Zeds = ((ICollection<(int id, ZData data)>)environmentData.GetEnvironmentItem(Constants.ZEDS)).ToDictionary(kvp => kvp.id, kvp => new Zed { Data = kvp.data });
        this.Humans = ((ICollection<(int id, HData data)>)environmentData.GetEnvironmentItem(Constants.HUMANS)).ToDictionary(kvp => kvp.id, kvp => new Human { Data = kvp.data });
        this.ProcessOrder = processOrder.ToArray();
        this.shouldLog = shouldLog;
    }

    public int RunSimulation()
    {
        this.pIndex = 0;
        while(this.Zeds.Any() && this.pIndex < this.ProcessOrder.Length)
        {
            int tId = this.ProcessOrder[this.pIndex];
            if(tId >= 0 && this.Zeds.ContainsKey(tId))
            {
                ZedUtil.MoveZombies(this.Zeds, this.Humans);
                ZedUtil.MoveAsh(this.Zeds[tId].X, this.Zeds[tId].Y, this.Humans[Constants.AshId], false, this.shouldLog);
                this.score += ZedUtil.ProcessRoundDeaths(this.Zeds, this.Humans, this.shouldLog);
                if(this.shouldLog) this.LogState();
                this.pIndex--;
                this.round++;
            }
            else if(tId < 0 && this.Humans.ContainsKey(tId))
            {
                ZedUtil.MoveZombies(this.Zeds, this.Humans);
                ZedUtil.MoveAsh(this.Humans[tId].X, this.Humans[tId].Y, this.Humans[Constants.AshId], true, this.shouldLog);
                this.score += ZedUtil.ProcessRoundDeaths(this.Zeds, this.Humans, this.shouldLog);
                if(this.shouldLog) this.LogState();
                this.pIndex--;
                this.round++;
            }
            this.pIndex++;

        }
        return this.score;
    }

    public bool RunNextSimRound()
    {
        while(this.Zeds.Any())
        {
            for(this.pIndex = 0; this.pIndex < this.ProcessOrder.Length; this.pIndex++)
            {
                int tId = this.ProcessOrder[this.pIndex];
                if(tId >= 0 && this.Zeds.ContainsKey(tId))
                {
                    ZedUtil.MoveZombies(this.Zeds, this.Humans);
                    ZedUtil.MoveAsh(this.Zeds[tId].X, this.Zeds[tId].Y, this.Humans[Constants.AshId], false, this.shouldLog);
                    this.score += ZedUtil.ProcessRoundDeaths(this.Zeds, this.Humans, this.shouldLog);
                    if(this.shouldLog) this.LogState();
                    this.round++;
                    return true;
                }
                else if(tId < 0 && this.Humans.ContainsKey(tId))
                {
                    ZedUtil.MoveZombies(this.Zeds, this.Humans);
                    ZedUtil.MoveAsh(this.Humans[tId].X, this.Humans[tId].Y, this.Humans[Constants.AshId], true, this.shouldLog);
                    this.score += ZedUtil.ProcessRoundDeaths(this.Zeds, this.Humans, this.shouldLog);
                    if(this.shouldLog) this.LogState();
                    this.round++;
                    return true;
                }
            }
        }
        return false;
    }

    public void LogState()
    {
        //Console.Error.WriteLine($"Logging Round {this.round}");
        //foreach(KeyValuePair<int, Zed> zed in this.Zeds)
        //{
        //    Console.Error.WriteLine($"Zed {zed.Key} ({zed.Value.X},{zed.Value.Y})");
        //}

        //foreach(KeyValuePair<int, Human> human in this.Humans)
        //{
        //    Console.Error.WriteLine($"Human {human.Key * -1 + 1} ({human.Value.X},{human.Value.Y})");
        //}
    }

    public void OverrideScore(int score)
    {
        this.score = score;
    }

    public int[] GetGenome()
    {
        return this.ProcessOrder;
    }

    public void SetGenome(int[] genome)
    {
        this.ProcessOrder = genome;
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
    public const int AshId = -50000;
    public const string ZEDS = "ZEDS";
    public const string HUMANS = "HUMANS";
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

    public static void MoveAsh(int zX, int zY, Human ash, bool stopAtLocation, bool shouldLog = false)
    {
        int distanceToZed = Utility.CalculateDistanceAsInt(ash.X, ash.Y, zX, zY);
        if(shouldLog) Console.Error.Write($"Moving ash from ({ash.X},{ash.Y} towards ({zX},{zX}) )");
        if(distanceToZed <= 1000 && stopAtLocation)
        {
            ash.Data = new HData { X = zX, Y = zY };
        }
        else
        {
            (int nX, int nY) = Utility.ProjectDistanceFromPointToPoint(ash.X, ash.Y, zX, zY, 1000);
            ash.Data = new HData { X = Math.Max(Math.Min(nX, 16000), 0) , Y = Math.Max(Math.Min(nY, 9000), 0) };
        }
        if(shouldLog) Console.Error.WriteLine($"resulting at ({ash.X},{ash.Y})");
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

        foreach(KeyValuePair<int, Human> human in humans)
        {
            foreach(Zed zed in zeds.Values)
            {
                if(zed.X == human.Value.X && zed.Y == human.Value.Y)
                {
                    humans.Remove(human.Key);
                    if(shouldLog) Console.Error.WriteLine($"--SIM-- Human {human.Key} at ({human.Value.X},{human.Value.Y})");
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
    public Fibonacci() { this.a = 1; this.b = 1; }

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