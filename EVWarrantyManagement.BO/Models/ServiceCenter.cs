using System;
using System.Collections.Generic;

namespace EVWarrantyManagement.BO.Models;

public partial class ServiceCenter
{
    public int ServiceCenterId { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string? ContactName { get; set; }

    public string? ContactPhone { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<WarrantyClaim> WarrantyClaims { get; set; } = new List<WarrantyClaim>();

    public virtual ICollection<WarrantyHistory> WarrantyHistories { get; set; } = new List<WarrantyHistory>();

    public virtual ICollection<ServiceCenterTechnician> ServiceCenterTechnicians { get; set; } = new List<ServiceCenterTechnician>();

    public virtual ICollection<ServiceBooking> ServiceBookings { get; set; } = new List<ServiceBooking>();
}
