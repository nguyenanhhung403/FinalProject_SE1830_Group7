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
public class EditModel : PageModel
{
    private readonly IPartService _partService;
    private readonly IHubContext<NotificationHub> _notificationHub;

    public EditModel(IPartService partService, IHubContext<NotificationHub> notificationHub)
    {
        _partService = partService;
        _notificationHub = notificationHub;
    }

    [BindProperty]
    public int Id { get; set; }

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

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var part = await _partService.GetPartAsync(id);
        if (part == null) return RedirectToPage("Index");

        Id = id;
        Input = new PartInputModel
        {
            PartCode = part.PartCode,
            PartName = part.PartName,
            UnitPrice = part.UnitPrice
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var part = await _partService.GetPartAsync(Id);
        if (part == null) return RedirectToPage("Index");

        // Uniqueness check if code changed
        if (!string.Equals(part.PartCode, Input.PartCode, StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _partService.GetPartByCodeAsync(Input.PartCode);
            if (exists != null)
            {
                ModelState.AddModelError(nameof(Input.PartCode), "Part code already exists.");
                return Page();
            }
        }

        part.PartCode = Input.PartCode;
        part.PartName = Input.PartName;
        part.UnitPrice = Input.UnitPrice;

        await _partService.UpdatePartAsync(part);
        
        // Send notification
        var notificationPayload = new
        {
            Type = "part_updated",
            Title = "Part Updated",
            Message = $"Part {part.PartName} ({part.PartCode}) has been updated",
            PartId = part.PartId,
            PartName = part.PartName,
            PartCode = part.PartCode
        };

        await _notificationHub.Clients.Groups("Admin", "EVM Staff", "EVM")
            .SendAsync("ReceiveNotification", notificationPayload);

        await _notificationHub.Clients.Group($"Part_{part.PartId}")
            .SendAsync("ReceivePartUpdate", notificationPayload);

        TempData["Success"] = "Part updated.";
        return RedirectToPage("Index");
    }
}

