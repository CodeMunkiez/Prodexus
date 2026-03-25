using System;

namespace Prodexus.Functions
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(Greet("developer"));
        }

        public static string Greet(string name)
        {
            return $"Hello, {name}! Welcome to Prodexus.Functions .NET Core.";
        }
    }
}
