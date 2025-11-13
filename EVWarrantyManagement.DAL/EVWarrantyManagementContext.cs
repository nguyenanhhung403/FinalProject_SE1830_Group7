using EVWarrantyManagement.BO.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace EVWarrantyManagement.DAL;

public partial class EVWarrantyManagementContext : DbContext
{
    public EVWarrantyManagementContext()
    {
    }

    public EVWarrantyManagementContext(DbContextOptions<EVWarrantyManagementContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ClaimStatus> ClaimStatuses { get; set; }

    public virtual DbSet<ClaimStatusLog> ClaimStatusLogs { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Part> Parts { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<ServiceCenter> ServiceCenters { get; set; }

    public virtual DbSet<ServiceCenterTechnician> ServiceCenterTechnicians { get; set; }

    public virtual DbSet<PartInventory> PartInventories { get; set; }

    public virtual DbSet<PartStockMovement> PartStockMovements { get; set; }

    public virtual DbSet<UsedPart> UsedParts { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    public virtual DbSet<WarrantyClaim> WarrantyClaims { get; set; }

    public virtual DbSet<WarrantyHistory> WarrantyHistories { get; set; }

    public virtual DbSet<ServiceBooking> ServiceBookings { get; set; }

    public virtual DbSet<ServiceBookingStatusLog> ServiceBookingStatusLogs { get; set; }

    public virtual DbSet<ServiceBookingPart> ServiceBookingParts { get; set; }

    private string GetConnectionString()
    {
        IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true).Build();
        return configuration["ConnectionStrings:DefaultConnectionString"];
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer(GetConnectionString());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClaimStatus>(entity =>
        {
            entity.HasKey(e => e.StatusCode).HasName("PK__ClaimSta__6A7B44FD017F2BA3");

            entity.ToTable("ClaimStatuses", "ev");

            entity.Property(e => e.StatusCode).HasMaxLength(30);
            entity.Property(e => e.Description).HasMaxLength(200);
        });

        modelBuilder.Entity<ClaimStatusLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__ClaimSta__5E548648585C4C71");

            entity.ToTable("ClaimStatusLog", "ev");

            entity.Property(e => e.ChangedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Comment).HasMaxLength(1000);
            entity.Property(e => e.NewStatus).HasMaxLength(30);
            entity.Property(e => e.OldStatus).HasMaxLength(30);

            entity.HasOne(d => d.ChangedByUser).WithMany(p => p.ClaimStatusLogs)
                .HasForeignKey(d => d.ChangedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClaimStatusLog_User");

            entity.HasOne(d => d.Claim).WithMany(p => p.ClaimStatusLogs)
                .HasForeignKey(d => d.ClaimId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClaimStatusLog_Claim");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__A4AE64D8887AF509");

            entity.ToTable("Customers", "ev");

            entity.Property(e => e.Address).HasMaxLength(400);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
        });

        modelBuilder.Entity<Part>(entity =>
        {
            entity.HasKey(e => e.PartId).HasName("PK__Parts__7C3F0D500FE65EC3");

            entity.ToTable("Parts", "ev");

            entity.HasIndex(e => e.PartCode, "UQ__Parts__6525D39D12465997").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.PartCode).HasMaxLength(100);
            entity.Property(e => e.PartName).HasMaxLength(300);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1A92979D87");

            entity.ToTable("Roles", "ev");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B61603589A661").IsUnique();

            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<ServiceCenter>(entity =>
        {
            entity.HasKey(e => e.ServiceCenterId).HasName("PK__ServiceC__71B62BE3A78826A1");

            entity.ToTable("ServiceCenters", "ev");

            entity.Property(e => e.Address).HasMaxLength(400);
            entity.Property(e => e.ContactName).HasMaxLength(200);
            entity.Property(e => e.ContactPhone).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<ServiceCenterTechnician>(entity =>
        {
            entity.HasKey(e => e.ServiceCenterTechnicianId).HasName("PK_ServiceCenterTechnicians");

            entity.ToTable("ServiceCenterTechnicians", "ev");

            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.ServiceCenter).WithMany(p => p.ServiceCenterTechnicians)
                .HasForeignKey(d => d.ServiceCenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceCenterTechnicians_ServiceCenter");

            entity.HasOne(d => d.User).WithMany(p => p.ServiceCenterTechnicians)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceCenterTechnicians_User");

            entity.HasOne(d => d.AssignedByUser).WithMany(p => p.ServiceCenterTechnicianAssignments)
                .HasForeignKey(d => d.AssignedByUserId)
                .HasConstraintName("FK_ServiceCenterTechnicians_AssignedBy");
        });

        modelBuilder.Entity<PartInventory>(entity =>
        {
            entity.HasKey(e => e.InventoryId).HasName("PK_PartInventory");

            entity.ToTable("PartInventory", "ev");

            entity.HasIndex(e => e.PartId, "IX_PartInventory_PartId").IsUnique();

            entity.Property(e => e.LastUpdated).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.StockQuantity).HasDefaultValue(0);

            entity.HasOne(d => d.Part).WithOne(p => p.PartInventory)
                .HasForeignKey<PartInventory>(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PartInventory_Part");

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.PartInventoryUpdates)
                .HasForeignKey(d => d.UpdatedByUserId)
                .HasConstraintName("FK_PartInventory_UpdatedBy");
        });

        modelBuilder.Entity<PartStockMovement>(entity =>
        {
            entity.HasKey(e => e.MovementId).HasName("PK_PartStockMovements");

            entity.ToTable("PartStockMovements", "ev");

            entity.HasIndex(e => e.PartId, "IX_PartStockMovements_PartId");
            entity.HasIndex(e => e.CreatedAt, "IX_PartStockMovements_CreatedAt");
            entity.HasIndex(e => new { e.ReferenceType, e.ReferenceId }, "IX_PartStockMovements_Reference");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.MovementType).HasMaxLength(20);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.ReferenceType).HasMaxLength(50);

            entity.HasOne(d => d.Part).WithMany(p => p.PartStockMovements)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PartStockMovements_Part");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.PartStockMovements)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PartStockMovements_CreatedBy");
        });

        modelBuilder.Entity<UsedPart>(entity =>
        {
            entity.HasKey(e => e.UsedPartId).HasName("PK__UsedPart__4F7AA36385A67BD9");

            entity.ToTable("UsedParts", "ev");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.PartCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Quantity).HasDefaultValue(1);

            entity.HasOne(d => d.Claim).WithMany(p => p.UsedParts)
                .HasForeignKey(d => d.ClaimId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsedParts_Claim");

            entity.HasOne(d => d.Part).WithMany(p => p.UsedParts)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsedParts_Part");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CC2B074E0");

            entity.ToTable("Users", "ev");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E438FDA73A").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(64);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Username).HasMaxLength(100);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.VehicleId).HasName("PK__Vehicles__476B5492BC033727");

            entity.ToTable("Vehicles", "ev");

            entity.HasIndex(e => e.Vin, "IX_Vehicles_VIN");

            entity.HasIndex(e => e.Vin, "UQ__Vehicles__C5DF234CB553C1ED").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Model).HasMaxLength(200);
            entity.Property(e => e.RegistrationNumber).HasMaxLength(50);
            entity.Property(e => e.Vin)
                .HasMaxLength(50)
                .HasColumnName("VIN");

            entity.HasOne(d => d.Customer).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Vehicles_Customers");
        });

