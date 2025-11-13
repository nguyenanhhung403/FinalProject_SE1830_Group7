using System;
using System.Collections.Generic;

namespace EVWarrantyManagement.BO.Models;

public partial class Part
{
    public int PartId { get; set; }

    public string PartCode { get; set; } = null!;

    public string PartName { get; set; } = null!;

    public decimal? UnitPrice { get; set; }

    public int? WarrantyPeriodMonths { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<UsedPart> UsedParts { get; set; } = new List<UsedPart>();

    public virtual PartInventory? PartInventory { get; set; }

    public virtual ICollection<PartStockMovement> PartStockMovements { get; set; } = new List<PartStockMovement>();

    public virtual ICollection<ServiceBookingPart> ServiceBookingParts { get; set; } = new List<ServiceBookingPart>();
}
