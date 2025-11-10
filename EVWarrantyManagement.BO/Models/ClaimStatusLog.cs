using System;
using System.Collections.Generic;

namespace EVWarrantyManagement.BO.Models;

public partial class ClaimStatusLog
{
    public int LogId { get; set; }

    public int ClaimId { get; set; }

    public string? OldStatus { get; set; }

    public string NewStatus { get; set; } = null!;

    public int ChangedByUserId { get; set; }

    public DateTime ChangedAt { get; set; }

    public string? Comment { get; set; }

    public virtual User ChangedByUser { get; set; } = null!;

    public virtual WarrantyClaim Claim { get; set; } = null!;
}
