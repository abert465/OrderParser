using System;
using System.IO;

namespace OrderParser;

class Program
{
    static void Main(string[] args)
    {
        string filePath;

        // Check if file path was provided as command line argument
        if (args.Length > 0)
        {
            filePath = args[0];
            
            // Validate command line argument
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: File not found at '{filePath}'.");
                return;
            }
        }
        else
        {
            // Prompt user for file path with validation
            filePath = GetValidFilePath();
        }

        // Create parser instance and parse file
        var orders = new Orders();
        Console.WriteLine($"\nParsing file: {filePath}\n");
        orders.ParseFile(filePath);

        // Display results
        orders.DisplayOrders();

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static string GetValidFilePath()
    {
        while (true)
        {
            Console.Write("Enter the path to the order file: ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Error: File path cannot be empty. Please try again.\n");
                continue;
            }

            if (!File.Exists(input))
            {
                Console.WriteLine($"Error: File not found at '{input}'. Please try again.\n");
                continue;
            }

            return input;
        }
    }
}
