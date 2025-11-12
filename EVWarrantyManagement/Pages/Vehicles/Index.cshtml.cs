using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Vehicles;

[Authorize(Policy = "RequireAdmin")]
public class IndexModel : PageModel
{
    private readonly IVehicleService _vehicleService;

    public IndexModel(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    public IReadOnlyList<Vehicle> Vehicles { get; private set; } = Array.Empty<Vehicle>();
    public string? Q { get; private set; }

    public async Task OnGetAsync(int? customerId, string? q)
    {
        Q = q;
        var list = await _vehicleService.GetVehiclesAsync();
        var filtered = customerId.HasValue ? list.Where(v => v.CustomerId == customerId.Value) : list;
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            filtered = filtered.Where(v => (v.Vin?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                                        || (v.Model?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        Vehicles = filtered.ToList();
    }
}

