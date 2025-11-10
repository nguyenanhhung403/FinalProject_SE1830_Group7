using System.Linq;
using System.Security.Claims;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Claims;

[Authorize(Policy = "RequireEVM")]
public class ReviewModel : PageModel
{
    private readonly IWarrantyClaimService _claimService;

    public ReviewModel(IWarrantyClaimService claimService)
    {
        _claimService = claimService;
    }

    public IReadOnlyList<WarrantyClaim> Claims { get; private set; } = Array.Empty<WarrantyClaim>();
    public string? Q { get; private set; }

    public async Task OnGetAsync(string? q)
    {
        Q = q?.Trim();
        var pendingClaims = await _claimService.GetPendingClaimsAsync();
        var filtered = pendingClaims.Where(c => c.StatusCode == "Pending" || c.StatusCode == "OnHold");

        if (!string.IsNullOrWhiteSpace(Q))
        {
            filtered = filtered.Where(c =>
                (c.Vehicle?.Model?.Contains(Q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.Vin?.Contains(Q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.Description?.Contains(Q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        Claims = filtered.OrderByDescending(c => c.CreatedAt).ToList();
    }

    public async Task<IActionResult> OnPostApproveAsync(int claimId, string? note)
    {
        await _claimService.ApproveClaimAsync(claimId, GetUserId(), note, null);
        TempData["Success"] = $"Claim #{claimId} approved.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int claimId, string? note)
    {
        await _claimService.RejectClaimAsync(claimId, GetUserId(), note);
        TempData["Success"] = $"Claim #{claimId} rejected.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostHoldAsync(int claimId, string? note)
    {
        await _claimService.PutClaimOnHoldAsync(claimId, GetUserId(), note);
        TempData["Success"] = $"Claim #{claimId} placed on hold.";
        return RedirectToPage();
    }

    private int GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var userId) ? userId : 0;
    }
}

