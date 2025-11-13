using System;
using System.Collections.Generic;

namespace EVWarrantyManagement.BO.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public byte[] PasswordHash { get; set; } = null!;

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public int RoleId { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<ClaimStatusLog> ClaimStatusLogs { get; set; } = new List<ClaimStatusLog>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<WarrantyClaim> WarrantyClaimCreatedByUsers { get; set; } = new List<WarrantyClaim>();

    public virtual ICollection<WarrantyClaim> WarrantyClaimTechnicians { get; set; } = new List<WarrantyClaim>();

    public virtual ICollection<WarrantyHistory> WarrantyHistoryCompletedByUsers { get; set; } = new List<WarrantyHistory>();

    public virtual ICollection<ServiceCenterTechnician> ServiceCenterTechnicians { get; set; } = new List<ServiceCenterTechnician>();

    public virtual ICollection<ServiceCenterTechnician> ServiceCenterTechnicianAssignments { get; set; } = new List<ServiceCenterTechnician>();

    public virtual ICollection<PartInventory> PartInventoryUpdates { get; set; } = new List<PartInventory>();

    public virtual ICollection<PartStockMovement> PartStockMovements { get; set; } = new List<PartStockMovement>();

    public virtual ICollection<ServiceBooking> ServiceBookingsAssigned { get; set; } = new List<ServiceBooking>();

    public virtual ICollection<ServiceBooking> ServiceBookingsApproved { get; set; } = new List<ServiceBooking>();

    public virtual ICollection<ServiceBooking> ServiceBookingsCancelled { get; set; } = new List<ServiceBooking>();

    public virtual ICollection<ServiceBookingStatusLog> ServiceBookingStatusLogs { get; set; } = new List<ServiceBookingStatusLog>();

    public virtual ICollection<ServiceBookingPart> ServiceBookingParts { get; set; } = new List<ServiceBookingPart>();
}
