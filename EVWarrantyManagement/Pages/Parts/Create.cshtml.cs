using System.ComponentModel.DataAnnotations;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Parts;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IPartService _partService;

    public CreateModel(IPartService partService)
    {
        _partService = partService;
    }

    [BindProperty]
    public PartInputModel Input { get; set; } = new();

    public class PartInputModel
    {
        [Required]
        public string PartCode { get; set; } = string.Empty;
        [Required]
        public string PartName { get; set; } = string.Empty;
        [Range(0, double.MaxValue)]
        public decimal? UnitPrice { get; set; }
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        // PartCode uniqueness validation
        var existing = await _partService.GetPartByCodeAsync(Input.PartCode);
        if (existing != null)
        {
            ModelState.AddModelError(nameof(Input.PartCode), "Part code already exists.");
            return Page();
        }

        var part = new Part
        {
            PartCode = Input.PartCode,
            PartName = Input.PartName,
            UnitPrice = Input.UnitPrice
        };
        await _partService.CreatePartAsync(part);
        TempData["Success"] = "Part created.";
        return RedirectToPage("Index");
    }
}

