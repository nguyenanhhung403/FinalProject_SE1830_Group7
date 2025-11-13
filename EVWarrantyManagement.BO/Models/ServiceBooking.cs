using System;
using System.Collections.Generic;

namespace EVWarrantyManagement.BO.Models;

public partial class ServiceBooking
{
    public int ServiceBookingId { get; set; }

    public int CustomerId { get; set; }

    public int VehicleId { get; set; }

    public int ServiceCenterId { get; set; }

    public int? AssignedTechnicianId { get; set; }

    public int? ApprovedByUserId { get; set; }

    public int? CancelledByUserId { get; set; }

    public string ServiceType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime PreferredStart { get; set; }

    public DateTime? PreferredEnd { get; set; }

    public DateTime? ConfirmedStart { get; set; }

    public DateTime? ConfirmedEnd { get; set; }

    public string? CustomerNote { get; set; }

    public string? InternalNote { get; set; }

    public string? RejectionReason { get; set; }

    public int EstimatedDurationMinutes { get; set; } = 60;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Vehicle Vehicle { get; set; } = null!;

    public virtual ServiceCenter ServiceCenter { get; set; } = null!;

    public virtual User? AssignedTechnician { get; set; }

    public virtual User? ApprovedByUser { get; set; }

    public virtual User? CancelledByUser { get; set; }

    public virtual ICollection<ServiceBookingStatusLog> StatusLogs { get; set; } = new List<ServiceBookingStatusLog>();

    public virtual ICollection<ServiceBookingPart> ServiceBookingParts { get; set; } = new List<ServiceBookingPart>();
}
