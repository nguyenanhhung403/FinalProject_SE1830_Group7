using System;
using System.Collections.Generic;

namespace EVWarrantyManagement.BO.Models;

public partial class ClaimStatus
{
    public string StatusCode { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<WarrantyClaim> WarrantyClaims { get; set; } = new List<WarrantyClaim>();
}
