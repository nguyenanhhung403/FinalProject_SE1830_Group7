using System.Linq;
using System.Security.Claims;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Claims;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IWarrantyClaimService _claimService;

    public IndexModel(IWarrantyClaimService claimService)
    {
        _claimService = claimService;
    }

    public IReadOnlyList<WarrantyClaim> Claims { get; private set; } = Array.Empty<WarrantyClaim>();
    public string? Q { get; private set; }
    public string? Status { get; private set; }
    public string[] StatusOptions { get; } = new[] { "Pending","Approved","Rejected","OnHold","InProgress","Completed","Closed" };

    public async Task OnGetAsync(string? q, string? status)
    {
        Q = q;
        Status = status;
        var isAdmin = User.IsInRole("Admin");
        var isEvm = User.IsInRole("EVM Staff") || User.IsInRole("EVM");
        var isScStaff = User.IsInRole("SC Staff");
        var isScTechnician = User.IsInRole("SC Technician") || (!isScStaff && User.IsInRole("SC"));
        var all = await _claimService.GetAllClaimsAsync();

        if (isAdmin)
        {
            Claims = all;
        }
        else if (isEvm)
        {
            Claims = all.Where(c => c.StatusCode == "Pending" || c.StatusCode == "OnHold").ToList();
        }
        else if (isScStaff)
        {
            var userId = GetUserId();
            Claims = all.Where(c => c.CreatedByUserId == userId).ToList();
        }
        else if (isScTechnician)
        {
            Claims = all.Where(c =>
                    string.Equals(c.StatusCode, "Approved", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.StatusCode, "InProgress", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.StatusCode, "Completed", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (string.IsNullOrWhiteSpace(Status))
            {
                Claims = Claims.Where(c =>
                    !string.Equals(c.StatusCode, "Completed", StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }
        else
        {
            // SC / Technician: show claims created by this user
            var userId = GetUserId();
            Claims = all.Where(c => c.CreatedByUserId == userId).ToList();
        }

        // Apply filters
        if (!string.IsNullOrWhiteSpace(Status))
        {
            Claims = Claims.Where(c => string.Equals(c.StatusCode, Status, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        if (!string.IsNullOrWhiteSpace(Q))
        {
            var qq = Q.Trim();
            Claims = Claims.Where(c =>
                (c.Vehicle?.Model?.Contains(qq, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.Vin?.Contains(qq, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.Description?.Contains(qq, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        }
    }

    public async Task<IActionResult> OnPostApproveAsync(int claimId)
    {
        if (!User.IsInRole("EVM Staff") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }
        await _claimService.ApproveClaimAsync(claimId, GetUserId(), "Approved", null);
        TempData["Success"] = $"Claim #{claimId} approved.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int claimId)
    {
        if (!User.IsInRole("EVM Staff") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }
        await _claimService.RejectClaimAsync(claimId, GetUserId(), "Rejected");
        TempData["Success"] = $"Claim #{claimId} rejected.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostOnHoldAsync(int claimId)
    {
        if (!User.IsInRole("EVM Staff") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }
        await _claimService.PutClaimOnHoldAsync(claimId, GetUserId(), "On hold");
        TempData["Success"] = $"Claim #{claimId} put on hold.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCompleteAsync(int claimId, string? technicianNote)
    {
        if (!User.IsInRole("SC Technician") && !User.IsInRole("SC") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }
        var claim = await _claimService.GetClaimAsync(claimId);
        if (claim is null)
        {
            TempData["Error"] = "Claim not found.";
            return RedirectToPage();
        }
        if (!string.Equals(claim.StatusCode, "InProgress", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Claim must be InProgress before completion.";
            return RedirectToPage();
        }
        var note = string.IsNullOrWhiteSpace(technicianNote) ? "Work completed" : technicianNote;
        await _claimService.CompleteClaimAsync(claimId, GetUserId(), DateOnly.FromDateTime(DateTime.UtcNow), note);
        TempData["Success"] = $"Claim #{claimId} marked completed.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostStartAsync(int claimId, string? technicianNote)
    {
        if (!User.IsInRole("SC Technician") && !User.IsInRole("SC") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }
        var claim = await _claimService.GetClaimAsync(claimId);
        if (claim is null)
        {
            TempData["Error"] = "Claim not found.";
            return RedirectToPage();
        }
        if (!string.Equals(claim.StatusCode, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Claim must be Approved before starting repair.";
            return RedirectToPage();
        }
        var note = string.IsNullOrWhiteSpace(technicianNote) ? "Start repair" : technicianNote;
        await _claimService.StartRepairAsync(claimId, GetUserId(), note);
        TempData["Success"] = $"Claim #{claimId} set to InProgress.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostArchiveAsync(int claimId)
    {
        if (!User.IsInRole("Admin"))
        {
            return Forbid();
        }
        var claim = await _claimService.GetClaimAsync(claimId);
        if (claim is null)
        {
            TempData["Error"] = "Claim not found.";
            return RedirectToPage();
        }
        if (!string.Equals(claim.StatusCode, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only completed claims can be archived.";
            return RedirectToPage();
        }
        await _claimService.ArchiveClaimAsync(claimId, GetUserId(), "Archived");
        TempData["Success"] = $"Claim #{claimId} archived.";
        return RedirectToPage();
    }

    private int GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var userId) ? userId : 0;
    }
}

