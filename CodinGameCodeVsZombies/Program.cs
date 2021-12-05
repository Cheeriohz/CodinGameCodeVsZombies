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
    static void Main(string[] args)
    {
        string[] inputs;

        // game loop
        while(true)
        {
            inputs = Console.ReadLine().Split(' ');
            int x = int.Parse(inputs[0]);
            int y = int.Parse(inputs[1]);
            int humanCount = int.Parse(Console.ReadLine());
            for(int i = 0; i < humanCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int humanId = int.Parse(inputs[0]);
                int humanX = int.Parse(inputs[1]);
                int humanY = int.Parse(inputs[2]);
            }
            int zombieCount = int.Parse(Console.ReadLine());
            for(int i = 0; i < zombieCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int zombieId = int.Parse(inputs[0]);
                int zombieX = int.Parse(inputs[1]);
                int zombieY = int.Parse(inputs[2]);
                int zombieXNext = int.Parse(inputs[3]);
                int zombieYNext = int.Parse(inputs[4]);
            }

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");

            Console.WriteLine("0 0"); // Your destination coordinates

        }
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