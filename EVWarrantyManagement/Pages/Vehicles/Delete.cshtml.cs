using EVWarrantyManagement.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Vehicles;

[Authorize]
public class DeleteModel : PageModel
{
    private readonly IVehicleService _vehicleService;

    public DeleteModel(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [BindProperty]
    public int Id { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Id = id;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _vehicleService.DeleteVehicleAsync(Id);
        TempData["Success"] = "Vehicle deleted.";
        return RedirectToPage("Index");
    }
}

