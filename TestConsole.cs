using System;

namespace TestConsole
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Test console app started");
            Console.WriteLine(".NET version: " + Environment.Version.ToString());
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}