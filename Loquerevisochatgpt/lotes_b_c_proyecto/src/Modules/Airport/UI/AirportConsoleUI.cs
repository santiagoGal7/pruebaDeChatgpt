namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.UI;

/// <summary>Interfaz de consola para el módulo Airport.</summary>
public sealed class AirportConsoleUI : IModuleUI
{
    private readonly IAirportService _service;

    public string Key   => "airport";
    public string Title => "Airport Management";

    public AirportConsoleUI(IAirportService service) => _service = service;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        bool running = true;
        while (running && !cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("\n========== AIRPORT MANAGEMENT ==========");
            Console.WriteLine("1. List all airports");
            Console.WriteLine("2. Get airport by ID");
            Console.WriteLine("3. Create airport");
            Console.WriteLine("4. Update airport");
            Console.WriteLine("5. Delete airport");
            Console.WriteLine("0. Back to main menu");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();
            try
            {
                switch (option)
                {
                    case "1": await ListAllAsync(cancellationToken);  break;
                    case "2": await GetByIdAsync(cancellationToken);  break;
                    case "3": await CreateAsync(cancellationToken);   break;
                    case "4": await UpdateAsync(cancellationToken);   break;
                    case "5": await DeleteAsync(cancellationToken);   break;
                    case "0": running = false;                        break;
                    default:  Console.WriteLine("Invalid option.");   break;
                }
            }
            catch (Exception ex) { Console.WriteLine($"[ERROR] {ex.Message}"); }
        }
    }

    private async Task ListAllAsync(CancellationToken ct)
    {
        var list = await _service.GetAllAsync(ct);
        if (!list.Any()) { Console.WriteLine("No airports found."); return; }
        Console.WriteLine($"\n{"ID",-6} {"IATA",-6} {"Name",-35} {"CityId",-8} {"CreatedAt",-22}");
        Console.WriteLine(new string('-', 79));
        foreach (var a in list)
            Console.WriteLine($"{a.AirportId,-6} {a.IataCode,-6} {a.Name,-35} {a.CityId,-8} {a.CreatedAt:yyyy-MM-dd HH:mm}");
    }

    private async Task GetByIdAsync(CancellationToken ct)
    {
        Console.Write("Enter Airport ID: ");
        if (!int.TryParse(Console.ReadLine(), out var id)) { Console.WriteLine("Invalid ID."); return; }
        var a = await _service.GetByIdAsync(id, ct);
        if (a is null) { Console.WriteLine("Airport not found."); return; }
        Console.WriteLine($"\nID: {a.AirportId} | IATA: {a.IataCode} | Name: {a.Name} | CityId: {a.CityId}");
    }

    private async Task CreateAsync(CancellationToken ct)
    {
        Console.Write("IATA Code (3 letters): ");
        var iata = Console.ReadLine()?.Trim() ?? string.Empty;
        Console.Write("Airport name: ");
        var name = Console.ReadLine()?.Trim() ?? string.Empty;
        Console.Write("City ID: ");
        if (!int.TryParse(Console.ReadLine(), out var cityId)) { Console.WriteLine("Invalid city ID."); return; }

        var created = await _service.CreateAsync(new CreateAirportRequest(iata, name, cityId), ct);
        Console.WriteLine($"Airport created with ID: {created.AirportId}");
    }

    private async Task UpdateAsync(CancellationToken ct)
    {
        Console.Write("Airport ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out var id)) { Console.WriteLine("Invalid ID."); return; }
        Console.Write("New IATA Code (3 letters): ");
        var iata = Console.ReadLine()?.Trim() ?? string.Empty;
        Console.Write("New name: ");
        var name = Console.ReadLine()?.Trim() ?? string.Empty;
        Console.Write("New City ID: ");
        if (!int.TryParse(Console.ReadLine(), out var cityId)) { Console.WriteLine("Invalid city ID."); return; }

        var updated = await _service.UpdateAsync(id, new UpdateAirportRequest(iata, name, cityId), ct);
        Console.WriteLine($"Airport updated: {updated.Name} ({updated.IataCode})");
    }

    private async Task DeleteAsync(CancellationToken ct)
    {
        Console.Write("Airport ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out var id)) { Console.WriteLine("Invalid ID."); return; }
        Console.Write($"Confirm delete airport {id}? (y/n): ");
        if (Console.ReadLine()?.Trim().ToLower() != "y") { Console.WriteLine("Cancelled."); return; }
        await _service.DeleteAsync(id, ct);
        Console.WriteLine("Airport deleted successfully.");
    }
}
