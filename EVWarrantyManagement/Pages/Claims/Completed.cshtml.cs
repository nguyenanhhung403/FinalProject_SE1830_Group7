using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Claims;

[Authorize(Policy = "RequireEVM")]
public class CompletedModel : PageModel
{
    private readonly IWarrantyClaimService _claimService;

    public CompletedModel(IWarrantyClaimService claimService)
    {
        _claimService = claimService;
    }

    public IReadOnlyList<WarrantyClaim> CompletedClaims { get; private set; } = Array.Empty<WarrantyClaim>();

    public async Task OnGetAsync()
    {
        var allClaims = await _claimService.GetAllClaimsAsync();
        CompletedClaims = allClaims.Where(c => c.StatusCode == "Completed").OrderByDescending(c => c.CompletionDate).ToList();
    }

    public async Task<IActionResult> OnPostArchiveAsync(int claimId)
    {
        await _claimService.ArchiveClaimAsync(claimId, GetUserId(), "Archived from Completed list");
        TempData["Success"] = $"Claim #{claimId} archived.";
        return RedirectToPage();
    }

    private int GetUserId()
    {
        var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(id, out var userId) ? userId : 0;
    }
}

