using System;

namespace EVWarrantyManagement.BO.Models;

public partial class ServiceBookingStatusLog
{
    public int ServiceBookingStatusLogId { get; set; }

    public int ServiceBookingId { get; set; }

    public string? OldStatus { get; set; }

    public string NewStatus { get; set; } = null!;

    public string? Note { get; set; }

    public int? ChangedByUserId { get; set; }

    public DateTime ChangedAt { get; set; }

    public virtual ServiceBooking ServiceBooking { get; set; } = null!;

    public virtual User? ChangedByUser { get; set; }
}
