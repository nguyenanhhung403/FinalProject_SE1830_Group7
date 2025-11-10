using System;
using System.Collections.Generic;

namespace EVWarrantyManagement.BO.Models;

public partial class UsedPart
{
    public int UsedPartId { get; set; }

    public int ClaimId { get; set; }

    public int PartId { get; set; }

    public int Quantity { get; set; }

    public decimal? PartCost { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual WarrantyClaim Claim { get; set; } = null!;

    public virtual Part Part { get; set; } = null!;
}
