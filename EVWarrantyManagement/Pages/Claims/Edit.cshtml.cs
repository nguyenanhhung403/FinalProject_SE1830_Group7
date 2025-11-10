using System.ComponentModel.DataAnnotations;
using EVWarrantyManagement.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Claims;

[Authorize(Policy = "RequireEVM")]
public class EditModel : PageModel
{
    private readonly IWarrantyClaimService _claimService;

    public EditModel(IWarrantyClaimService claimService)
    {
        _claimService = claimService;
    }

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public ReviewInputModel Input { get; set; } = new();

    public class ReviewInputModel
    {
        [Required]
        public string NewStatus { get; set; } = "Approved";
        public string? Note { get; set; }
        [Range(0, double.MaxValue)]
        public decimal? Cost { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var claim = await _claimService.GetClaimAsync(id);
        if (claim == null) return RedirectToPage("Index");
        Id = id;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        switch (Input.NewStatus)
        {
            case "Approved":
                await _claimService.ApproveClaimAsync(Id, GetUserId(), Input.Note, Input.Cost);
                break;
            case "Rejected":
                await _claimService.RejectClaimAsync(Id, GetUserId(), Input.Note);
                break;
            case "OnHold":
                await _claimService.PutClaimOnHoldAsync(Id, GetUserId(), Input.Note);
                break;
            default:
                ModelState.AddModelError(nameof(Input.NewStatus), "Invalid status.");
                return Page();
        }

        TempData["Success"] = "Claim updated.";
        return RedirectToPage("Index");
    }

    private int GetUserId()
    {
        var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(id, out var userId) ? userId : 0;
    }
}

