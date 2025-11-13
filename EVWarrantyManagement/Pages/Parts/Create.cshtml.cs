using System.ComponentModel.DataAnnotations;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace EVWarrantyManagement.Pages.Parts;

[Authorize(Policy = "RequireAdmin")]
public class CreateModel : PageModel
{
    private readonly IPartService _partService;
    private readonly IHubContext<NotificationHub> _notificationHub;

    public CreateModel(IPartService partService, IHubContext<NotificationHub> notificationHub)
    {
        _partService = partService;
        _notificationHub = notificationHub;
    }

    [BindProperty]
    public PartInputModel Input { get; set; } = new();

    public class PartInputModel
    {
        [Required]
        public string PartCode { get; set; } = string.Empty;
        [Required]
        public string PartName { get; set; } = string.Empty;
        [Range(0, double.MaxValue)]
        public decimal? UnitPrice { get; set; }
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        // PartCode uniqueness validation
        var existing = await _partService.GetPartByCodeAsync(Input.PartCode);
        if (existing != null)
        {
            ModelState.AddModelError(nameof(Input.PartCode), "Part code already exists.");
            return Page();
        }

        var part = new Part
        {
            PartCode = Input.PartCode,
            PartName = Input.PartName,
            UnitPrice = Input.UnitPrice
        };
        var createdPart = await _partService.CreatePartAsync(part);
        
        // Send notification
        var notificationPayload = new
        {
            Type = "part_created",
            Title = "New Part Created",
            Message = $"New part {part.PartName} ({part.PartCode}) has been created",
            PartId = createdPart.PartId,
            PartName = part.PartName,
            PartCode = part.PartCode
        };

        await _notificationHub.Clients.Groups("Admin", "EVM Staff", "EVM")
            .SendAsync("ReceiveNotification", notificationPayload);

        await _notificationHub.Clients.Group($"Part_{createdPart.PartId}")
            .SendAsync("ReceivePartUpdate", notificationPayload);

        TempData["Success"] = "Part created.";
        return RedirectToPage("Index");
    }
}

