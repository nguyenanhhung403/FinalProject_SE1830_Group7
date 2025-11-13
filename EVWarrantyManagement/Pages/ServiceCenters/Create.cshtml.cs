using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace EVWarrantyManagement.Pages.ServiceCenters;

[Authorize(Policy = "RequireAdmin")]
public class CreateModel : PageModel
{
    private readonly IServiceCenterService _serviceCenterService;
    private readonly IHubContext<NotificationHub> _notificationHub;

    public CreateModel(IServiceCenterService serviceCenterService, IHubContext<NotificationHub> notificationHub)
    {
        _serviceCenterService = serviceCenterService;
        _notificationHub = notificationHub;
    }

    [BindProperty]
    public ServiceCenter ServiceCenter { get; set; } = new();

    public IActionResult OnGet()
    {
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
            var createdServiceCenter = await _serviceCenterService.CreateServiceCenterAsync(ServiceCenter);
            
            // Send notification
            var notificationPayload = new
            {
                Type = "servicecenter_created",
                Title = "New Service Center Created",
                Message = $"New service center {ServiceCenter.Name} has been created",
                ServiceCenterId = createdServiceCenter.ServiceCenterId,
                ServiceCenterName = ServiceCenter.Name
            };

            await _notificationHub.Clients.Groups("Admin", "EVM Staff", "EVM")
                .SendAsync("ReceiveNotification", notificationPayload);

            await _notificationHub.Clients.Group($"ServiceCenter_{createdServiceCenter.ServiceCenterId}")
                .SendAsync("ReceiveServiceCenterUpdate", notificationPayload);

            TempData["Success"] = "Service center created successfully.";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error creating service center: {ex.Message}";
            return Page();
        }
    }
}

