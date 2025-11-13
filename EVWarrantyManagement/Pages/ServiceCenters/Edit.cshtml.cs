using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace EVWarrantyManagement.Pages.ServiceCenters;

[Authorize(Policy = "RequireAdmin")]
public class EditModel : PageModel
{
    private readonly IServiceCenterService _serviceCenterService;
    private readonly IHubContext<NotificationHub> _notificationHub;

    public EditModel(IServiceCenterService serviceCenterService, IHubContext<NotificationHub> notificationHub)
    {
        _serviceCenterService = serviceCenterService;
        _notificationHub = notificationHub;
    }

    [BindProperty]
    public ServiceCenter ServiceCenter { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var serviceCenter = await _serviceCenterService.GetServiceCenterByIdAsync(id);
        if (serviceCenter == null)
        {
            TempData["Error"] = "Service center not found.";
            return RedirectToPage("Index");
        }

        ServiceCenter = serviceCenter;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await _serviceCenterService.UpdateServiceCenterAsync(ServiceCenter);
            
            // Send notification
            var notificationPayload = new
            {
                Type = "servicecenter_updated",
                Title = "Service Center Updated",
                Message = $"Service center {ServiceCenter.Name} has been updated",
                ServiceCenterId = ServiceCenter.ServiceCenterId,
                ServiceCenterName = ServiceCenter.Name
            };

            await _notificationHub.Clients.Groups("Admin", "EVM Staff", "EVM")
                .SendAsync("ReceiveNotification", notificationPayload);

            await _notificationHub.Clients.Group($"ServiceCenter_{ServiceCenter.ServiceCenterId}")
                .SendAsync("ReceiveServiceCenterUpdate", notificationPayload);

            TempData["Success"] = "Service center updated successfully.";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error updating service center: {ex.Message}";
            return Page();
        }
    }
}

