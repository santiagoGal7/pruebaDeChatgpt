namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Application.Interfaces;

public sealed class CustomerConsoleUI
{
    private readonly ICustomerService _service;

    public CustomerConsoleUI(ICustomerService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== CUSTOMER MODULE ==========");
            Console.WriteLine("1. List all customers");
            Console.WriteLine("2. Get customer by ID");
            Console.WriteLine("3. Create customer");
            Console.WriteLine("4. Update contact info");
            Console.WriteLine("5. Delete customer");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();  break;
                case "2": await GetByIdAsync();  break;
                case "3": await CreateAsync();   break;
                case "4": await UpdateAsync();   break;
                case "5": await DeleteAsync();   break;
                case "0": running = false;       break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task ListAllAsync()
    {
        var customers = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Customers ---");

        foreach (var c in customers)
            PrintCustomer(c);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter customer ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var customer = await _service.GetByIdAsync(id);

        if (customer is null)
            Console.WriteLine($"Customer with ID {id} not found.");
        else
            PrintCustomer(customer);
    }

    private async Task CreateAsync()
    {
        Console.Write("Enter Person ID: ");
        if (!int.TryParse(Console.ReadLine(), out int personId))
        {
            Console.WriteLine("Invalid Person ID.");
            return;
        }

        Console.Write("Phone (optional — press Enter to skip): ");
        var phoneInput = Console.ReadLine()?.Trim();
        string? phone  = string.IsNullOrWhiteSpace(phoneInput) ? null : phoneInput;

        Console.Write("Email (optional — press Enter to skip): ");
        var emailInput = Console.ReadLine()?.Trim();
        string? email  = string.IsNullOrWhiteSpace(emailInput) ? null : emailInput;

        try
        {
            var created = await _service.CreateAsync(personId, phone, email);
            Console.WriteLine(
                $"Customer created: [{created.Id}] PersonId: {created.PersonId} | " +
                $"Email: {created.Email ?? "—"} | Phone: {created.Phone ?? "—"}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task UpdateAsync()
    {
        Console.Write("Enter customer ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        Console.Write("New phone (optional — press Enter to clear): ");
        var phoneInput = Console.ReadLine()?.Trim();
        string? phone  = string.IsNullOrWhiteSpace(phoneInput) ? null : phoneInput;

        Console.Write("New email (optional — press Enter to clear): ");
        var emailInput = Console.ReadLine()?.Trim();
        string? email  = string.IsNullOrWhiteSpace(emailInput) ? null : emailInput;

        try
        {
            await _service.UpdateAsync(id, phone, email);
            Console.WriteLine("Customer contact info updated successfully.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter customer ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        await _service.DeleteAsync(id);
        Console.WriteLine("Customer deleted successfully.");
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    private static void PrintCustomer(CustomerDto c)
        => Console.WriteLine(
            $"  [{c.Id}] PersonId: {c.PersonId} | " +
            $"Phone: {c.Phone ?? "—"} | Email: {c.Email ?? "—"} | " +
            $"Created: {c.CreatedAt:yyyy-MM-dd HH:mm}" +
            (c.UpdatedAt.HasValue ? $" | Updated: {c.UpdatedAt:yyyy-MM-dd HH:mm}" : string.Empty));
}
