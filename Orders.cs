using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace OrderParser;

public class Orders
{
    public class Order
    {
        public string? OrderNumber;
        public int TotalItems;
        public decimal TotalCost;
        public DateTime OrderDate;
        public string? CustomerName;
        public string? CustomerPhone;
        public string? CustomerEmail;
        public bool IsPaid;
        public bool IsShipped;
        public bool IsCompleted;
        public Address Address = new Address();
        public List<OrderLineItem> LineItems = new List<OrderLineItem>();
        public List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }

    public class Address
    {
        public string? AddressLine1;
        public string? AddressLine2;
        public string? City;
        public string? State;
        public string? Zip;

        public static Address Parse(string line)
        {
            Address address = new Address();

            address.AddressLine1 = line.Substring(3, 50).Trim();
            address.AddressLine2 = line.Substring(53, 50).Trim();
            address.City = line.Substring(103, 50).Trim();
            address.State = line.Substring(153, 2).Trim();
            address.Zip = line.Substring(155, 10).Trim();

            return address;
        }
    }

    public class OrderLineItem
    {
        public int LineNumber;
        public int Quantity;
        public decimal CostEach;
        public decimal TotalCost;
        public string? Description;

        public static OrderLineItem Parse(string line)
        {
            OrderLineItem lineItem = new OrderLineItem();

            lineItem.LineNumber = int.Parse(line.Substring(3, 2).Trim());
            lineItem.Quantity = int.Parse(line.Substring(5, 5).Trim());
            lineItem.CostEach = decimal.Parse(line.Substring(10, 10).Trim());
            lineItem.TotalCost = decimal.Parse(line.Substring(20, 10).Trim());
            lineItem.Description = line.Substring(30, 50).Trim();

            return lineItem;
        }
    }

    public List<Order> OrderList = new List<Order>();

