using System;
using System.Linq;
using System.Security.Claims;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace EVWarrantyManagement.Pages.Claims;

[Authorize(Policy = "RequireEVM")]
public class ReviewModel : PageModel
{
    private readonly IWarrantyClaimService _claimService;
    private readonly IHubContext<NotificationHub> _notificationHub;

    public ReviewModel(IWarrantyClaimService claimService, IHubContext<NotificationHub> notificationHub)
    {
        _claimService = claimService;
        _notificationHub = notificationHub;
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
        var claim = await _claimService.GetClaimAsync(claimId);
        await _claimService.ApproveClaimAsync(claimId, GetUserId(), note, null);
        TempData["Success"] = $"Claim #{claimId} approved.";

        var updatedClaim = await _claimService.GetClaimAsync(claimId);
        var updatePayload = BuildClaimUpdatePayload(claimId, updatedClaim, updatedClaim?.StatusCode ?? "Approved", claim?.StatusCode, $"Claim #{claimId} has been approved");

        await BroadcastClaimUpdateAsync(claimId, updatePayload);

        // Send notification to SC Staff and SC Technician
        await _notificationHub.Clients.Groups("SC Staff", "SC Technician", "SC")
            .SendAsync("ReceiveNotification", new
            {
                Type = "info",
                Title = "Claim Approved",
                Message = $"Claim #{claimId} has been approved by EVM" + (updatedClaim != null && !string.IsNullOrEmpty(updatedClaim.Vin) ? $" (VIN: {updatedClaim.Vin})" : ""),
                ClaimId = claimId
            });

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int claimId, string? note)
    {
        var claim = await _claimService.GetClaimAsync(claimId);
        await _claimService.RejectClaimAsync(claimId, GetUserId(), note);
        TempData["Success"] = $"Claim #{claimId} rejected.";

        var rejectedClaim = await _claimService.GetClaimAsync(claimId);
        var rejectPayload = BuildClaimUpdatePayload(claimId, rejectedClaim, rejectedClaim?.StatusCode ?? "Rejected", claim?.StatusCode, $"Claim #{claimId} has been rejected");

        await BroadcastClaimUpdateAsync(claimId, rejectPayload);

        // Send notification to SC Staff and SC Technician
        await _notificationHub.Clients.Groups("SC Staff", "SC Technician", "SC")
            .SendAsync("ReceiveNotification", new
            {
                Type = "warning",
                Title = "Claim Rejected",
                Message = $"Claim #{claimId} has been rejected by EVM" + (rejectedClaim != null && !string.IsNullOrEmpty(rejectedClaim.Vin) ? $" (VIN: {rejectedClaim.Vin})" : ""),
                ClaimId = claimId
            });

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostHoldAsync(int claimId, string? note)
    {
        var claim = await _claimService.GetClaimAsync(claimId);
        await _claimService.PutClaimOnHoldAsync(claimId, GetUserId(), note);
        TempData["Success"] = $"Claim #{claimId} placed on hold.";

        var holdClaim = await _claimService.GetClaimAsync(claimId);
        var holdPayload = BuildClaimUpdatePayload(claimId, holdClaim, holdClaim?.StatusCode ?? "OnHold", claim?.StatusCode, $"Claim #{claimId} has been put on hold");

        await BroadcastClaimUpdateAsync(claimId, holdPayload);

        // Send notification to SC Staff and SC Technician
        await _notificationHub.Clients.Groups("SC Staff", "SC Technician", "SC")
            .SendAsync("ReceiveNotification", new
            {
                Type = "warning",
                Title = "Claim On Hold",
                Message = $"Claim #{claimId} has been put on hold by EVM" + (holdClaim != null && !string.IsNullOrEmpty(holdClaim.Vin) ? $" (VIN: {holdClaim.Vin})" : ""),
                ClaimId = claimId
            });

        return RedirectToPage();
    }

    private int GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var userId) ? userId : 0;
    }

    private object BuildClaimUpdatePayload(int claimId, WarrantyClaim? claim, string newStatus, string? oldStatus, string message)
    {
        var resolvedStatus = string.IsNullOrWhiteSpace(newStatus)
            ? (claim?.StatusCode ?? string.Empty)
            : newStatus;

        return new
        {
            ClaimId = claim?.ClaimId ?? claimId,
            Type = "status_change",
            NewStatus = resolvedStatus,
            OldStatus = oldStatus,
            Message = message,
            StatusCode = resolvedStatus,
            Vin = claim?.Vin,
            VehicleModel = claim?.Vehicle?.Model,
            Description = claim?.Description,
            ServiceCenterName = claim?.ServiceCenter?.Name,
            DateDiscovered = claim != null ? claim.DateDiscovered.ToString("yyyy-MM-dd") : null,
            TechnicianId = claim?.TechnicianId,
            TechnicianName = claim?.Technician?.FullName,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private Task BroadcastClaimUpdateAsync(int claimId, object payload)
    {
        var roleGroups = new[] { "Admin", "EVM Staff", "EVM", "SC Staff", "SC Technician", "SC" };

        return Task.WhenAll(
            _notificationHub.Clients.Group($"Claim_{claimId}").SendAsync("ReceiveClaimUpdate", payload),
            _notificationHub.Clients.Groups(roleGroups).SendAsync("ReceiveClaimUpdate", payload)
        );
    }
}

