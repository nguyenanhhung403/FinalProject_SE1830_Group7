using System.ComponentModel.DataAnnotations;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Parts;

[Authorize(Policy = "RequireAdmin")]
public class EditModel : PageModel
{
    private readonly IPartService _partService;

    public EditModel(IPartService partService)
    {
        _partService = partService;
    }

    [BindProperty]
    public int Id { get; set; }

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

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var part = await _partService.GetPartAsync(id);
        if (part == null) return RedirectToPage("Index");

        Id = id;
        Input = new PartInputModel
        {
            PartCode = part.PartCode,
            PartName = part.PartName,
            UnitPrice = part.UnitPrice
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var part = await _partService.GetPartAsync(Id);
        if (part == null) return RedirectToPage("Index");

        // Uniqueness check if code changed
        if (!string.Equals(part.PartCode, Input.PartCode, StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _partService.GetPartByCodeAsync(Input.PartCode);
            if (exists != null)
            {
                ModelState.AddModelError(nameof(Input.PartCode), "Part code already exists.");
                return Page();
            }
        }

        part.PartCode = Input.PartCode;
        part.PartName = Input.PartName;
        part.UnitPrice = Input.UnitPrice;

        await _partService.UpdatePartAsync(part);
        TempData["Success"] = "Part updated.";
        return RedirectToPage("Index");
    }
}