    // Reads and parses the fixed-width order file into OrderList
    public void ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Error: File not found at {filePath}");
            return;
        }

        try
        {
            using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using StreamReader reader = new StreamReader(stream);

            Order currentOrder = new Order();
            bool hasCurrentOrder = false;
            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line) || line.Length < 3)
                {
                    throw new FormatException("Invalid file format: empty or short line encountered.");
                }

                string lineType = line.Substring(0, 3);

                switch (lineType)
                {
                    case "100":
                        // Finalize previous order if exists
                        if (hasCurrentOrder)
                        {
                            ValidateOrder(currentOrder);
                            OrderList.Add(currentOrder);
                        }
                        // Start new order
                        currentOrder = ParseOrderHeader(line);
                        hasCurrentOrder = true;
                        break;

                    case "200":
                        if (!hasCurrentOrder)
                        {
                            throw new FormatException("Invalid file format: 200 line found before 100 line.");
                        }
                        currentOrder.Address = Address.Parse(line);
                        break;

                    case "300":
                        if (!hasCurrentOrder)
                        {
                            throw new FormatException("Invalid file format: 300 line found before 100 line.");
                        }
                        currentOrder.LineItems.Add(OrderLineItem.Parse(line));
                        break;

                    default:
                        throw new FormatException($"Invalid file format: unknown line type '{lineType}'.");
                }
            }

            // Finalize last order
            if (hasCurrentOrder)
            {
                ValidateOrder(currentOrder);
                OrderList.Add(currentOrder);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading file: {ex.Message}");
            OrderList.Clear();
        }
    }

    // Parses a 100 line type containing order header and customer info
    private Order ParseOrderHeader(string line)
    {
        Order order = new Order();

        if (line.Length != 180)
        {
            order.Errors.Add("Header line must be exactly 180 characters.");
            return order;
        }

        // Position 3, Length 10 - Order number
        order.OrderNumber = line.Substring(3, 10).Trim();
        if (string.IsNullOrWhiteSpace(order.OrderNumber) || !order.OrderNumber.All(char.IsDigit))
        {
            order.Errors.Add("Order number is invalid or empty.");
        }

        // Position 13, Length 5 - Total Items
        string totalItemsStr = line.Substring(13, 5).Trim();
        if (!int.TryParse(totalItemsStr, out int totalItems))
        {
            order.Errors.Add("Invalid total items format.");
        }
        order.TotalItems = totalItems;

        // Position 18, Length 10 - Total Cost
        string totalCostStr = line.Substring(18, 10).Trim();
        if (!decimal.TryParse(totalCostStr, out decimal totalCost))
        {
            order.Errors.Add("Invalid total cost format.");
        }
        order.TotalCost = totalCost;

        // Position 28, Length 19 - Order Date
        string orderDateStr = line.Substring(28, 19).Trim();
        if (!DateTime.TryParseExact(orderDateStr, "MM/dd/yyyy HH:mm:ss",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime orderDate))
        {
            order.Errors.Add("Invalid order date format.");
        }
        order.OrderDate = orderDate;

        // Position 47, Length 50 - Customer Name
        order.CustomerName = line.Substring(47, 50).Trim();

        // Position 97, Length 30 - Customer Phone
        order.CustomerPhone = line.Substring(97, 30).Trim();

        // Position 127, Length 50 - Customer Email
        order.CustomerEmail = line.Substring(127, 50).Trim();

        // Position 177, Length 1 - Paid
        string paidStr = line.Substring(177, 1);
        if (paidStr != "0" && paidStr != "1")
        {
            order.Errors.Add("Invalid paid flag (must be 0 or 1).");
        }
        order.IsPaid = paidStr == "1";

        // Position 178, Length 1 - Shipped
        string shippedStr = line.Substring(178, 1);
        if (shippedStr != "0" && shippedStr != "1")
        {
            order.Errors.Add("Invalid shipped flag (must be 0 or 1).");
        }
        order.IsShipped = shippedStr == "1";

        // Position 179, Length 1 - Completed
        string completedStr = line.Substring(179, 1);
        if (completedStr != "0" && completedStr != "1")
        {
            order.Errors.Add("Invalid completed flag (must be 0 or 1).");
        }
        order.IsCompleted = completedStr == "1";

        return order;
    }

    // Validates all order fields and sets IsValid/Errors accordingly
    private void ValidateOrder(Order order)
    {
        // Validate total items
        if (order.TotalItems < 1)
        {
            order.Errors.Add("Total items must be >= 1.");
        }

        // Validate total cost
        if (order.TotalCost < 0)
        {
            order.Errors.Add("Total cost must be >= 0.");
        }

        // Validate customer name
        if (string.IsNullOrWhiteSpace(order.CustomerName))
        {
            order.Errors.Add("Customer name is required.");
        }

        // Validate address
        if (order.Address == null)
        {
            order.Errors.Add("Address is missing.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(order.Address.AddressLine1))
            {
                order.Errors.Add("Address line 1 is required.");
            }

            if (string.IsNullOrWhiteSpace(order.Address.City))
            {
                order.Errors.Add("City is required.");
            }

            if (string.IsNullOrWhiteSpace(order.Address.State) || order.Address.State.Length != 2)
            {
                order.Errors.Add("State must be exactly 2 characters.");
            }

            if (string.IsNullOrWhiteSpace(order.Address.Zip))
            {
                order.Errors.Add("Zip is required.");
            }
        }

        // Validate line items exist
        if (order.LineItems.Count == 0)
        {
            order.Errors.Add("Order must have at least one line item.");
        }
        else
        {
            // Validate each line item
            foreach (OrderLineItem item in order.LineItems)
            {
                if (item.LineNumber <= 0)
                {
                    order.Errors.Add($"Line {item.LineNumber} has invalid line number.");
                }

                if (item.Quantity <= 0)
                {
                    order.Errors.Add($"Line {item.LineNumber} has invalid quantity.");
                }

                if (item.CostEach < 0)
                {
                    order.Errors.Add($"Line {item.LineNumber} has invalid cost each.");
                }

                if (item.TotalCost < 0)
                {
                    order.Errors.Add($"Line {item.LineNumber} has invalid total cost.");
                }

                if (string.IsNullOrWhiteSpace(item.Description))
                {
                    order.Errors.Add($"Line {item.LineNumber} has missing description.");
                }

                // Validate calculated total
                decimal calculatedTotal = item.Quantity * item.CostEach;
                if (Math.Abs(calculatedTotal - item.TotalCost) > 0.01m)
                {
                    order.Errors.Add($"Line {item.LineNumber} total mismatch (qty * cost != total).");
                }
            }

            // Validate order-level totals
            int totalQuantity = order.LineItems.Sum(x => x.Quantity);
            if (totalQuantity != order.TotalItems)
            {
                order.Errors.Add($"Total quantity ({totalQuantity}) does not match header total items ({order.TotalItems}).");
            }

            decimal sumLineItemTotals = order.LineItems.Sum(x => x.TotalCost);
            if (sumLineItemTotals != order.TotalCost)
            {
                order.Errors.Add($"Sum of line items (${sumLineItemTotals:F2}) does not match header total (${order.TotalCost:F2}).");
            }
        }
    }

    public void DisplayOrders()
    {
        if (OrderList.Count == 0)
        {
            Console.WriteLine("No orders found.");
            return;
        }

        Console.WriteLine($"\n{'=',-60}");
        Console.WriteLine($"PARSED ORDERS - Total: {OrderList.Count}");
        Console.WriteLine($"{'=',-60}\n");

        foreach (Order order in OrderList)
        {
            Console.WriteLine($"Order #: {order.OrderNumber}");
            Console.WriteLine($"Status: {(order.IsValid ? "SUCCESS" : "FAILED")}");
            Console.WriteLine($"Customer: {order.CustomerName}");
            Console.WriteLine($"Order Date: {order.OrderDate:MM/dd/yyyy HH:mm:ss}");
            Console.WriteLine($"Total Items: {order.TotalItems} | Total Cost: ${order.TotalCost:F2}");
            Console.WriteLine($"Paid: {order.IsPaid} | Shipped: {order.IsShipped} | Completed: {order.IsCompleted}");

            if (order.Address != null)
            {
                Console.WriteLine($"Address: {order.Address.AddressLine1}");
                if (!string.IsNullOrWhiteSpace(order.Address.AddressLine2))
                {
                    Console.WriteLine($"         {order.Address.AddressLine2}");
                }
                Console.WriteLine($"         {order.Address.City}, {order.Address.State} {order.Address.Zip}");
            }
            else
            {
                Console.WriteLine("Address: [MISSING]");
            }

            Console.WriteLine("Line Items:");
            if (order.LineItems.Count > 0)
            {
                foreach (OrderLineItem item in order.LineItems)
                {
                    Console.WriteLine($"  {item.LineNumber}. {item.Description} - Qty: {item.Quantity} @ ${item.CostEach:F2} = ${item.TotalCost:F2}");
                }
            }
            else
            {
                Console.WriteLine("  [No line items]");
            }

            if (!order.IsValid)
            {
                Console.WriteLine("\nERRORS:");
                foreach (string error in order.Errors)
                {
                    Console.WriteLine($"  - {error}");
                }
            }

            Console.WriteLine($"{'-',-60}\n");
        }

        // Summary statistics
        int successCount = OrderList.Count(o => o.IsValid);
        int failedCount = OrderList.Count(o => !o.IsValid);
        Console.WriteLine($"\n{'=',-60}");
        Console.WriteLine($"SUMMARY: {successCount} successful, {failedCount} failed");
        Console.WriteLine($"{'=',-60}\n");
    }
}
