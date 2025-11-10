using System;
using System.Collections.Generic;

namespace EVWarrantyManagement.BO.Models;

public partial class WarrantyHistory
{
    public int HistoryId { get; set; }

    public int ClaimId { get; set; }

    public string Vin { get; set; } = null!;

    public int? VehicleId { get; set; }

    public int? ServiceCenterId { get; set; }

    public int? CompletedByUserId { get; set; }

    public DateOnly? CompletionDate { get; set; }

    public decimal? TotalCost { get; set; }

    public string? Note { get; set; }

    public DateTime ArchivedAt { get; set; }

    public virtual User? CompletedByUser { get; set; }

    public virtual ServiceCenter? ServiceCenter { get; set; }

    public virtual Vehicle? Vehicle { get; set; }
}
