namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Application.Interfaces;

public sealed class BoardingPassConsoleUI
{
    private readonly IBoardingPassService _service;

    public BoardingPassConsoleUI(IBoardingPassService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== BOARDING PASS MODULE ==========");
            Console.WriteLine("1. List all boarding passes");
            Console.WriteLine("2. Get boarding pass by ID");
            Console.WriteLine("3. Get boarding pass by check-in");
            Console.WriteLine("4. Issue boarding pass");
            Console.WriteLine("5. Update gate and boarding group");
            Console.WriteLine("6. Delete boarding pass");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();         break;
                case "2": await GetByIdAsync();         break;
                case "3": await GetByCheckInAsync();    break;
                case "4": await IssueAsync();           break;
                case "5": await UpdateAsync();          break;
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
        var passes = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Boarding Passes ---");
        foreach (var p in passes) PrintPass(p);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter boarding pass ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var p = await _service.GetByIdAsync(id);
        if (p is null) Console.WriteLine($"Boarding pass with ID {id} not found.");
        else           PrintPass(p);
    }

    private async Task GetByCheckInAsync()
    {
        Console.Write("Enter Check-In ID: ");
        if (!int.TryParse(Console.ReadLine(), out int checkInId))
        { Console.WriteLine("Invalid ID."); return; }

        var p = await _service.GetByCheckInAsync(checkInId);
        if (p is null)
            Console.WriteLine($"No boarding pass for check-in {checkInId} yet.");
        else
            PrintPass(p);
    }

    private async Task IssueAsync()
    {
        Console.Write("Check-In ID: ");
        if (!int.TryParse(Console.ReadLine(), out int checkInId))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("Flight Seat ID: ");
        if (!int.TryParse(Console.ReadLine(), out int flightSeatId))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("Gate ID (optional): ");
        var gateInput = Console.ReadLine()?.Trim();
        int? gateId = int.TryParse(gateInput, out int gParsed) ? gParsed : null;

        Console.Write("Boarding group (optional, e.g. A, B, 1): ");
        var groupInput = Console.ReadLine()?.Trim();
        string? group = string.IsNullOrWhiteSpace(groupInput) ? null : groupInput;

        try
        {
            var created = await _service.CreateAsync(checkInId, gateId, group, flightSeatId);
            Console.WriteLine($"Boarding pass issued: [{created.Id}]");
            PrintPass(created);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task UpdateAsync()
    {
        Console.Write("Boarding pass ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("New Gate ID (optional, Enter to clear): ");
        var gateInput = Console.ReadLine()?.Trim();
        int? gateId = int.TryParse(gateInput, out int gParsed) ? gParsed : null;

        Console.Write("New boarding group (optional, Enter to clear): ");
        var groupInput = Console.ReadLine()?.Trim();
        string? group = string.IsNullOrWhiteSpace(groupInput) ? null : groupInput;

        try
        {
            await _service.UpdateAsync(id, gateId, group);
            Console.WriteLine("Boarding pass updated successfully.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task DeleteAsync()
    {
        Console.Write("Boarding pass ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Boarding pass deleted successfully.");
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    private static void PrintPass(BoardingPassDto p)
        => Console.WriteLine(
            $"  [{p.Id}] CheckIn:{p.CheckInId} | Seat:{p.FlightSeatId} | " +
            $"Gate:{(p.GateId.HasValue ? p.GateId.ToString() : "N/A")} | " +
            $"Group:{p.BoardingGroup ?? "N/A"}");
}
