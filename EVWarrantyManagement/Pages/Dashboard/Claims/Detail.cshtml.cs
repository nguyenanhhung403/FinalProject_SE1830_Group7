using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;

namespace EVWarrantyManagement.Pages.Dashboard.Claims;

[Authorize(Policy = "RequireEVM")]
public class DetailModel : PageModel
{
    private readonly IWarrantyClaimService _claimService;

    public DetailModel(IWarrantyClaimService claimService)
    {
        _claimService = claimService;
    }

    public WarrantyClaim? Claim { get; private set; }

    [BindProperty]
    public int ClaimId { get; set; }

    [BindProperty]
    public string? Note { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var claim = await _claimService.GetClaimAsync(id);
        if (claim == null)
        {
            return RedirectToPage("/Dashboard/Index", new { error = "Claim not found." });
        }

        Claim = claim;
        ClaimId = claim.ClaimId;
        Note = claim.Note;
        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync()
    {
        var userId = GetUserId();
        await _claimService.ApproveClaimAsync(ClaimId, userId, Note, null);
        TempData["Success"] = $"Claim #{ClaimId} approved.";
        return RedirectToPage("/Dashboard/Index");
    }

    public async Task<IActionResult> OnPostRejectAsync()
    {
        var userId = GetUserId();
        await _claimService.RejectClaimAsync(ClaimId, userId, Note);
        TempData["Success"] = $"Claim #{ClaimId} rejected.";
        return RedirectToPage("/Dashboard/Index");
    }

    public async Task<IActionResult> OnPostHoldAsync()
    {
        var userId = GetUserId();
        await _claimService.PutClaimOnHoldAsync(ClaimId, userId, Note);
        TempData["Success"] = $"Claim #{ClaimId} put on hold.";
        return RedirectToPage("/Dashboard/Index");
    }

    public async Task<IActionResult> OnPostArchiveAsync()
    {
        var userId = GetUserId();
        await _claimService.ArchiveClaimAsync(ClaimId, userId, Note);
        TempData["Success"] = $"Claim #{ClaimId} archived.";
        return RedirectToPage("/Dashboard/Index");
    }

    private int GetUserId()
    {
        var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(id, out var userId) ? userId : 0;
    }
}

