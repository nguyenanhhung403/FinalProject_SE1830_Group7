using System;
using System.Collections.Generic;

namespace EVWarrantyManagement.BO.Models;

public partial class ServiceCenterTechnician
{
    public int ServiceCenterTechnicianId { get; set; }

    public int ServiceCenterId { get; set; }

    public int UserId { get; set; }

    public DateTime AssignedAt { get; set; }

    public int? AssignedByUserId { get; set; }

    public bool IsActive { get; set; }

    public virtual User? AssignedByUser { get; set; }

    public virtual ServiceCenter ServiceCenter { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}

