using System.Diagnostics;

namespace GameOfLife;

public static class Program
{
    public static void Main()
    {
        //Console.CursorVisible = false;

        int counter = 0;

        //var world = new World(140, 70, true);
        using var world = new WorldGpu(2000, 2000);

        var stopWatch = Stopwatch.StartNew();

        while (true)
        {
            foreach (var cell in world.Step())
            {
                if (cell.CellLocation.X >= 140 || cell.CellLocation.Y >= 70)
                {
                    continue;
                }

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;

                    if (key == ConsoleKey.Q)
                    {
                        return;
                    }
                }

                Console.SetCursorPosition(cell.CellLocation.X, cell.CellLocation.Y);

                if (cell.IsAlive)
                {
                    Console.Write("@");
                }
                else
                {
                    Console.Write(".");
                }
            }

            //Thread.Sleep(250);

            if (++counter == 500)
            {
                // counter = 0;
                // world = new WorldCpu(2000, 2000);
                stopWatch.Stop();
                break;
            }
        }

        Console.Clear();
        Console.WriteLine($"500 frames in {stopWatch.ElapsedMilliseconds / 1000} seconds.");
    }
}