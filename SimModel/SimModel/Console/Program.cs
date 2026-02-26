using Engine.Core;

namespace ConsoleApp;

internal class Program
{
    static void Main(string[] args)
    {
        SimulationEngine e = new();
        while (true) 
        {
            var t = e.Tick();
            Console.WriteLine(t);
        }
    }
}
