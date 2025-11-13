using System;

namespace EVWarrantyManagement.BO.Models;

public partial class PartStockMovement
{
    public int MovementId { get; set; }

    public int PartId { get; set; }

    public string MovementType { get; set; } = null!;

    public int Quantity { get; set; }

    public string? ReferenceType { get; set; }

    public int? ReferenceId { get; set; }

    public string? Note { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual Part Part { get; set; } = null!;
}

