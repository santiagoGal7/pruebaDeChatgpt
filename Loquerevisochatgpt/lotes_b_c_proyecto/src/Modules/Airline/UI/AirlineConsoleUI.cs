namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.UI;

public sealed class AirlineConsoleUI : IModuleUI
{
    private readonly IAirlineService _service;
    public string Key   => "airline";
    public string Title => "Airline Management";
    public AirlineConsoleUI(IAirlineService service) => _service = service;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        bool running = true;
        while (running && !cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("\n========== AIRLINE MANAGEMENT ==========");
            Console.WriteLine("1. List all airlines");
            Console.WriteLine("2. Get airline by ID");
            Console.WriteLine("3. Create airline");
            Console.WriteLine("4. Update airline");
            Console.WriteLine("5. Delete airline");
            Console.WriteLine("0. Back");
            Console.Write("Select: ");
            var opt = Console.ReadLine()?.Trim();
            try
            {
                switch (opt)
                {
                    case "1": await ListAllAsync(cancellationToken); break;
                    case "2": await GetByIdAsync(cancellationToken); break;
                    case "3": await CreateAsync(cancellationToken);  break;
                    case "4": await UpdateAsync(cancellationToken);  break;
                    case "5": await DeleteAsync(cancellationToken);  break;
                    case "0": running = false;                       break;
                    default:  Console.WriteLine("Invalid option.");  break;
                }
            }
            catch (Exception ex) { Console.WriteLine($"[ERROR] {ex.Message}"); }
        }
    }

    private async Task ListAllAsync(CancellationToken ct)
    {
        var list = await _service.GetAllAsync(ct);
        if (!list.Any()) { Console.WriteLine("No airlines found."); return; }
        Console.WriteLine($"\n{"ID",-6} {"IATA",-6} {"Name",-30} {"Active",-8}");
        Console.WriteLine(new string('-', 52));
        foreach (var a in list)
            Console.WriteLine($"{a.AirlineId,-6} {a.IataCode,-6} {a.Name,-30} {a.IsActive,-8}");
    }

    private async Task GetByIdAsync(CancellationToken ct)
    {
        Console.Write("Airline ID: ");
        if (!int.TryParse(Console.ReadLine(), out var id)) { Console.WriteLine("Invalid ID."); return; }
        var a = await _service.GetByIdAsync(id, ct);
        if (a is null) { Console.WriteLine("Airline not found."); return; }
        Console.WriteLine($"ID: {a.AirlineId} | IATA: {a.IataCode} | Name: {a.Name} | Active: {a.IsActive}");
    }

    private async Task CreateAsync(CancellationToken ct)
    {
        Console.Write("IATA Code (2 letters): "); var iata = Console.ReadLine()?.Trim() ?? "";
        Console.Write("Name: "); var name = Console.ReadLine()?.Trim() ?? "";
        Console.Write("Is active? (y/n): "); var active = Console.ReadLine()?.Trim().ToLower() == "y";
        var created = await _service.CreateAsync(new CreateAirlineRequest(iata, name, active), ct);
        Console.WriteLine($"Airline created with ID: {created.AirlineId}");
    }

    private async Task UpdateAsync(CancellationToken ct)
    {
        Console.Write("Airline ID: "); if (!int.TryParse(Console.ReadLine(), out var id)) return;
        Console.Write("New IATA: "); var iata = Console.ReadLine()?.Trim() ?? "";
        Console.Write("New name: "); var name = Console.ReadLine()?.Trim() ?? "";
        Console.Write("Active? (y/n): "); var active = Console.ReadLine()?.Trim().ToLower() == "y";
        var updated = await _service.UpdateAsync(id, new UpdateAirlineRequest(iata, name, active), ct);
        Console.WriteLine($"Updated: {updated.Name}");
    }

    private async Task DeleteAsync(CancellationToken ct)
    {
        Console.Write("Airline ID to delete: "); if (!int.TryParse(Console.ReadLine(), out var id)) return;
        Console.Write($"Confirm delete airline {id}? (y/n): ");
        if (Console.ReadLine()?.Trim().ToLower() != "y") return;
        await _service.DeleteAsync(id, ct);
        Console.WriteLine("Airline deleted.");
    }
}
