namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Application.Interfaces;

public sealed class CurrencyConsoleUI
{
    private readonly ICurrencyService _service;

    public CurrencyConsoleUI(ICurrencyService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== CURRENCY MODULE ==========");
            Console.WriteLine("1. List all currencies");
            Console.WriteLine("2. Get currency by ID");
            Console.WriteLine("3. Create currency");
            Console.WriteLine("4. Update currency");
            Console.WriteLine("5. Delete currency");
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
        var currencies = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Currencies ---");

        foreach (var c in currencies)
            Console.WriteLine($"  [{c.Id}] {c.IsoCode} — {c.Name} ({c.Symbol})");
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter currency ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var currency = await _service.GetByIdAsync(id);

        if (currency is null)
            Console.WriteLine($"Currency with ID {id} not found.");
        else
            Console.WriteLine($"  [{currency.Id}] {currency.IsoCode} — {currency.Name} ({currency.Symbol})");
    }

    private async Task CreateAsync()
    {
        Console.Write("ISO code (3 chars, e.g. COP, USD, EUR): ");
        var isoCode = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(isoCode))
        { Console.WriteLine("ISO code cannot be empty."); return; }

        Console.Write("Name (e.g. Colombian Peso): ");
        var name = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        { Console.WriteLine("Name cannot be empty."); return; }

        Console.Write("Symbol (e.g. $, €, £): ");
        var symbol = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(symbol))
        { Console.WriteLine("Symbol cannot be empty."); return; }

        try
        {
            var created = await _service.CreateAsync(isoCode, name, symbol);
            Console.WriteLine($"Currency created: [{created.Id}] {created.IsoCode} — {created.Name} ({created.Symbol})");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task UpdateAsync()
    {
        Console.Write("Enter currency ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("New ISO code (3 chars): ");
        var isoCode = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(isoCode)) { Console.WriteLine("ISO code cannot be empty."); return; }

        Console.Write("New name: ");
        var name = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(name)) { Console.WriteLine("Name cannot be empty."); return; }

        Console.Write("New symbol: ");
        var symbol = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(symbol)) { Console.WriteLine("Symbol cannot be empty."); return; }

        try
        {
            await _service.UpdateAsync(id, isoCode, name, symbol);
            Console.WriteLine("Currency updated successfully.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter currency ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        await _service.DeleteAsync(id);
        Console.WriteLine("Currency deleted successfully.");
    }
}
