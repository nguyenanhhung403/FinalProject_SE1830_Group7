using System.ComponentModel.DataAnnotations;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Vehicles;

[Authorize]
public class EditModel : PageModel
{
    private readonly IVehicleService _vehicleService;

    public EditModel(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [BindProperty]
    public int Id { get; set; }

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

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var vehicle = await _vehicleService.GetVehicleAsync(id);
        if (vehicle == null) return RedirectToPage("Index");

        Id = id;
        Input = new VehicleInputModel
        {
            Vin = vehicle.Vin,
            Model = vehicle.Model,
            CustomerId = vehicle.CustomerId,
            RegistrationNumber = vehicle.RegistrationNumber
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var vehicle = await _vehicleService.GetVehicleAsync(Id);
        if (vehicle == null) return RedirectToPage("Index");

        vehicle.Vin = Input.Vin;
        vehicle.Model = Input.Model;
        vehicle.CustomerId = Input.CustomerId;
        vehicle.RegistrationNumber = Input.RegistrationNumber;

        await _vehicleService.UpdateVehicleAsync(vehicle);
        TempData["Success"] = "Vehicle updated.";
        return RedirectToPage("Index");
    }
}