        modelBuilder.Entity<WarrantyClaim>(entity =>
        {
            entity.HasKey(e => e.ClaimId).HasName("PK__Warranty__EF2E139B3C735D39");

            entity.ToTable("WarrantyClaim", "ev");

            entity.HasIndex(e => e.StatusCode, "IX_WarrantyClaim_Status");

            entity.HasIndex(e => e.Vin, "IX_WarrantyClaim_VIN");

            entity.Property(e => e.Cost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.ImageUrl).HasMaxLength(1000);
            entity.Property(e => e.Note).HasMaxLength(2000);
            entity.Property(e => e.StatusCode)
                .HasMaxLength(30)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Vin)
                .HasMaxLength(50)
                .HasColumnName("VIN");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.WarrantyClaimCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Claim_CreatedBy");

            entity.HasOne(d => d.ServiceCenter).WithMany(p => p.WarrantyClaims)
                .HasForeignKey(d => d.ServiceCenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Claim_ServiceCenter");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.WarrantyClaims)
                .HasForeignKey(d => d.VehicleId)
                .HasConstraintName("FK_Claim_Vehicle");

            entity.HasOne(d => d.StatusCodeNavigation).WithMany(p => p.WarrantyClaims)
                .HasForeignKey(d => d.StatusCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Claim_Status");

            entity.HasOne(d => d.Technician).WithMany(p => p.WarrantyClaimTechnicians)
                .HasForeignKey(d => d.TechnicianId)
                .HasConstraintName("FK_Claim_Technician");
        });

        modelBuilder.Entity<WarrantyHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__Warranty__4D7B4ABDAAA4CBCD");

            entity.ToTable("WarrantyHistory", "ev");

            entity.Property(e => e.ArchivedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Note).HasMaxLength(2000);
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Vin)
                .HasMaxLength(50)
                .HasColumnName("VIN");

            entity.HasOne(d => d.CompletedByUser).WithMany(p => p.WarrantyHistoryCompletedByUsers)
                .HasForeignKey(d => d.CompletedByUserId)
                .HasConstraintName("FK_WarrantyHistory_CompletedBy");

