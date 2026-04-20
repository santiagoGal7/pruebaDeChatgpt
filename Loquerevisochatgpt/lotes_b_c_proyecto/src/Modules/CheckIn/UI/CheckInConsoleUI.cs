namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Application.Interfaces;

public sealed class CheckInConsoleUI
{
    private readonly ICheckInService _service;

    public CheckInConsoleUI(ICheckInService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== CHECK-IN MODULE ==========");
            Console.WriteLine("1. List all check-ins");
            Console.WriteLine("2. Get check-in by ID");
            Console.WriteLine("3. Get check-in by ticket");
            Console.WriteLine("4. Register check-in");
            Console.WriteLine("5. Change check-in status");
            Console.WriteLine("6. Delete check-in");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();         break;
                case "2": await GetByIdAsync();         break;
                case "3": await GetByTicketAsync();     break;
                case "4": await RegisterAsync();        break;
                case "5": await ChangeStatusAsync();    break;
                case "6": await DeleteAsync();          break;
                case "0": running = false;              break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task ListAllAsync()
    {
        var checkIns = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Check-Ins ---");
        foreach (var c in checkIns) PrintCheckIn(c);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter check-in ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var c = await _service.GetByIdAsync(id);
        if (c is null) Console.WriteLine($"Check-in with ID {id} not found.");
        else           PrintCheckIn(c);
    }

    private async Task GetByTicketAsync()
    {
        Console.Write("Enter Ticket ID: ");
        if (!int.TryParse(Console.ReadLine(), out int ticketId))
        { Console.WriteLine("Invalid ID."); return; }

        var c = await _service.GetByTicketAsync(ticketId);
        if (c is null)
            Console.WriteLine($"Ticket {ticketId} has not checked in yet.");
        else
            PrintCheckIn(c);
    }

    private async Task RegisterAsync()
    {
        Console.Write("Ticket ID: ");
        if (!int.TryParse(Console.ReadLine(), out int ticketId))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("Initial Check-In Status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int statusId))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("Counter number (optional): ");
        var counterInput = Console.ReadLine()?.Trim();
        string? counter = string.IsNullOrWhiteSpace(counterInput) ? null : counterInput;

        try
        {
            var created = await _service.CreateAsync(ticketId, statusId, counter);
            Console.WriteLine(
                $"Check-in registered: [{created.Id}] Ticket:{created.TicketId} | " +
                $"Status:{created.CheckInStatusId} | " +
                $"Time:{created.CheckInTime:yyyy-MM-dd HH:mm}" +
                (created.CounterNumber is not null ? $" | Counter:{created.CounterNumber}" : string.Empty));
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task ChangeStatusAsync()
    {
        Console.Write("Check-in ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("New Check-In Status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int statusId))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("Counter number (optional, Enter to keep current): ");
        var counterInput = Console.ReadLine()?.Trim();
        string? counter = string.IsNullOrWhiteSpace(counterInput) ? null : counterInput;

        try
        {
            await _service.ChangeStatusAsync(id, statusId, counter);
            Console.WriteLine("Check-in status updated successfully.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task DeleteAsync()
    {
        Console.Write("Check-in ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Check-in deleted successfully.");
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    private static void PrintCheckIn(CheckInDto c)
        => Console.WriteLine(
            $"  [{c.Id}] Ticket:{c.TicketId} | Status:{c.CheckInStatusId} | " +
            $"Time:{c.CheckInTime:yyyy-MM-dd HH:mm}" +
            (c.CounterNumber is not null ? $" | Counter:{c.CounterNumber}" : string.Empty));
}
