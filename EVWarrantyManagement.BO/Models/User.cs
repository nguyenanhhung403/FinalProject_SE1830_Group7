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
}
