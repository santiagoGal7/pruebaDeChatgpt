namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.UI;

public sealed class AircraftManufacturerConsoleUI : IModuleUI
{
    private readonly IAircraftManufacturerService _service;
    public string Key   => "aircraft_manufacturer";
    public string Title => "Aircraft Manufacturer Management";
    public AircraftManufacturerConsoleUI(IAircraftManufacturerService service) => _service = service;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        bool running = true;
        while (running && !cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("\n========== AIRCRAFT MANUFACTURER MANAGEMENT ==========");
            Console.WriteLine("1. List all  2. Get by ID  3. Create  4. Update  5. Delete  0. Back");
            Console.Write("Select: ");
            var opt = Console.ReadLine()?.Trim();
            try
            {
                switch (opt)
                {
                    case "1": var list = await _service.GetAllAsync(cancellationToken);
                              foreach (var m in list) Console.WriteLine($"[{m.ManufacturerId}] {m.Name} (Country: {m.CountryId})");
                              break;
                    case "2": Console.Write("ID: "); if (!int.TryParse(Console.ReadLine(), out var gId)) break;
                              var gm = await _service.GetByIdAsync(gId, cancellationToken);
                              Console.WriteLine(gm is null ? "Not found." : $"[{gm.ManufacturerId}] {gm.Name} Country:{gm.CountryId}");
                              break;
                    case "3": Console.Write("Name: "); var n = Console.ReadLine()?.Trim() ?? "";
                              Console.Write("Country ID: "); if (!int.TryParse(Console.ReadLine(), out var cId)) break;
                              var created = await _service.CreateAsync(new CreateAircraftManufacturerRequest(n, cId), cancellationToken);
                              Console.WriteLine($"Created ID: {created.ManufacturerId}");
                              break;
                    case "4": Console.Write("ID: "); if (!int.TryParse(Console.ReadLine(), out var uId)) break;
                              Console.Write("Name: "); var un = Console.ReadLine()?.Trim() ?? "";
                              Console.Write("Country ID: "); if (!int.TryParse(Console.ReadLine(), out var ucId)) break;
                              await _service.UpdateAsync(uId, new UpdateAircraftManufacturerRequest(un, ucId), cancellationToken);
                              Console.WriteLine("Updated.");
                              break;
                    case "5": Console.Write("ID: "); if (!int.TryParse(Console.ReadLine(), out var dId)) break;
                              Console.Write($"Confirm delete {dId}? (y/n): ");
                              if (Console.ReadLine()?.ToLower() == "y") { await _service.DeleteAsync(dId, cancellationToken); Console.WriteLine("Deleted."); }
                              break;
                    case "0": running = false; break;
                    default: Console.WriteLine("Invalid."); break;
                }
            }
            catch (Exception ex) { Console.WriteLine($"[ERROR] {ex.Message}"); }
        }
    }
}
