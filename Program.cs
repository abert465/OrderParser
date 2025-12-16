using System;

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
        }
        else
        {
           
            Console.Write("Enter the path to the order file: ");
            filePath = Console.ReadLine() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Console.WriteLine("Error: No file path provided.");
            return;
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
}
