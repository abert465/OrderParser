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
        public string OrderNumber { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
        public bool IsShipped { get; set; }
        public bool IsCompleted { get; set; }
        public Address? Address { get; set; }
        public List<OrderLineItem> LineItems { get; set; } = new List<OrderLineItem>();
        public bool IsValid { get; set; } = true;
        public string ErrorMessages { get; set; } = string.Empty;
    }

    public class Address
    {
        public string AddressLine1 { get; set; } = string.Empty;
        public string AddressLine2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
    }

    public class OrderLineItem
    {
        public int LineNumber { get; set; }
        public int Quantity { get; set; }
        public decimal CostEach { get; set; }
        public decimal TotalCost { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public List<Order> OrderList { get; set; } = new List<Order>();

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
            string[] lines = File.ReadAllLines(filePath);
            Order? currentOrder = null;

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.Length < 3)
                {
                    continue;
                }

                string lineType = line.Substring(0, 3);

                switch (lineType)
                {
                    case "100":
                        // Finalize previous order if exists
                        if (currentOrder != null)
                        {
                            ValidateOrder(currentOrder);
                            OrderList.Add(currentOrder);
                        }
                        // Start new order
                        currentOrder = ParseOrderHeader(line);
                        break;

                    case "200":
                        if (currentOrder != null)
                        {
                            currentOrder.Address = ParseAddress(line);
                        }
                        break;

                    case "300":
                        if (currentOrder != null)
                        {
                            var (lineItem, error) = ParseLineItem(line);
                            if (lineItem != null)
                            {
                                currentOrder.LineItems.Add(lineItem);
                            }
                            if (!string.IsNullOrEmpty(error))
                            {
                                currentOrder.IsValid = false;
                                currentOrder.ErrorMessages += error;
                            }
                        }
                        break;

                    default:
                        Console.WriteLine($"Warning: Unknown line type '{lineType}' - skipping line");
                        break;
                }
            }

            // Finalize last order
            if (currentOrder != null)
            {
                ValidateOrder(currentOrder);
                OrderList.Add(currentOrder);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading file: {ex.Message}");
        }
    }

    // Parses a 100 line type containing order header and customer info
    private Order ParseOrderHeader(string line)
    {
        var order = new Order();

        try
        {
            // Ensure line is long enough
            if (line.Length < 180)
            {
                order.IsValid = false;
                order.ErrorMessages += "Header line too short. ";
                return order;
            }

            // Position 3, Length 10 - Order number
            order.OrderNumber = SafeSubstring(line, 3, 10).Trim();

            // Position 13, Length 5 - Total Items
            string totalItemsStr = SafeSubstring(line, 13, 5).Trim();
            if (!int.TryParse(totalItemsStr, out int totalItems))
            {
                order.IsValid = false;
                order.ErrorMessages += "Invalid total items format. ";
            }
            order.TotalItems = totalItems;

            // Position 18, Length 10 - Total Cost
            string totalCostStr = SafeSubstring(line, 18, 10).Trim();
            if (!decimal.TryParse(totalCostStr, out decimal totalCost))
            {
                order.IsValid = false;
                order.ErrorMessages += "Invalid total cost format. ";
            }
            order.TotalCost = totalCost;

            // Position 28, Length 19 - Order Date 
            string orderDateStr = SafeSubstring(line, 28, 19).Trim();
            if (!DateTime.TryParseExact(orderDateStr, "MM/dd/yyyy HH:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime orderDate))
            {
                order.IsValid = false;
                order.ErrorMessages += "Invalid order date format. ";
            }
            order.OrderDate = orderDate;

            // Position 47, Length 50 - Customer Name
            order.CustomerName = SafeSubstring(line, 47, 50).Trim();

            // Position 97, Length 30 - Customer Phone
            order.CustomerPhone = SafeSubstring(line, 97, 30).Trim();

            // Position 127, Length 50 - Customer Email
            order.CustomerEmail = SafeSubstring(line, 127, 50).Trim();

            // Position 177, Length 1 - Paid
            string paidStr = SafeSubstring(line, 177, 1);
            if (paidStr != "0" && paidStr != "1")
            {
                order.IsValid = false;
                order.ErrorMessages += "Invalid paid flag (must be 0 or 1). ";
            }
            order.IsPaid = paidStr == "1";

            // Position 178, Length 1 - Shipped
            string shippedStr = SafeSubstring(line, 178, 1);
            if (shippedStr != "0" && shippedStr != "1")
            {
                order.IsValid = false;
                order.ErrorMessages += "Invalid shipped flag (must be 0 or 1). ";
            }
            order.IsShipped = shippedStr == "1";

            // Position 179, Length 1 - Completed
            string completedStr = SafeSubstring(line, 179, 1);
            if (completedStr != "0" && completedStr != "1")
            {
                order.IsValid = false;
                order.ErrorMessages += "Invalid completed flag (must be 0 or 1). ";
            }
            order.IsCompleted = completedStr == "1";
        }
        catch (Exception ex)
        {
            order.IsValid = false;
            order.ErrorMessages += $"Error parsing header: {ex.Message}. ";
        }

        return order;
    }

    // Parses a 200 line type containing shipping address info
    private Address ParseAddress(string line)
    {
        var address = new Address();

        try
        {
            if (line.Length < 165)
            {
                return address;
            }

            // Position 3, Length 50 - Address line 1
            address.AddressLine1 = SafeSubstring(line, 3, 50).Trim();

            // Position 53, Length 50 - Address line 2
            address.AddressLine2 = SafeSubstring(line, 53, 50).Trim();

            // Position 103, Length 50 - City
            address.City = SafeSubstring(line, 103, 50).Trim();

            // Position 153, Length 2 - State
            address.State = SafeSubstring(line, 153, 2).Trim();

            // Position 155, Length 10 - Zip
            address.Zip = SafeSubstring(line, 155, 10).Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing address: {ex.Message}");
        }

        return address;
    }

    private (OrderLineItem?, string) ParseLineItem(string line)
    {
        var lineItem = new OrderLineItem();
        string errorMessage = string.Empty;

        try
        {
            if (line.Length < 80)
            {
                return (null, "Line item too short. ");
            }

            // Position 3, Length 2 - Line number
            string lineNumStr = SafeSubstring(line, 3, 2).Trim();
            if (!int.TryParse(lineNumStr, out int lineNum))
            {
                return (null, "Invalid line number format. ");
            }
            lineItem.LineNumber = lineNum;

            // Position 5, Length 5 - Quantity
            string quantityStr = SafeSubstring(line, 5, 5).Trim();
            if (!int.TryParse(quantityStr, out int quantity))
            {
                return (null, $"Line {lineNum}: Invalid quantity format. ");
            }
            lineItem.Quantity = quantity;

            // Position 10, Length 10 - Cost each
            string costEachStr = SafeSubstring(line, 10, 10).Trim();
            if (!decimal.TryParse(costEachStr, out decimal costEach))
            {
                return (null, $"Line {lineNum}: Invalid cost each format ('{costEachStr}'). ");
            }
            lineItem.CostEach = costEach;

            // Position 20, Length 10 - Total Cost
            string totalCostStr = SafeSubstring(line, 20, 10).Trim();
            if (!decimal.TryParse(totalCostStr, out decimal totalCost))
            {
                return (null, $"Line {lineNum}: Invalid total cost format ('{totalCostStr}'). ");
            }
            lineItem.TotalCost = totalCost;

            // Position 30, Length 50 - Description
            lineItem.Description = SafeSubstring(line, 30, 50).Trim();
        }
        catch (Exception ex)
        {
            return (null, $"Error parsing line item: {ex.Message}. ");
        }

        return (lineItem, string.Empty);
    }

    // Validates all order fields and sets IsValid/ErrorMessages accordingly
    private void ValidateOrder(Order order)
    {
        // Validate order number
        if (string.IsNullOrWhiteSpace(order.OrderNumber) || !order.OrderNumber.All(char.IsDigit))
        {
            order.IsValid = false;
            order.ErrorMessages += "Order number is invalid or empty. ";
        }

        // Validate total items
        if (order.TotalItems < 1)
        {
            order.IsValid = false;
            order.ErrorMessages += "Total items must be >= 1. ";
        }

        // Validate total cost
        if (order.TotalCost < 0)
        {
            order.IsValid = false;
            order.ErrorMessages += "Total cost must be >= 0. ";
        }

        // Validate customer name
        if (string.IsNullOrWhiteSpace(order.CustomerName))
        {
            order.IsValid = false;
            order.ErrorMessages += "Customer name is required. ";
        }

        // Validate address
        if (order.Address == null)
        {
            order.IsValid = false;
            order.ErrorMessages += "Address is missing. ";
        }
        else
        {
            if (string.IsNullOrWhiteSpace(order.Address.AddressLine1))
            {
                order.IsValid = false;
                order.ErrorMessages += "Address line 1 is required. ";
            }

            if (string.IsNullOrWhiteSpace(order.Address.City))
            {
                order.IsValid = false;
                order.ErrorMessages += "City is required. ";
            }

            if (string.IsNullOrWhiteSpace(order.Address.State) || order.Address.State.Length != 2)
            {
                order.IsValid = false;
                order.ErrorMessages += "State must be exactly 2 characters. ";
            }

            if (string.IsNullOrWhiteSpace(order.Address.Zip))
            {
                order.IsValid = false;
                order.ErrorMessages += "Zip is required. ";
            }
        }

        // Validate line items exist
        if (order.LineItems.Count == 0)
        {
            order.IsValid = false;
            order.ErrorMessages += "Order must have at least one line item. ";
        }
        else
        {
            // Validate each line item
            foreach (var item in order.LineItems)
            {
                if (item.LineNumber <= 0)
                {
                    order.IsValid = false;
                    order.ErrorMessages += $"Line {item.LineNumber} has invalid line number. ";
                }

                if (item.Quantity <= 0)
                {
                    order.IsValid = false;
                    order.ErrorMessages += $"Line {item.LineNumber} has invalid quantity. ";
                }

                if (item.CostEach < 0)
                {
                    order.IsValid = false;
                    order.ErrorMessages += $"Line {item.LineNumber} has invalid cost each. ";
                }

                if (item.TotalCost < 0)
                {
                    order.IsValid = false;
                    order.ErrorMessages += $"Line {item.LineNumber} has invalid total cost. ";
                }

                if (string.IsNullOrWhiteSpace(item.Description))
                {
                    order.IsValid = false;
                    order.ErrorMessages += $"Line {item.LineNumber} has missing description. ";
                }

                // Validate calculated total
                decimal calculatedTotal = item.Quantity * item.CostEach;
                if (Math.Abs(calculatedTotal - item.TotalCost) > 0.01m)
                {
                    order.IsValid = false;
                    order.ErrorMessages += $"Line {item.LineNumber} total mismatch (qty * cost != total). ";
                }
            }

            // Validate order-level totals
            int totalQuantity = order.LineItems.Sum(x => x.Quantity);
            if (totalQuantity != order.TotalItems)
            {
                order.IsValid = false;
                order.ErrorMessages += $"Total quantity ({totalQuantity}) does not match header total items ({order.TotalItems}). ";
            }

            decimal sumLineItemTotals = order.LineItems.Sum(x => x.TotalCost);
            if (Math.Abs(sumLineItemTotals - order.TotalCost) > 0.01m)
            {
                order.IsValid = false;
                order.ErrorMessages += $"Sum of line items (${sumLineItemTotals:F2}) does not match header total (${order.TotalCost:F2}). ";
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

        foreach (var order in OrderList)
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
                foreach (var item in order.LineItems)
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
                Console.WriteLine($"\nERRORS: {order.ErrorMessages}");
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

    private string SafeSubstring(string source, int startIndex, int length)
    {
        if (startIndex >= source.Length)
            return string.Empty;

        if (startIndex + length > source.Length)
            length = source.Length - startIndex;

        return source.Substring(startIndex, length);
    }
}
