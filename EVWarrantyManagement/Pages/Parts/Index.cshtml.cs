using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Parts;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IPartService _partService;

    public IndexModel(IPartService partService)
    {
        _partService = partService;
    }

    public IReadOnlyList<Part> Parts { get; private set; } = Array.Empty<Part>();
    public Dictionary<int, PartInventory?> PartInventories { get; private set; } = new();
    public IReadOnlyList<PartStockMovement> RecentStockMovements { get; private set; } = Array.Empty<PartStockMovement>();

    public async Task OnGetAsync()
    {
        Parts = await _partService.GetPartsAsync();
        
        // Load inventories for all parts
        PartInventories = new Dictionary<int, PartInventory?>();
        foreach (var part in Parts)
        {
            var inventory = await _partService.GetInventoryAsync(part.PartId);
            PartInventories[part.PartId] = inventory;
        }

        // Load recent stock movements (last 50)
        RecentStockMovements = await _partService.GetRecentStockMovementsAsync(50);
    }
}

