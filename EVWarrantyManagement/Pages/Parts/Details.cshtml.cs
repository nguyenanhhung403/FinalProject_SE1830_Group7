using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace EVWarrantyManagement.Pages.Parts;

[Authorize(Policy = "RequireTechnician")]
public class DetailsModel : PageModel
{
    private readonly IPartService _partService;
    private readonly IHubContext<NotificationHub> _notificationHub;

    public DetailsModel(IPartService partService, IHubContext<NotificationHub> notificationHub)
    {
        _partService = partService;
        _notificationHub = notificationHub;
    }

    private int GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var userId) ? userId : 0;
    }

    public Part? Part { get; private set; }
    public PartInventory? Inventory { get; private set; }
    public IReadOnlyList<PartStockMovement> StockMovements { get; private set; } = Array.Empty<PartStockMovement>();

    [BindProperty]
    public int? MinStockLevel { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Part = await _partService.GetPartAsync(id);
        if (Part == null)
        {
            TempData["Error"] = "Part not found.";
            return RedirectToPage("Index");
        }

        Inventory = await _partService.GetInventoryAsync(id);
        if (Inventory != null)
        {
            MinStockLevel = Inventory.MinStockLevel;
        }

        StockMovements = await _partService.GetStockMovementsAsync(id, null, null);

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateMinStockLevelAsync(int id)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("EVM Staff") && !User.IsInRole("EVM"))
        {
            return Forbid();
        }

        var userId = GetUserId();
        if (userId == 0)
        {
            TempData["Error"] = "User not found.";
            return RedirectToPage(new { id });
        }

        try
        {
            var part = await _partService.GetPartAsync(id);
            if (part == null)
            {
                TempData["Error"] = "Part not found.";
                return RedirectToPage(new { id });
            }

            var oldInventory = await _partService.GetInventoryAsync(id);
            await _partService.UpdateMinStockLevelAsync(id, MinStockLevel, userId);
            
            // Check if stock is now below new min level
            var newInventory = await _partService.GetInventoryAsync(id);
            if (newInventory != null && MinStockLevel.HasValue && newInventory.StockQuantity < MinStockLevel.Value)
            {
                var stockAlertPayload = new
                {
                    Type = "low_stock_alert",
                    Title = "Low Stock Alert",
                    Message = $"⚠️ {part.PartName} is below minimum stock level. Current: {newInventory.StockQuantity}, Min: {MinStockLevel.Value}",
                    PartId = id,
                    PartName = part.PartName,
                    StockQuantity = newInventory.StockQuantity,
                    MinStockLevel = MinStockLevel.Value
                };

                await _notificationHub.Clients.Groups("Admin", "EVM Staff", "EVM")
                    .SendAsync("ReceiveNotification", stockAlertPayload);

                await _notificationHub.Clients.Groups("Admin", "EVM Staff", "EVM")
                    .SendAsync("ReceiveStockAlert", stockAlertPayload);

                await _notificationHub.Clients.Group($"Part_{id}")
                    .SendAsync("ReceivePartUpdate", stockAlertPayload);
            }
            
            // Send part update notification for min stock level change
            var updatePayload = new
            {
                Type = "part_updated",
                Title = "Part Updated",
                Message = $"Min stock level for {part.PartName} updated to {(MinStockLevel.HasValue ? MinStockLevel.Value.ToString() : "None")}",
                PartId = id,
                PartName = part.PartName,
                MinStockLevel = MinStockLevel
            };

            await _notificationHub.Clients.Group($"Part_{id}")
                .SendAsync("ReceivePartUpdate", updatePayload);
            
            TempData["Success"] = $"Min stock level updated to {(MinStockLevel.HasValue ? MinStockLevel.Value.ToString() : "None")}.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to update min stock level: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }
}

