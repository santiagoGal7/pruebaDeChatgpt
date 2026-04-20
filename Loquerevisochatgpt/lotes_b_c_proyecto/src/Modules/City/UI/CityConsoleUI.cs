namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.UI;

/// <summary>
/// Interfaz de consola para el módulo City.
/// Implementa <see cref="IModuleUI"/> para integración con el menú principal.
/// </summary>
public sealed class CityConsoleUI : IModuleUI
{
    private readonly ICityService _service;

    public string Key   => "city";
    public string Title => "City Management";

    public CityConsoleUI(ICityService service) => _service = service;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        bool running = true;
        while (running && !cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("\n========== CITY MANAGEMENT ==========");
            Console.WriteLine("1. List all cities");
            Console.WriteLine("2. Get city by ID");
            Console.WriteLine("3. Create city");
            Console.WriteLine("4. Update city");
            Console.WriteLine("5. Delete city");
            Console.WriteLine("0. Back to main menu");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            try
            {
                switch (option)
                {
                    case "1": await ListAllAsync(cancellationToken);       break;
                    case "2": await GetByIdAsync(cancellationToken);       break;
                    case "3": await CreateAsync(cancellationToken);        break;
                    case "4": await UpdateAsync(cancellationToken);        break;
                    case "5": await DeleteAsync(cancellationToken);        break;
                    case "0": running = false;                             break;
                    default:  Console.WriteLine("Invalid option.");        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
            }
        }
    }

    private async Task ListAllAsync(CancellationToken ct)
    {
        var cities = await _service.GetAllAsync(ct);
        if (!cities.Any()) { Console.WriteLine("No cities found."); return; }
        Console.WriteLine($"\n{"ID",-6} {"Name",-30} {"CountryId",-12} {"CreatedAt",-22}");
        Console.WriteLine(new string('-', 72));
        foreach (var c in cities)
            Console.WriteLine($"{c.CityId,-6} {c.Name,-30} {c.CountryId,-12} {c.CreatedAt:yyyy-MM-dd HH:mm}");
    }

    private async Task GetByIdAsync(CancellationToken ct)
    {
        Console.Write("Enter City ID: ");
        if (!int.TryParse(Console.ReadLine(), out var id)) { Console.WriteLine("Invalid ID."); return; }
        var city = await _service.GetByIdAsync(id, ct);
        if (city is null) { Console.WriteLine("City not found."); return; }
        Console.WriteLine($"\nID: {city.CityId} | Name: {city.Name} | CountryId: {city.CountryId} | CreatedAt: {city.CreatedAt:yyyy-MM-dd HH:mm}");
    }

    private async Task CreateAsync(CancellationToken ct)
    {
        Console.Write("City name: ");
        var name = Console.ReadLine()?.Trim() ?? string.Empty;
        Console.Write("Country ID: ");
        if (!int.TryParse(Console.ReadLine(), out var countryId)) { Console.WriteLine("Invalid country ID."); return; }

        var created = await _service.CreateAsync(new CreateCityRequest(name, countryId), ct);
        Console.WriteLine($"City created with ID: {created.CityId}");
    }

    private async Task UpdateAsync(CancellationToken ct)
    {
        Console.Write("City ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out var id)) { Console.WriteLine("Invalid ID."); return; }
        Console.Write("New name: ");
        var name = Console.ReadLine()?.Trim() ?? string.Empty;
        Console.Write("New Country ID: ");
        if (!int.TryParse(Console.ReadLine(), out var countryId)) { Console.WriteLine("Invalid country ID."); return; }

        var updated = await _service.UpdateAsync(id, new UpdateCityRequest(name, countryId), ct);
        Console.WriteLine($"City updated: {updated.Name}");
    }

    private async Task DeleteAsync(CancellationToken ct)
    {
        Console.Write("City ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out var id)) { Console.WriteLine("Invalid ID."); return; }
        Console.Write($"Confirm delete city {id}? (y/n): ");
        if (Console.ReadLine()?.Trim().ToLower() != "y") { Console.WriteLine("Cancelled."); return; }
        await _service.DeleteAsync(id, ct);
        Console.WriteLine("City deleted successfully.");
    }
}
