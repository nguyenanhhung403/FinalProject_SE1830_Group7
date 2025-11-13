using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Inventory;

[Authorize(Policy = "RequireTechnician")]
public class IndexModel : PageModel
{
    private readonly IPartService _partService;

    public IndexModel(IPartService partService)
    {
        _partService = partService;
    }

    public IReadOnlyList<Part> LowStockParts { get; private set; } = Array.Empty<Part>();
    public decimal TotalInventoryValue { get; private set; }
    public IReadOnlyList<Part> AllParts { get; private set; } = Array.Empty<Part>();
    public Dictionary<int, PartInventory?> PartInventories { get; private set; } = new();

    public async Task OnGetAsync()
    {
        LowStockParts = await _partService.GetLowStockPartsAsync();
        AllParts = await _partService.GetPartsAsync();

        // Load all inventories
        PartInventories = new Dictionary<int, PartInventory?>();
        foreach (var part in AllParts)
        {
            var inventory = await _partService.GetInventoryAsync(part.PartId);
            PartInventories[part.PartId] = inventory;
        }

        // Calculate total inventory value
        TotalInventoryValue = 0;
        foreach (var part in AllParts)
        {
            var inventory = PartInventories.GetValueOrDefault(part.PartId);
            if (inventory != null && part.UnitPrice.HasValue)
            {
                TotalInventoryValue += inventory.StockQuantity * part.UnitPrice.Value;
            }
        }
    }
}

