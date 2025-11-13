using System.Security.Claims;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;

namespace EVWarrantyManagement.Pages.Inventory;

[Authorize(Policy = "RequireEVM")]
public class AdjustStockModel : PageModel
{
    private readonly IPartService _partService;
    private readonly IHubContext<NotificationHub> _notificationHub;

    public AdjustStockModel(IPartService partService, IHubContext<NotificationHub> notificationHub)
    {
        _partService = partService;
        _notificationHub = notificationHub;
    }

    public IReadOnlyList<Part> Parts { get; private set; } = Array.Empty<Part>();
    public SelectList PartsSelectList { get; private set; } = null!;

    [BindProperty]
    public int PartId { get; set; }

    [BindProperty]
    public string MovementType { get; set; } = "IN";

    [BindProperty]
    public int Quantity { get; set; } = 1;

    [BindProperty]
    public string? Reason { get; set; }

    public async Task OnGetAsync()
    {
        Parts = await _partService.GetPartsAsync();
        PartsSelectList = new SelectList(Parts, "PartId", "PartName");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Parts = await _partService.GetPartsAsync();
            PartsSelectList = new SelectList(Parts, "PartId", "PartName");
            return Page();
        }

        try
        {
            var userId = GetUserId();
            var part = await _partService.GetPartAsync(PartId);
            var oldInventory = await _partService.GetInventoryAsync(PartId);
            await _partService.AdjustStockAsync(PartId, Quantity, MovementType, Reason, userId);
            
            // Get updated inventory
            var inventory = await _partService.GetInventoryAsync(PartId);
            
            // Send inventory update notification
            var inventoryUpdatePayload = new
            {
                Type = "stock_adjusted",
                Title = "Stock Adjusted",
                Message = $"Stock adjusted for {part?.PartName ?? "Part"}: {MovementType} {Quantity} units. New stock: {inventory?.StockQuantity ?? 0}",
                PartId = PartId,
                PartName = part?.PartName,
                StockQuantity = inventory?.StockQuantity ?? 0,
                Quantity = Quantity,
                MovementType = MovementType,
                Reason = Reason
            };

            await _notificationHub.Clients.Groups("Admin", "EVM Staff", "EVM")
                .SendAsync("ReceiveNotification", inventoryUpdatePayload);

            await _notificationHub.Clients.Group($"Inventory_{PartId}")
                .SendAsync("ReceiveInventoryUpdate", inventoryUpdatePayload);

            await _notificationHub.Clients.Group($"Part_{PartId}")
                .SendAsync("ReceivePartUpdate", inventoryUpdatePayload);
            
            // Check for low stock after adjustment
            if (inventory != null && inventory.MinStockLevel.HasValue && inventory.StockQuantity < inventory.MinStockLevel.Value)
            {
                var stockAlertPayload = new
                {
                    Type = "low_stock_alert",
                    Title = "Low Stock Alert",
                    Message = $"⚠️ {part?.PartName ?? "Part"} is low on stock. Only {inventory.StockQuantity} units remaining (Min: {inventory.MinStockLevel})",
                    PartId = PartId,
                    PartName = part?.PartName,
                    StockQuantity = inventory.StockQuantity,
                    MinStockLevel = inventory.MinStockLevel.Value
                };

                await _notificationHub.Clients.Groups("Admin", "EVM Staff", "EVM")
                    .SendAsync("ReceiveNotification", stockAlertPayload);

                await _notificationHub.Clients.Groups("Admin", "EVM Staff", "EVM")
                    .SendAsync("ReceiveStockAlert", stockAlertPayload);
            }
            
            TempData["Success"] = $"Stock adjusted successfully. Movement type: {MovementType}, Quantity: {Quantity}";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error adjusting stock: {ex.Message}";
            Parts = await _partService.GetPartsAsync();
            PartsSelectList = new SelectList(Parts, "PartId", "PartName");
            return Page();
        }
    }

    private int GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var userId) ? userId : 0;
    }
}

