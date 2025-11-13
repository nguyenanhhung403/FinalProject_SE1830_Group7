using System;

namespace EVWarrantyManagement.BO.Models;

public partial class ServiceBookingPart
{
    public int ServiceBookingPartId { get; set; }

    public int ServiceBookingId { get; set; }

    public int PartId { get; set; }

    public int Quantity { get; set; }

    public decimal? PartCost { get; set; }

    public string? Note { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Part Part { get; set; } = null!;

    public virtual ServiceBooking ServiceBooking { get; set; } = null!;

    public virtual User? CreatedByUser { get; set; }
}

