using System.ComponentModel.DataAnnotations;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Vehicles;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IVehicleService _vehicleService;

    public CreateModel(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [BindProperty]
    public VehicleInputModel Input { get; set; } = new();

    public class VehicleInputModel
    {
        [Required]
        public string Vin { get; set; } = string.Empty;
        public string? Model { get; set; }
        public int? CustomerId { get; set; }
        public string? RegistrationNumber { get; set; }
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var vehicle = new Vehicle
        {
            Vin = Input.Vin,
            Model = Input.Model,
            CustomerId = Input.CustomerId,
            RegistrationNumber = Input.RegistrationNumber
        };
        await _vehicleService.CreateVehicleAsync(vehicle);
        TempData["Success"] = "Vehicle created.";
        return RedirectToPage("Index");
    }
}

