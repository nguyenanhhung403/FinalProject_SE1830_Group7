using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using EVWarrantyManagement.UI.Hubs;

namespace EVWarrantyManagement.Pages.Claims;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IWarrantyClaimService _claimService;
    private readonly IPartService _partService;
    private readonly IHubContext<NotificationHub> _notificationHub;

    public DetailsModel(IWarrantyClaimService claimService, IPartService partService, IHubContext<NotificationHub> notificationHub)
    {
        _claimService = claimService;
        _partService = partService;
        _notificationHub = notificationHub;
    }

    [BindProperty]
    public int ClaimId { get; set; }

    public WarrantyClaim? Claim { get; private set; }
    public IEnumerable<PartOptionViewModel> PartOptions { get; private set; } = Enumerable.Empty<PartOptionViewModel>();

    [BindProperty]
    public AddPartInputModel AddPartInput { get; set; } = new();

    public class AddPartInputModel
    {
        [Required]
        public int PartId { get; set; }
        [Range(1, 999)]
        public int Quantity { get; set; } = 1;
        [Range(0, double.MaxValue)]
        public decimal? PartCost { get; set; }
    }

    public record PartOptionViewModel(int PartId, string Display, decimal UnitPrice);

    public async Task<IActionResult> OnGetAsync(int id)
    {
        ClaimId = id;
        Claim = await _claimService.GetClaimAsync(id);
        if (Claim == null) return RedirectToPage("Index");
        await HydrateUsedPartsAsync(Claim.UsedParts);
        await LoadLookupsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddPartAsync()
    {
        if (!ModelState.IsValid) return await Reload();

        var claim = await _claimService.GetClaimAsync(ClaimId);
        if (claim is null)
        {
            TempData["Error"] = "Claim not found.";
            return RedirectToPage(new { id = ClaimId });
        }
        if (string.Equals(claim.StatusCode, "Completed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(claim.StatusCode, "Archived", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Cannot add parts to a completed or archived claim.";
            return RedirectToPage(new { id = ClaimId });
        }

        if (AddPartInput.PartId <= 0)
        {
            ModelState.AddModelError(nameof(AddPartInput.PartId), "Please select a part.");
            return await Reload();
        }

        var part = await _partService.GetPartAsync(AddPartInput.PartId);
        if (part is null)
        {
            ModelState.AddModelError(nameof(AddPartInput.PartId), "Selected part not found.");
            return await Reload();
        }

        var quantity = Math.Max(1, AddPartInput.Quantity);
        decimal totalCost = AddPartInput.PartCost ?? part.UnitPrice ?? 0m;
        if (totalCost <= 0 && part.UnitPrice.HasValue)
        {
            totalCost = part.UnitPrice.Value * quantity;
        }
        var perUnitCost = quantity > 0 ? Math.Round(totalCost / quantity, 2) : totalCost;
        if (perUnitCost <= 0)
        {
            ModelState.AddModelError(nameof(AddPartInput.PartCost), "Cost must be greater than zero.");
            return await Reload();
        }

        var usedPart = new UsedPart
        {
            ClaimId = ClaimId,
            PartId = AddPartInput.PartId,
            Quantity = quantity,
            PartCost = perUnitCost
        };
        await _claimService.AddUsedPartAsync(usedPart, GetUserId());
        TempData["Success"] = "Part added.";
        return RedirectToPage(new { id = ClaimId });
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        if (!User.IsInRole("EVM Staff") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var claim = await _claimService.GetClaimAsync(id);
        await _claimService.ApproveClaimAsync(id, GetUserId(), "Approved", null);

        // Send real-time notification
        var notificationData = new
        {
            ClaimId = id,
            NewStatus = "Approved",
            Message = $"Claim #{id} has been approved",
            Type = "status_change"
        };
        await _notificationHub.Clients.Groups("SC Technician", "SC Staff", "SC")
            .SendAsync("ReceiveClaimUpdate", notificationData);

        TempData["Success"] = "Approved.";
        return RedirectToPage(new { id });
    }
    public async Task<IActionResult> OnPostRejectAsync(int id)
    {
        if (!User.IsInRole("EVM Staff") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        await _claimService.RejectClaimAsync(id, GetUserId(), "Rejected");

        // Send real-time notification
        var notificationData = new
        {
            ClaimId = id,
            NewStatus = "Rejected",
            Message = $"Claim #{id} has been rejected",
            Type = "status_change"
        };
        await _notificationHub.Clients.Groups("SC Technician", "SC Staff", "SC")
            .SendAsync("ReceiveClaimUpdate", notificationData);

        TempData["Success"] = "Rejected.";
        return RedirectToPage(new { id });
    }
    public async Task<IActionResult> OnPostOnHoldAsync(int id)
    {
        if (!User.IsInRole("EVM Staff") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }
        await _claimService.PutClaimOnHoldAsync(id, GetUserId(), "On hold");
        TempData["Success"] = "On hold.";
        return RedirectToPage(new { id });
    }
    public async Task<IActionResult> OnPostCompleteAsync(int id, string? technicianNote)
    {
        if (!User.IsInRole("SC Technician") && !User.IsInRole("SC") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }
        var claim = await _claimService.GetClaimAsync(id);
        if (claim is null)
        {
            TempData["Error"] = "Claim not found.";
            return RedirectToPage(new { id });
        }
        if (!string.Equals(claim.StatusCode, "InProgress", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Claim must be InProgress before completion.";
            return RedirectToPage(new { id });
        }
        var note = string.IsNullOrWhiteSpace(technicianNote) ? "Work completed" : technicianNote;
        await _claimService.CompleteClaimAsync(id, GetUserId(), DateOnly.FromDateTime(DateTime.UtcNow), note);

        // Send real-time notification
        var notificationData = new
        {
            ClaimId = id,
            NewStatus = "Completed",
            Message = $"Claim #{id} repair has been completed",
            Type = "status_change"
        };
        await _notificationHub.Clients.Groups("EVM Staff", "Admin", "SC Staff")
            .SendAsync("ReceiveClaimUpdate", notificationData);

        TempData["Success"] = "Completed.";
        return RedirectToPage(new { id });
    }
    public async Task<IActionResult> OnPostStartAsync(int id, string? technicianNote)
    {
        if (!User.IsInRole("SC Technician") && !User.IsInRole("SC") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }
        var claim = await _claimService.GetClaimAsync(id);
        if (claim is null)
        {
            TempData["Error"] = "Claim not found.";
            return RedirectToPage(new { id });
        }
        if (!string.Equals(claim.StatusCode, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Claim must be Approved before starting repair.";
            return RedirectToPage(new { id });
        }
        var note = string.IsNullOrWhiteSpace(technicianNote) ? "Start repair" : technicianNote;
        await _claimService.StartRepairAsync(id, GetUserId(), note);

        // Send real-time notification
        var notificationData = new
        {
            ClaimId = id,
            NewStatus = "InProgress",
            Message = $"Claim #{id} repair has started",
            Type = "status_change"
        };
        await _notificationHub.Clients.Groups("EVM Staff", "Admin", "SC Staff")
            .SendAsync("ReceiveClaimUpdate", notificationData);

        TempData["Success"] = "Started repair.";
        return RedirectToPage(new { id });
    }
    public async Task<IActionResult> OnPostArchiveAsync(int id)
    {
        if (!User.IsInRole("Admin"))
        {
            return Forbid();
        }
        var claim = await _claimService.GetClaimAsync(id);
        if (claim is null)
        {
            TempData["Error"] = "Claim not found.";
            return RedirectToPage(new { id });
        }
        if (!string.Equals(claim.StatusCode, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only completed claims can be archived.";
            return RedirectToPage(new { id });
        }
        await _claimService.ArchiveClaimAsync(id, GetUserId(), "Archived");
        TempData["Success"] = "Archived.";
        return RedirectToPage(new { id });
    }

    private async Task<IActionResult> Reload()
    {
        Claim = await _claimService.GetClaimAsync(ClaimId);
        await HydrateUsedPartsAsync(Claim?.UsedParts);
        await LoadLookupsAsync();
        return Page();
    }

    private async Task LoadLookupsAsync()
    {
        var parts = await _partService.GetPartsAsync();
        PartOptions = parts
            .OrderBy(p => p.PartName)
            .Select(p => new PartOptionViewModel(
                p.PartId,
                string.IsNullOrWhiteSpace(p.PartCode) ? p.PartName ?? $"Part #{p.PartId}" : $"{p.PartCode} - {p.PartName}",
                p.UnitPrice ?? 0m))
            .ToList();
    }

    private async Task HydrateUsedPartsAsync(IEnumerable<UsedPart>? parts)
    {
        if (parts is null)
        {
            return;
        }

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

    private int GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var userId) ? userId : 0;
    }
}

