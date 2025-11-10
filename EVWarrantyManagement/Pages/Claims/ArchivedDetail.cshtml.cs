using System.Collections.Generic;
using System.Linq;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Claims;

[Authorize]
public class ArchivedDetailModel : PageModel
{
    private readonly IWarrantyClaimService _claimService;
    private readonly IPartService _partService;

    public ArchivedDetailModel(IWarrantyClaimService claimService, IPartService partService)
    {
        _claimService = claimService;
        _partService = partService;
    }

    public WarrantyHistory? History { get; private set; }
    public IReadOnlyList<UsedPart> UsedParts { get; private set; } = Array.Empty<UsedPart>();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("EVM Staff") && !User.IsInRole("EVM") &&
            !User.IsInRole("SC Technician") && !User.IsInRole("SC") && !User.IsInRole("SC Staff"))
        {
            return Forbid();
        }

        var history = await _claimService.GetArchivedClaimAsync(id);
        if (history == null)
        {
            TempData["Error"] = "Archived claim not found.";
            return RedirectToPage("Archived");
        }

        if ((User.IsInRole("SC Technician") || User.IsInRole("SC")) &&
            history.CompletedByUserId != GetUserId())
        {
            return Forbid();
        }

        if (User.IsInRole("SC Staff"))
        {
            var creatorClaim = await _claimService.GetClaimAsync(history.ClaimId);
            if (creatorClaim?.CreatedByUserId != GetUserId())
            {
                return Forbid();
            }
        }

        History = history;

        var archivedClaim = await _claimService.GetClaimAsync(history.ClaimId);
        UsedParts = archivedClaim?.UsedParts
            .OrderByDescending(p => p.CreatedAt)
            .ToList()
            ?? new List<UsedPart>();
        await HydrateUsedPartsAsync(UsedParts);

        return Page();
    }

    private int GetUserId()
    {
        var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(id, out var userId) ? userId : 0;
    }

    private async Task HydrateUsedPartsAsync(IEnumerable<UsedPart> parts)
    {
        var missingIds = parts
            .Where(p => p.Part is null)
            .Select(p => p.PartId)
            .Distinct()
            .ToList();

        foreach (var id in missingIds)
        {
            var part = await _partService.GetPartAsync(id);
            foreach (var usedPart in parts.Where(p => p.PartId == id))
            {
                usedPart.Part = part ?? usedPart.Part;
            }
        }
    }
}

