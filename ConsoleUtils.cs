using System;

namespace AS.Concept_vSdk.Cli
{
    internal static class ConsoleUtils
    {
        public const string Prompt = "> ";
        public static void WriteLine(string line, ConsoleColor color)
        {
            var currentColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(line);
            Console.ForegroundColor = currentColor;
        }

        public static void Write(string line, ConsoleColor color)
        {
            var currentColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(line);
            Console.ForegroundColor = currentColor;
        }

        public static void WritePrompt()
        {
            Console.Write(Prompt);
        }
    }
}
