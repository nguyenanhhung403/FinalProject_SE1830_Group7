using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVWarrantyManagement.DAL.Repositories;

public class ServiceCenterRepository : IServiceCenterRepository
{
    private readonly EVWarrantyManagementContext _context;

    public ServiceCenterRepository(EVWarrantyManagementContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ServiceCenter>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ServiceCenters
            .AsNoTracking()
            .OrderBy(sc => sc.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceCenter?> GetByIdAsync(int serviceCenterId, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceCenters
            .AsNoTracking()
            .Include(sc => sc.ServiceCenterTechnicians.Where(sct => sct.IsActive))
                .ThenInclude(sct => sct.User)
            .Include(sc => sc.WarrantyClaims)
            .FirstOrDefaultAsync(sc => sc.ServiceCenterId == serviceCenterId, cancellationToken);
    }

    public async Task<ServiceCenter> CreateAsync(ServiceCenter serviceCenter, CancellationToken cancellationToken = default)
    {
        serviceCenter.CreatedAt = DateTime.UtcNow;
        _context.ServiceCenters.Add(serviceCenter);
        await _context.SaveChangesAsync(cancellationToken);
        return serviceCenter;
    }

    public async Task UpdateAsync(ServiceCenter serviceCenter, CancellationToken cancellationToken = default)
    {
        _context.ServiceCenters.Update(serviceCenter);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int serviceCenterId, CancellationToken cancellationToken = default)
    {
        var serviceCenter = await _context.ServiceCenters.FindAsync(new object[] { serviceCenterId }, cancellationToken);
        if (serviceCenter != null)
        {
            _context.ServiceCenters.Remove(serviceCenter);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<ServiceCenterTechnician>> GetTechniciansAsync(int serviceCenterId, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceCenterTechnicians
            .AsNoTracking()
            .Include(sct => sct.User)
                .ThenInclude(u => u.Role)
            .Include(sct => sct.AssignedByUser)
            .Where(sct => sct.ServiceCenterId == serviceCenterId && sct.IsActive)
            .OrderBy(sct => sct.User.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task AssignTechnicianAsync(int serviceCenterId, int userId, int assignedByUserId, CancellationToken cancellationToken = default)
    {
        // Check if already assigned (active)
        var existing = await _context.ServiceCenterTechnicians
            .FirstOrDefaultAsync(sct => sct.ServiceCenterId == serviceCenterId 
                && sct.UserId == userId 
                && sct.IsActive, cancellationToken);

        if (existing != null)
        {
            return; // Already assigned
        }

        // Deactivate any other assignments for this user
        var otherAssignments = await _context.ServiceCenterTechnicians
            .Where(sct => sct.UserId == userId && sct.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var assignment in otherAssignments)
        {
            assignment.IsActive = false;
        }

        // Create new assignment
        var newAssignment = new ServiceCenterTechnician
        {
            ServiceCenterId = serviceCenterId,
            UserId = userId,
            AssignedByUserId = assignedByUserId,
            AssignedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.ServiceCenterTechnicians.Add(newAssignment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UnassignTechnicianAsync(int userId, CancellationToken cancellationToken = default)
    {
        var assignments = await _context.ServiceCenterTechnicians
            .Where(sct => sct.UserId == userId && sct.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var assignment in assignments)
        {
            assignment.IsActive = false;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetAvailableTechniciansAsync(CancellationToken cancellationToken = default)
    {
        // Get users with SC Technician or SC role who are not currently assigned to any service center
        var assignedUserIds = await _context.ServiceCenterTechnicians
            .Where(sct => sct.IsActive)
            .Select(sct => sct.UserId)
            .ToListAsync(cancellationToken);

        var technicianRoleIds = await _context.Roles
            .Where(r => r.RoleName == "SC Technician" || r.RoleName == "SC")
            .Select(r => r.RoleId)
            .ToListAsync(cancellationToken);

        return await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => technicianRoleIds.Contains(u.RoleId) 
                && u.IsActive 
                && !assignedUserIds.Contains(u.UserId))
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceCenterStats> GetServiceCenterStatsAsync(int serviceCenterId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = DateOnly.FromDateTime(new DateTime(now.Year, now.Month, 1));

        var totalClaims = await _context.WarrantyClaims
            .CountAsync(c => c.ServiceCenterId == serviceCenterId, cancellationToken);

        var activeClaims = await _context.WarrantyClaims
            .CountAsync(c => c.ServiceCenterId == serviceCenterId 
                && (c.StatusCode == "Pending" || c.StatusCode == "Approved" || c.StatusCode == "InProgress"), cancellationToken);

        var completedThisMonth = await _context.WarrantyClaims
            .CountAsync(c => c.ServiceCenterId == serviceCenterId 
                && c.StatusCode == "Completed" 
                && c.CompletionDate.HasValue 
                && c.CompletionDate.Value >= startOfMonth, cancellationToken);

        var assignedTechnicians = await _context.ServiceCenterTechnicians
            .CountAsync(sct => sct.ServiceCenterId == serviceCenterId && sct.IsActive, cancellationToken);

        return new ServiceCenterStats(
            TotalClaims: totalClaims,
            ActiveClaims: activeClaims,
            CompletedClaimsThisMonth: completedThisMonth,
            AssignedTechniciansCount: assignedTechnicians
        );
    }
}

