namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Application.Interfaces;

public sealed class BaggageTypeConsoleUI
{
    private readonly IBaggageTypeService _service;

    public BaggageTypeConsoleUI(IBaggageTypeService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== BAGGAGE TYPE MODULE ==========");
            Console.WriteLine("1. List all baggage types");
            Console.WriteLine("2. Get baggage type by ID");
            Console.WriteLine("3. Create baggage type");
            Console.WriteLine("4. Update baggage type");
            Console.WriteLine("5. Delete baggage type");
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
        var types = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Baggage Types ---");

        foreach (var t in types)
            Console.WriteLine(
                $"  [{t.Id}] {t.Name} | Max:{t.MaxWeightKg:F1}kg | Fee:{t.ExtraFee:F2}");
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter baggage type ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var t = await _service.GetByIdAsync(id);
        if (t is null)
            Console.WriteLine($"Baggage type with ID {id} not found.");
        else
            Console.WriteLine(
                $"  [{t.Id}] {t.Name} | Max:{t.MaxWeightKg:F1}kg | Fee:{t.ExtraFee:F2}");
    }

    private async Task CreateAsync()
    {
        Console.Write("Name (e.g. STANDARD_CHECKED, OVERSIZE, FRAGILE): ");
        var name = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        { Console.WriteLine("Name cannot be empty."); return; }

        Console.Write("Max weight kg (e.g. 23.00): ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal maxKg))
        { Console.WriteLine("Invalid weight."); return; }

        Console.Write("Extra fee (>= 0, default 0): ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal fee))
            fee = 0m;

        try
        {
            var created = await _service.CreateAsync(name, maxKg, fee);
            Console.WriteLine(
                $"Baggage type created: [{created.Id}] {created.Name} | " +
                $"Max:{created.MaxWeightKg:F1}kg | Fee:{created.ExtraFee:F2}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task UpdateAsync()
    {
        Console.Write("Baggage type ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("New name: ");
        var name = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        { Console.WriteLine("Name cannot be empty."); return; }

        Console.Write("New max weight kg: ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal maxKg))
        { Console.WriteLine("Invalid weight."); return; }

        Console.Write("New extra fee (>= 0): ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal fee))
        { Console.WriteLine("Invalid fee."); return; }

        try
        {
            await _service.UpdateAsync(id, name, maxKg, fee);
            Console.WriteLine("Baggage type updated successfully.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task DeleteAsync()
    {
        Console.Write("Baggage type ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Baggage type deleted successfully.");
    }
}
