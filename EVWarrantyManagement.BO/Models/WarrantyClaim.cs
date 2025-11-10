using System;
using System.Collections.Generic;

namespace EVWarrantyManagement.BO.Models;

public partial class WarrantyClaim
{
    public int ClaimId { get; set; }

    public string Vin { get; set; } = null!;

    public int? VehicleId { get; set; }

    public int ServiceCenterId { get; set; }

    public int CreatedByUserId { get; set; }

    public DateOnly DateDiscovered { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public string StatusCode { get; set; } = null!;

    public decimal? Cost { get; set; }

    public string? Note { get; set; }

    public int? TechnicianId { get; set; }

    public DateOnly? CompletionDate { get; set; }

    public decimal? TotalCost { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ClaimStatusLog> ClaimStatusLogs { get; set; } = new List<ClaimStatusLog>();

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual ServiceCenter ServiceCenter { get; set; } = null!;

    public virtual ClaimStatus StatusCodeNavigation { get; set; } = null!;

    public virtual User? Technician { get; set; }

    public virtual Vehicle? Vehicle { get; set; }

    public virtual ICollection<UsedPart> UsedParts { get; set; } = new List<UsedPart>();
}
