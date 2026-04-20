namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.Interfaces;

public sealed class BaggageAllowanceConsoleUI
{
    private readonly IBaggageAllowanceService _service;

    public BaggageAllowanceConsoleUI(IBaggageAllowanceService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== BAGGAGE ALLOWANCE MODULE ==========");
            Console.WriteLine("1. List all allowances");
            Console.WriteLine("2. Get allowance by ID");
            Console.WriteLine("3. Get allowance by cabin class + fare type");
            Console.WriteLine("4. Create allowance");
            Console.WriteLine("5. Update allowance limits");
            Console.WriteLine("6. Delete allowance");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();              break;
                case "2": await GetByIdAsync();              break;
                case "3": await GetByCabinAndFareAsync();    break;
                case "4": await CreateAsync();               break;
                case "5": await UpdateAsync();               break;
                case "6": await DeleteAsync();               break;
                case "0": running = false;                   break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task ListAllAsync()
    {
        var allowances = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Baggage Allowances ---");
        foreach (var a in allowances) PrintAllowance(a);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter allowance ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var a = await _service.GetByIdAsync(id);
        if (a is null) Console.WriteLine($"Allowance with ID {id} not found.");
        else           PrintAllowance(a);
    }

    private async Task GetByCabinAndFareAsync()
    {
        Console.Write("Cabin Class ID: ");
        if (!int.TryParse(Console.ReadLine(), out int cabinId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Fare Type ID: ");
        if (!int.TryParse(Console.ReadLine(), out int fareId))
        { Console.WriteLine("Invalid ID."); return; }

        var a = await _service.GetByCabinAndFareAsync(cabinId, fareId);
        if (a is null)
            Console.WriteLine($"No allowance found for Cabin:{cabinId} / Fare:{fareId}.");
        else
            PrintAllowance(a);
    }

    private async Task CreateAsync()
    {
        Console.Write("Cabin Class ID: ");
        if (!int.TryParse(Console.ReadLine(), out int cabinId)) { Console.WriteLine("Invalid."); return; }

        Console.Write("Fare Type ID: ");
        if (!int.TryParse(Console.ReadLine(), out int fareId)) { Console.WriteLine("Invalid."); return; }

        Console.Write("Carry-on pieces (default 1): ");
        if (!int.TryParse(Console.ReadLine(), out int coPieces)) coPieces = 1;

        Console.Write("Carry-on kg (default 10): ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal coKg)) coKg = 10m;

        Console.Write("Checked pieces (default 0): ");
        if (!int.TryParse(Console.ReadLine(), out int chkPieces)) chkPieces = 0;

        Console.Write("Checked kg (default 0): ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal chkKg)) chkKg = 0m;

        try
        {
            var created = await _service.CreateAsync(
                cabinId, fareId, coPieces, coKg, chkPieces, chkKg);
            Console.WriteLine($"Allowance created: [{created.Id}]");
            PrintAllowance(created);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task UpdateAsync()
    {
        Console.Write("Allowance ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) { Console.WriteLine("Invalid."); return; }

        Console.Write("New carry-on pieces: ");
        if (!int.TryParse(Console.ReadLine(), out int coPieces)) { Console.WriteLine("Invalid."); return; }

        Console.Write("New carry-on kg: ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal coKg)) { Console.WriteLine("Invalid."); return; }

        Console.Write("New checked pieces: ");
        if (!int.TryParse(Console.ReadLine(), out int chkPieces)) { Console.WriteLine("Invalid."); return; }

        Console.Write("New checked kg: ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal chkKg)) { Console.WriteLine("Invalid."); return; }

        try
        {
            await _service.UpdateAsync(id, coPieces, coKg, chkPieces, chkKg);
            Console.WriteLine("Allowance updated successfully.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task DeleteAsync()
    {
        Console.Write("Allowance ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) { Console.WriteLine("Invalid."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Allowance deleted successfully.");
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    private static void PrintAllowance(BaggageAllowanceDto a)
        => Console.WriteLine(
            $"  [{a.Id}] Cabin:{a.CabinClassId} | Fare:{a.FareTypeId} | " +
            $"Carry-on: {a.CarryOnPieces}pcs {a.CarryOnKg:F1}kg | " +
            $"Checked: {a.CheckedPieces}pcs {a.CheckedKg:F1}kg");
}
