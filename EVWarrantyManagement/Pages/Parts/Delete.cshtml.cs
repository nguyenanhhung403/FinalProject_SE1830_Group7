using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace EVWarrantyManagement.Pages.Parts;

[Authorize(Policy = "RequireAdmin")]
public class DeleteModel : PageModel
{
    private readonly IPartService _partService;
    private readonly IHubContext<NotificationHub> _notificationHub;

    public DeleteModel(IPartService partService, IHubContext<NotificationHub> notificationHub)
    {
        _partService = partService;
        _notificationHub = notificationHub;
    }

    [BindProperty]
    public int Id { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Id = id;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Get part info before deleting
        var part = await _partService.GetPartAsync(Id);
        var partName = part?.PartName ?? "Part";
        var partCode = part?.PartCode ?? "";

        await _partService.DeletePartAsync(Id);
        
        // Send notification
        var notificationPayload = new
        {
            Type = "part_deleted",
            Title = "Part Deleted",
            Message = $"Part {partName} ({partCode}) has been deleted",
            PartId = Id,
            PartName = partName,
            PartCode = partCode
        };

        await _notificationHub.Clients.Groups("Admin", "EVM Staff", "EVM")
            .SendAsync("ReceiveNotification", notificationPayload);

        await _notificationHub.Clients.Group($"Part_{Id}")
            .SendAsync("ReceivePartUpdate", notificationPayload);

        TempData["Success"] = "Part deleted.";
        return RedirectToPage("Index");
    }
}

