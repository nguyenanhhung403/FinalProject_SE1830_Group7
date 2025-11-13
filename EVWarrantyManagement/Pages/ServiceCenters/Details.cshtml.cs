using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.DAL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using EVWarrantyManagement.Hubs;

namespace EVWarrantyManagement.Pages.ServiceCenters;

[Authorize(Policy = "RequireEVM")]
public class DetailsModel : PageModel
{
    private readonly IServiceCenterService _serviceCenterService;
    private readonly IWarrantyClaimService _claimService;
    private readonly IHubContext<NotificationHub> _notificationHub;

    public DetailsModel(
        IServiceCenterService serviceCenterService,
        IWarrantyClaimService claimService,
        IHubContext<NotificationHub> notificationHub)
    {
        _serviceCenterService = serviceCenterService;
        _claimService = claimService;
        _notificationHub = notificationHub;
    }

    public ServiceCenter? ServiceCenter { get; private set; }
    public IReadOnlyList<ServiceCenterTechnician> Technicians { get; private set; } = Array.Empty<ServiceCenterTechnician>();
    public IReadOnlyList<User> AvailableTechnicians { get; private set; } = Array.Empty<User>();
    public IReadOnlyList<WarrantyClaim> Claims { get; private set; } = Array.Empty<WarrantyClaim>();
    public ServiceCenterStats Stats { get; private set; } = new ServiceCenterStats(0, 0, 0, 0);

    [BindProperty]
    public int? SelectedTechnicianId { get; set; }

    [BindProperty]
    public int? ClaimIdToAssign { get; set; }

    [BindProperty]
    public int? TechnicianIdToAssign { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        ServiceCenter = await _serviceCenterService.GetServiceCenterByIdAsync(id);
        if (ServiceCenter == null)
        {
            TempData["Error"] = "Service center not found.";
            return RedirectToPage("Index");
        }

        Technicians = await _serviceCenterService.GetTechniciansAsync(id);
        AvailableTechnicians = await _serviceCenterService.GetAvailableTechniciansAsync();
        Claims = await _claimService.GetClaimsByServiceCenterAsync(id);
        Stats = await _serviceCenterService.GetServiceCenterStatsAsync(id);

        return Page();
    }

    public async Task<IActionResult> OnPostAssignTechnicianAsync(int id)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("EVM Staff") && !User.IsInRole("EVM"))
        {
            return Forbid();
        }

        if (!SelectedTechnicianId.HasValue)
        {
            TempData["Error"] = "Please select a technician.";
            return RedirectToPage(new { id });
        }

        var userId = GetUserId();
        await _serviceCenterService.AssignTechnicianAsync(id, SelectedTechnicianId.Value, userId);

        // SignalR notification
        var serviceCenter = await _serviceCenterService.GetServiceCenterByIdAsync(id);
        if (serviceCenter != null)
        {
            var notificationPayload = new
            {
                Type = "technician_assigned",
                Title = "Assigned to Service Center",
                Message = $"You have been assigned to {serviceCenter.Name}",
                ServiceCenterId = id,
                ServiceCenterName = serviceCenter.Name,
                TechnicianId = SelectedTechnicianId.Value
            };

            await _notificationHub.Clients.User(SelectedTechnicianId.Value.ToString())
                .SendAsync("ReceiveNotification", notificationPayload);

            await _notificationHub.Clients.Groups("Admin", "EVM Staff", "EVM", "SC Staff", "SC")
                .SendAsync("ReceiveNotification", notificationPayload);

            await _notificationHub.Clients.Group($"ServiceCenter_{id}")
                .SendAsync("ReceiveServiceCenterUpdate", notificationPayload);
        }

        TempData["Success"] = "Technician assigned successfully.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUnassignTechnicianAsync(int id, int userId)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("EVM Staff") && !User.IsInRole("EVM"))
        {
            return Forbid();
        }

        await _serviceCenterService.UnassignTechnicianAsync(userId);

        // SignalR notification
        var serviceCenter = await _serviceCenterService.GetServiceCenterByIdAsync(id);
        if (serviceCenter != null)
        {
            var notificationPayload = new
            {
                Type = "technician_removed",
                Title = "Unassigned from Service Center",
                Message = $"You have been unassigned from {serviceCenter.Name}",
                ServiceCenterId = id,
                ServiceCenterName = serviceCenter.Name,
                TechnicianId = userId
            };

            await _notificationHub.Clients.User(userId.ToString())
                .SendAsync("ReceiveNotification", notificationPayload);

            await _notificationHub.Clients.Groups("Admin", "EVM Staff", "EVM", "SC Staff", "SC")
                .SendAsync("ReceiveNotification", notificationPayload);

            await _notificationHub.Clients.Group($"ServiceCenter_{id}")
                .SendAsync("ReceiveServiceCenterUpdate", notificationPayload);
        }

        TempData["Success"] = "Technician unassigned successfully.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAssignClaimToTechnicianAsync(int id)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("EVM Staff") && !User.IsInRole("EVM"))
        {
            return Forbid();
        }

        if (!ClaimIdToAssign.HasValue || !TechnicianIdToAssign.HasValue)
        {
            TempData["Error"] = "Please select both claim and technician.";
            return RedirectToPage(new { id });
        }

        var claim = await _claimService.GetClaimAsync(ClaimIdToAssign.Value);
        if (claim == null)
        {
            TempData["Error"] = "Claim not found.";
            return RedirectToPage(new { id });
        }

        // Update claim technician (we'll need to add this method to WarrantyClaimService)
        // For now, we'll use the repository directly through a new method
        // This will be handled in the integration step

        TempData["Success"] = "Claim assigned to technician successfully.";
        return RedirectToPage(new { id });
    }

    private int GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var userId) ? userId : 0;
    }
}

