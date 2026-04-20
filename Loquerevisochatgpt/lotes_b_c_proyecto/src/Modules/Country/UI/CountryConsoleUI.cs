namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Application.Interfaces;

public sealed class CountryConsoleUI
{
    private readonly ICountryService _service;

    public CountryConsoleUI(ICountryService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== COUNTRY MODULE ==========");
            Console.WriteLine("1. List all countries");
            Console.WriteLine("2. Get country by ID");
            Console.WriteLine("3. Create country");
            Console.WriteLine("4. Update country");
            Console.WriteLine("5. Delete country");
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
                default: Console.WriteLine("Invalid option."); break;
            }
        }
    }

    private async Task ListAllAsync()
    {
        var countries = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Countries ---");
        foreach (var c in countries)
            Console.WriteLine($"  [{c.Id}] {c.Name}");
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter country ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var c = await _service.GetByIdAsync(id);
        if (c is null) Console.WriteLine($"Country with ID {id} not found.");
        else           Console.WriteLine($"  [{c.Id}] {c.Name}");
    }

    private async Task CreateAsync()
    {
        Console.Write("Enter country name: ");
        var name = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(name)) { Console.WriteLine("Name cannot be empty."); return; }

        var created = await _service.CreateAsync(name);
        Console.WriteLine($"Country created: [{created.Id}] {created.Name}");
    }

    private async Task UpdateAsync()
    {
        Console.Write("Enter country ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Enter new name: ");
        var newName = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(newName)) { Console.WriteLine("Name cannot be empty."); return; }

        await _service.UpdateAsync(id, newName);
        Console.WriteLine("Country updated successfully.");
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter country ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) { Console.WriteLine("Invalid ID."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Country deleted successfully.");
    }
}
