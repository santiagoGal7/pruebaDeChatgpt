namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Application.Interfaces;

public sealed class CheckInStatusConsoleUI
{
    private readonly ICheckInStatusService _service;

    public CheckInStatusConsoleUI(ICheckInStatusService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== CHECK-IN STATUS MODULE ==========");
            Console.WriteLine("1. List all check-in statuses");
            Console.WriteLine("2. Get check-in status by ID");
            Console.WriteLine("3. Create check-in status");
            Console.WriteLine("4. Update check-in status");
            Console.WriteLine("5. Delete check-in status");
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

    private async Task ListAllAsync()
    {
        var statuses = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Check-In Statuses ---");
        foreach (var s in statuses)
            Console.WriteLine($"  [{s.Id}] {s.Name}");
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter check-in status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var status = await _service.GetByIdAsync(id);
        if (status is null) Console.WriteLine($"Check-in status with ID {id} not found.");
        else                Console.WriteLine($"  [{status.Id}] {status.Name}");
    }

    private async Task CreateAsync()
    {
        Console.Write("Enter name (e.g. PENDING, CHECKED_IN, BOARDED, NO_SHOW): ");
        var name = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        { Console.WriteLine("Name cannot be empty."); return; }

        var created = await _service.CreateAsync(name);
        Console.WriteLine($"Check-in status created: [{created.Id}] {created.Name}");
    }

    private async Task UpdateAsync()
    {
        Console.Write("Enter check-in status ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Enter new name: ");
        var newName = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(newName))
        { Console.WriteLine("Name cannot be empty."); return; }

        await _service.UpdateAsync(id, newName);
        Console.WriteLine("Check-in status updated successfully.");
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter check-in status ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Check-in status deleted successfully.");
    }
}
