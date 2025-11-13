using System;
using System.Collections.Generic;

namespace EVWarrantyManagement.BO.Models;

public partial class Vehicle
{
    public int VehicleId { get; set; }

    public string Vin { get; set; } = null!;

    public string? Model { get; set; }

    public int? CustomerId { get; set; }

    public int? Year { get; set; }

    public string? RegistrationNumber { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<WarrantyClaim> WarrantyClaims { get; set; } = new List<WarrantyClaim>();

    public virtual ICollection<ServiceBooking> ServiceBookings { get; set; } = new List<ServiceBooking>();
}