            entity.HasOne(d => d.ServiceCenter).WithMany(p => p.WarrantyHistories)
                .HasForeignKey(d => d.ServiceCenterId)
                .HasConstraintName("FK_WarrantyHistory_ServiceCenter");

            entity.HasOne(d => d.Vehicle).WithMany()
                .HasForeignKey(d => d.VehicleId)
                .HasConstraintName("FK_WarrantyHistory_Vehicle");
        });

        modelBuilder.Entity<ServiceBooking>(entity =>
        {
            entity.HasKey(e => e.ServiceBookingId).HasName("PK_ServiceBookings");

            entity.ToTable("ServiceBookings", "ev");

            entity.HasIndex(e => e.Status, "IX_ServiceBookings_Status");
            entity.HasIndex(e => new { e.ServiceCenterId, e.PreferredStart }, "IX_ServiceBookings_ServiceCenter_Date");
            entity.HasIndex(e => new { e.AssignedTechnicianId, e.PreferredStart }, "IX_ServiceBookings_AssignedTechnician")
                .HasFilter("([AssignedTechnicianId] IS NOT NULL)");

            entity.Property(e => e.ServiceType).HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Pending");
            entity.Property(e => e.CustomerNote).HasMaxLength(1000);
            entity.Property(e => e.InternalNote).HasMaxLength(1000);
            entity.Property(e => e.RejectionReason).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EstimatedDurationMinutes).HasDefaultValue(60);
            entity.Property(e => e.PreferredStart).HasColumnType("datetime2(0)");
            entity.Property(e => e.PreferredEnd).HasColumnType("datetime2(0)");
            entity.Property(e => e.ConfirmedStart).HasColumnType("datetime2(0)");
            entity.Property(e => e.ConfirmedEnd).HasColumnType("datetime2(0)");
            entity.Property(e => e.ApprovedAt).HasColumnType("datetime2(0)");
            entity.Property(e => e.CompletedAt).HasColumnType("datetime2(0)");
            entity.Property(e => e.CancelledAt).HasColumnType("datetime2(0)");

            entity.HasOne(d => d.Customer).WithMany(p => p.ServiceBookings)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceBookings_Customers");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.ServiceBookings)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceBookings_Vehicles");

            entity.HasOne(d => d.ServiceCenter).WithMany(p => p.ServiceBookings)
                .HasForeignKey(d => d.ServiceCenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceBookings_ServiceCenters");

            entity.HasOne(d => d.AssignedTechnician).WithMany(p => p.ServiceBookingsAssigned)
                .HasForeignKey(d => d.AssignedTechnicianId)
                .HasConstraintName("FK_ServiceBookings_AssignedTechnician");

            entity.HasOne(d => d.ApprovedByUser).WithMany(p => p.ServiceBookingsApproved)
                .HasForeignKey(d => d.ApprovedByUserId)
                .HasConstraintName("FK_ServiceBookings_ApprovedBy");

            entity.HasOne(d => d.CancelledByUser).WithMany(p => p.ServiceBookingsCancelled)
                .HasForeignKey(d => d.CancelledByUserId)
                .HasConstraintName("FK_ServiceBookings_CancelledBy");
        });

        modelBuilder.Entity<ServiceBookingStatusLog>(entity =>
        {
            entity.HasKey(e => e.ServiceBookingStatusLogId).HasName("PK_ServiceBookingStatusLogs");

            entity.ToTable("ServiceBookingStatusLogs", "ev");

            entity.Property(e => e.OldStatus).HasMaxLength(30);
            entity.Property(e => e.NewStatus).HasMaxLength(30);
            entity.Property(e => e.Note).HasMaxLength(1000);
            entity.Property(e => e.ChangedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.ServiceBooking).WithMany(p => p.StatusLogs)
                .HasForeignKey(d => d.ServiceBookingId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ServiceBookingStatusLogs_ServiceBookings");

            entity.HasOne(d => d.ChangedByUser).WithMany(p => p.ServiceBookingStatusLogs)
                .HasForeignKey(d => d.ChangedByUserId)
                .HasConstraintName("FK_ServiceBookingStatusLogs_Users");
        });

        modelBuilder.Entity<ServiceBookingPart>(entity =>
        {
            entity.HasKey(e => e.ServiceBookingPartId).HasName("PK_ServiceBookingParts");

            entity.ToTable("ServiceBookingParts", "ev");

            entity.Property(e => e.PartCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.ServiceBooking).WithMany(p => p.ServiceBookingParts)
                .HasForeignKey(d => d.ServiceBookingId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ServiceBookingParts_ServiceBookings");

            entity.HasOne(d => d.Part).WithMany(p => p.ServiceBookingParts)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceBookingParts_Parts");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.ServiceBookingParts)
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK_ServiceBookingParts_CreatedBy");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
