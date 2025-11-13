using System;

namespace EVWarrantyManagement.BO.Models;

public partial class PartInventory
{
    public int InventoryId { get; set; }

    public int PartId { get; set; }

    public int StockQuantity { get; set; }

    public int? MinStockLevel { get; set; }

    public DateTime LastUpdated { get; set; }

    public int? UpdatedByUserId { get; set; }

    public virtual Part Part { get; set; } = null!;

    public virtual User? UpdatedByUser { get; set; }
}

