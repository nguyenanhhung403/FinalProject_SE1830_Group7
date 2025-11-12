using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EVWarrantyManagement.Pages.Claims;

[Authorize(Policy = "RequireSCStaff")]
public class CreateModel : PageModel
{
    private readonly IWarrantyClaimService _claimService;
    private readonly IWebHostEnvironment _env;
    private readonly IVehicleService _vehicleService;
    private readonly EVWarrantyManagement.DAL.EVWarrantyManagementContext _db;

    public CreateModel(IWarrantyClaimService claimService, IWebHostEnvironment env, IVehicleService vehicleService, EVWarrantyManagement.DAL.EVWarrantyManagementContext db)
    {
        _claimService = claimService;
        _env = env;
        _vehicleService = vehicleService;
        _db = db;
    }

    [BindProperty]
    public CreateInputModel Input { get; set; } = new();

    [BindProperty]
    public IFormFile? Image { get; set; }

    public Microsoft.AspNetCore.Mvc.Rendering.SelectList? ServiceCenterOptions { get; private set; }
    public IEnumerable<VehicleOptionViewModel> VehicleOptions { get; private set; } = Enumerable.Empty<VehicleOptionViewModel>();

    public class CreateInputModel
    {
        [Required]
        [Display(Name = "Vehicle Model")]
        public int VehicleId { get; set; }

        [Required]
        [Display(Name = "Date discovered")]
        public DateOnly DateDiscovered { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Service Center")]
        public int ServiceCenterId { get; set; }
    }

    public record VehicleOptionViewModel(int VehicleId, string Display, string? Vin);

    public async Task OnGetAsync()
    {
        await LoadLookupsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return Page();
        }

        var vehicle = await _vehicleService.GetVehicleAsync(Input.VehicleId);
        if (vehicle is null)
        {
            ModelState.AddModelError(nameof(Input.VehicleId), "Selected vehicle not found.");
            await LoadLookupsAsync();
            return Page();
        }

        string? imageUrl = null;
        if (Image != null && Image.Length > 0)
        {
            var uploads = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
            Directory.CreateDirectory(uploads);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(Image.FileName)}";
            var fullPath = Path.Combine(uploads, fileName);
            using var stream = System.IO.File.Create(fullPath);
            await Image.CopyToAsync(stream);
            imageUrl = $"/uploads/{fileName}";
        }

        var claim = new WarrantyClaim
        {
            Vin = vehicle.Vin,
            VehicleId = vehicle.VehicleId,
            Description = Input.Description,
            DateDiscovered = Input.DateDiscovered,
            ImageUrl = imageUrl,
            ServiceCenterId = Input.ServiceCenterId
        };

        var userId = GetUserId();
        await _claimService.CreateClaimAsync(claim, userId, "Claim created");
        TempData["Success"] = "Claim created successfully.";
        return RedirectToPage("Index");
    }

    private async Task LoadLookupsAsync()
    {
        var vehicles = await _vehicleService.GetVehiclesAsync();
        VehicleOptions = vehicles
            .OrderBy(v => v.Model)
            .ThenBy(v => v.Vin)
            .Select(v => new VehicleOptionViewModel(
                v.VehicleId,
                string.IsNullOrWhiteSpace(v.Model) ? v.Vin : $"{v.Model} ({v.Vin})",
                v.Vin))
            .ToList();

        var centers = await _db.ServiceCenters
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new { c.ServiceCenterId, c.Name })
            .ToListAsync();
        ServiceCenterOptions = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(centers, "ServiceCenterId", "Name");
    }

    private int GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var userId) ? userId : 0;
    }
}

