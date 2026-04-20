namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.UI;

public sealed class AircraftTypeConsoleUI : IModuleUI
{
    private readonly IAircraftTypeService _service;
    public string Key   => "aircraft_type";
    public string Title => "Aircraft Type Management";
    public AircraftTypeConsoleUI(IAircraftTypeService service) => _service = service;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        bool running = true;
        while (running && !cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("\n========== AIRCRAFT TYPE MANAGEMENT ==========");
            Console.WriteLine("1. List  2. Get by ID  3. Create  4. Update  5. Delete  0. Back");
            Console.Write("Select: ");
            var opt = Console.ReadLine()?.Trim();
            try
            {
                switch (opt)
                {
                    case "1":
                        var list = await _service.GetAllAsync(cancellationToken);
                        Console.WriteLine($"\n{"ID",-6} {"ManufID",-8} {"Model",-20} {"Seats",-7} {"CargoKg",-12}");
                        foreach (var t in list) Console.WriteLine($"{t.AircraftTypeId,-6} {t.ManufacturerId,-8} {t.Model,-20} {t.TotalSeats,-7} {t.CargoCapacityKg,-12}");
                        break;
                    case "2":
                        Console.Write("ID: "); if (!int.TryParse(Console.ReadLine(), out var gId)) break;
                        var gt = await _service.GetByIdAsync(gId, cancellationToken);
                        Console.WriteLine(gt is null ? "Not found." : $"[{gt.AircraftTypeId}] {gt.Model} Seats:{gt.TotalSeats} Cargo:{gt.CargoCapacityKg}kg");
                        break;
                    case "3":
                        Console.Write("Manufacturer ID: "); if (!int.TryParse(Console.ReadLine(), out var mId)) break;
                        Console.Write("Model: "); var model = Console.ReadLine()?.Trim() ?? "";
                        Console.Write("Total seats: "); if (!int.TryParse(Console.ReadLine(), out var seats)) break;
                        Console.Write("Cargo capacity (kg): "); if (!decimal.TryParse(Console.ReadLine(), out var cargo)) break;
                        var created = await _service.CreateAsync(new CreateAircraftTypeRequest(mId, model, seats, cargo), cancellationToken);
                        Console.WriteLine($"Created ID: {created.AircraftTypeId}");
                        break;
                    case "4":
                        Console.Write("ID: "); if (!int.TryParse(Console.ReadLine(), out var uId)) break;
                        Console.Write("Manufacturer ID: "); if (!int.TryParse(Console.ReadLine(), out var umId)) break;
                        Console.Write("Model: "); var um = Console.ReadLine()?.Trim() ?? "";
                        Console.Write("Seats: "); if (!int.TryParse(Console.ReadLine(), out var us)) break;
                        Console.Write("Cargo kg: "); if (!decimal.TryParse(Console.ReadLine(), out var uc)) break;
                        await _service.UpdateAsync(uId, new UpdateAircraftTypeRequest(umId, um, us, uc), cancellationToken);
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
