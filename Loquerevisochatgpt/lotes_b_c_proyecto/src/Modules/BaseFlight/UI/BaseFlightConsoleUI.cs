namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Application.Interfaces;

public sealed class BaseFlightConsoleUI
{
    private readonly IBaseFlightService _service;

    public BaseFlightConsoleUI(IBaseFlightService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== BASE FLIGHT MODULE ==========");
            Console.WriteLine("1. List all base flights");
            Console.WriteLine("2. Get base flight by ID");
            Console.WriteLine("3. Create base flight");
            Console.WriteLine("4. Update base flight");
            Console.WriteLine("5. Delete base flight");
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
        var flights = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Base Flights ---");

        foreach (var f in flights)
            Console.WriteLine(
                $"  [{f.Id}] {f.FlightCode} | AirlineId: {f.AirlineId} | RouteId: {f.RouteId} | " +
                $"Created: {f.CreatedAt:yyyy-MM-dd HH:mm}" +
                (f.UpdatedAt.HasValue ? $" | Updated: {f.UpdatedAt:yyyy-MM-dd HH:mm}" : string.Empty));
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter base flight ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var flight = await _service.GetByIdAsync(id);

        if (flight is null)
            Console.WriteLine($"Base flight with ID {id} not found.");
        else
            Console.WriteLine(
                $"  [{flight.Id}] {flight.FlightCode} | " +
                $"AirlineId: {flight.AirlineId} | RouteId: {flight.RouteId} | " +
                $"Created: {flight.CreatedAt:yyyy-MM-dd HH:mm}");
    }

    private async Task CreateAsync()
    {
        Console.Write("Enter flight code (min 2 chars, e.g. AV101): ");
        var code = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(code))
        {
            Console.WriteLine("Flight code cannot be empty.");
            return;
        }

        Console.Write("Enter Airline ID: ");
        if (!int.TryParse(Console.ReadLine(), out int airlineId))
        {
            Console.WriteLine("Invalid Airline ID.");
            return;
        }

        Console.Write("Enter Route ID: ");
        if (!int.TryParse(Console.ReadLine(), out int routeId))
        {
            Console.WriteLine("Invalid Route ID.");
            return;
        }

        var created = await _service.CreateAsync(code, airlineId, routeId);
        Console.WriteLine(
            $"Base flight created successfully: [{created.Id}] {created.FlightCode}");
    }

    private async Task UpdateAsync()
    {
        Console.Write("Enter base flight ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        Console.Write("Enter new flight code: ");
        var code = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(code))
        {
            Console.WriteLine("Flight code cannot be empty.");
            return;
        }

        Console.Write("Enter new Airline ID: ");
        if (!int.TryParse(Console.ReadLine(), out int airlineId))
        {
            Console.WriteLine("Invalid Airline ID.");
            return;
        }

        Console.Write("Enter new Route ID: ");
        if (!int.TryParse(Console.ReadLine(), out int routeId))
        {
            Console.WriteLine("Invalid Route ID.");
            return;
        }

        await _service.UpdateAsync(id, code, airlineId, routeId);
        Console.WriteLine("Base flight updated successfully.");
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter base flight ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        await _service.DeleteAsync(id);
        Console.WriteLine("Base flight deleted successfully.");
    }
}
