namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.UI;

public sealed class AircraftConsoleUI : IModuleUI
{
    private readonly IAircraftService _service;
    public string Key   => "aircraft";
    public string Title => "Aircraft Management";
    public AircraftConsoleUI(IAircraftService service) => _service = service;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        bool running = true;
        while (running && !cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("\n========== AIRCRAFT MANAGEMENT ==========");
            Console.WriteLine("1. List  2. Get by ID  3. Create  4. Update  5. Delete  0. Back");
            Console.Write("Select: ");
            var opt = Console.ReadLine()?.Trim();
            try
            {
                switch (opt)
                {
                    case "1":
                        var list = await _service.GetAllAsync(cancellationToken);
                        Console.WriteLine($"\n{"ID",-6} {"RegNum",-15} {"AirlineId",-10} {"TypeId",-8} {"Year",-6} {"Active"}");
                        foreach (var a in list) Console.WriteLine($"{a.AircraftId,-6} {a.RegistrationNumber,-15} {a.AirlineId,-10} {a.AircraftTypeId,-8} {a.ManufactureYear,-6} {a.IsActive}");
                        break;
                    case "2":
                        Console.Write("ID: "); if (!int.TryParse(Console.ReadLine(), out var gId)) break;
                        var ga = await _service.GetByIdAsync(gId, cancellationToken);
                        Console.WriteLine(ga is null ? "Not found." : $"[{ga.AircraftId}] {ga.RegistrationNumber} | Airline:{ga.AirlineId} | Type:{ga.AircraftTypeId} | Year:{ga.ManufactureYear}");
                        break;
                    case "3":
                        Console.Write("Airline ID: ");       if (!int.TryParse(Console.ReadLine(), out var aId))   break;
                        Console.Write("Aircraft Type ID: "); if (!int.TryParse(Console.ReadLine(), out var atId))  break;
                        Console.Write("Registration Number: "); var reg = Console.ReadLine()?.Trim() ?? "";
                        Console.Write("Manufacture Year: "); if (!int.TryParse(Console.ReadLine(), out var year))  break;
                        Console.Write("Active? (y/n): "); var active = Console.ReadLine()?.ToLower() == "y";
                        var created = await _service.CreateAsync(new CreateAircraftRequest(aId, atId, reg, year, active), cancellationToken);
                        Console.WriteLine($"Created ID: {created.AircraftId}");
                        break;
                    case "4":
                        Console.Write("Aircraft ID: ");      if (!int.TryParse(Console.ReadLine(), out var uId))   break;
                        Console.Write("Airline ID: ");       if (!int.TryParse(Console.ReadLine(), out var uaId))  break;
                        Console.Write("Aircraft Type ID: "); if (!int.TryParse(Console.ReadLine(), out var uatId)) break;
                        Console.Write("Registration: "); var ureg = Console.ReadLine()?.Trim() ?? "";
                        Console.Write("Year: ");             if (!int.TryParse(Console.ReadLine(), out var uyear)) break;
                        Console.Write("Active? (y/n): "); var uact = Console.ReadLine()?.ToLower() == "y";
                        await _service.UpdateAsync(uId, new UpdateAircraftRequest(uaId, uatId, ureg, uyear, uact), cancellationToken);
                        Console.WriteLine("Updated.");
                        break;
                    case "5":
                        Console.Write("ID: "); if (!int.TryParse(Console.ReadLine(), out var dId)) break;
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
