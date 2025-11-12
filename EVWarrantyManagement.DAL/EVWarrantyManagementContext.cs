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

    public virtual DbSet<UsedPart> UsedParts { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    public virtual DbSet<WarrantyClaim> WarrantyClaims { get; set; }

    public virtual DbSet<WarrantyHistory> WarrantyHistories { get; set; }

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
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
