namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Application.Interfaces;

public sealed class CancellationReasonConsoleUI
{
    private readonly ICancellationReasonService _service;

    public CancellationReasonConsoleUI(ICancellationReasonService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== CANCELLATION REASON MODULE ==========");
            Console.WriteLine("1. List all cancellation reasons");
            Console.WriteLine("2. Get cancellation reason by ID");
            Console.WriteLine("3. Create cancellation reason");
            Console.WriteLine("4. Update cancellation reason");
            Console.WriteLine("5. Delete cancellation reason");
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
        var reasons = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Cancellation Reasons ---");

        foreach (var r in reasons)
            Console.WriteLine($"  [{r.Id}] {r.Name}");
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter cancellation reason ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var reason = await _service.GetByIdAsync(id);

        if (reason is null)
            Console.WriteLine($"Cancellation reason with ID {id} not found.");
        else
            Console.WriteLine($"  [{reason.Id}] {reason.Name}");
    }

    private async Task CreateAsync()
    {
        Console.Write("Name (e.g. WEATHER, TECHNICAL, COMMERCIAL, FORCE_MAJEURE): ");
        var name = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("Name cannot be empty.");
            return;
        }

        var created = await _service.CreateAsync(name);
        Console.WriteLine($"Cancellation reason created: [{created.Id}] {created.Name}");
    }

    private async Task UpdateAsync()
    {
        Console.Write("Enter cancellation reason ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        Console.Write("Enter new name: ");
        var newName = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(newName))
        {
            Console.WriteLine("Name cannot be empty.");
            return;
        }

        await _service.UpdateAsync(id, newName);
        Console.WriteLine("Cancellation reason updated successfully.");
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter cancellation reason ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        await _service.DeleteAsync(id);
        Console.WriteLine("Cancellation reason deleted successfully.");
    }
}
