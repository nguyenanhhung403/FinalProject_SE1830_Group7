using System.Threading;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVWarrantyManagement.DAL.Repositories;

public class WarrantyClaimRepository : IWarrantyClaimRepository
{
    private readonly EVWarrantyManagementContext _context;

    public WarrantyClaimRepository(EVWarrantyManagementContext context)
    {
        _context = context;
    }

    public async Task<WarrantyClaim?> GetByIdAsync(int claimId, CancellationToken cancellationToken = default)
    {
        return await _context.WarrantyClaims
            .AsNoTracking()
            .Include(c => c.ServiceCenter)
            .Include(c => c.Vehicle)
                .ThenInclude(v => v.Customer)
            .Include(c => c.CreatedByUser)
            .Include(c => c.Technician)
            .Include(c => c.UsedParts)
                .ThenInclude(up => up.Part)
            .Include(c => c.ClaimStatusLogs)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId, cancellationToken);
    }

    public async Task<IReadOnlyList<WarrantyClaim>> GetPendingClaimsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.WarrantyClaims
            .AsNoTracking()
            .Include(c => c.ServiceCenter)
            .Include(c => c.Technician)
            .Include(c => c.Vehicle)
            .Include(c => c.CreatedByUser)
            .Where(c => c.StatusCode == "Pending")
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WarrantyClaim>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.WarrantyClaims
            .AsNoTracking()
            .Include(c => c.ServiceCenter)
            .Include(c => c.Technician)
            .Include(c => c.Vehicle)
            .Include(c => c.CreatedByUser)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WarrantyClaim>> GetClaimsByServiceCenterAsync(int serviceCenterId, CancellationToken cancellationToken = default)
    {
        return await _context.WarrantyClaims
            .AsNoTracking()
            .Include(c => c.ServiceCenter)
            .Include(c => c.Technician)
            .Include(c => c.Vehicle)
            .Include(c => c.CreatedByUser)
            .Where(c => c.ServiceCenterId == serviceCenterId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<WarrantyClaim> CreateAsync(WarrantyClaim claim, int createdByUserId, string? comment, CancellationToken cancellationToken = default)
    {
        claim.StatusCode = string.IsNullOrWhiteSpace(claim.StatusCode) ? "Pending" : claim.StatusCode;
        claim.CreatedAt = DateTime.UtcNow;
        claim.CreatedByUserId = createdByUserId;

        if (claim.DateDiscovered == default)
        {
            claim.DateDiscovered = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        _context.WarrantyClaims.Add(claim);
        await _context.SaveChangesAsync(cancellationToken);

        var statusLog = new ClaimStatusLog
        {
            ClaimId = claim.ClaimId,
            OldStatus = null,
            NewStatus = claim.StatusCode,
            ChangedByUserId = createdByUserId,
            ChangedAt = DateTime.UtcNow,
            Comment = comment ?? "Claim created"
        };

        _context.ClaimStatusLogs.Add(statusLog);
        await _context.SaveChangesAsync(cancellationToken);

        return claim;
    }

    public async Task UpdateStatusAsync(int claimId, string newStatus, int changedByUserId, string? note, decimal? cost, CancellationToken cancellationToken = default)
    {
        var claim = await _context.WarrantyClaims.FirstOrDefaultAsync(c => c.ClaimId == claimId, cancellationToken);
        if (claim is null)
        {
            throw new InvalidOperationException($"Claim {claimId} not found.");
        }

        var oldStatus = claim.StatusCode;
        claim.StatusCode = newStatus;

        if (!string.IsNullOrWhiteSpace(note))
        {
            claim.Note = note;
        }

        if (cost.HasValue)
        {
            claim.Cost = cost;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _context.ClaimStatusLogs.Add(new ClaimStatusLog
        {
            ClaimId = claim.ClaimId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow,
            Comment = note
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddUsedPartAsync(UsedPart usedPart, int addedByUserId, CancellationToken cancellationToken = default)
    {
        var claim = await _context.WarrantyClaims.FirstOrDefaultAsync(c => c.ClaimId == usedPart.ClaimId, cancellationToken);
        if (claim is null)
        {
            throw new InvalidOperationException($"Claim {usedPart.ClaimId} not found.");
        }

        usedPart.CreatedAt = DateTime.UtcNow;

        _context.UsedParts.Add(usedPart);

        if (usedPart.PartCost.HasValue)
        {
            var partTotal = usedPart.PartCost.Value * usedPart.Quantity;
            claim.TotalCost = (claim.TotalCost ?? 0) + partTotal;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _context.ClaimStatusLogs.Add(new ClaimStatusLog
        {
            ClaimId = usedPart.ClaimId,
            OldStatus = claim.StatusCode,
            NewStatus = claim.StatusCode,
            ChangedByUserId = addedByUserId,
            ChangedAt = DateTime.UtcNow,
            Comment = $"Added part {usedPart.PartId} x{usedPart.Quantity}"
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveUsedPartAsync(int usedPartId, int removedByUserId, CancellationToken cancellationToken = default)
    {
        var usedPart = await _context.UsedParts
            .Include(up => up.Claim)
            .FirstOrDefaultAsync(up => up.UsedPartId == usedPartId, cancellationToken);

        if (usedPart is null)
        {
            throw new InvalidOperationException($"Used part {usedPartId} not found.");
        }

        var claim = usedPart.Claim;
        if (claim is null)
        {
            claim = await _context.WarrantyClaims.FirstOrDefaultAsync(c => c.ClaimId == usedPart.ClaimId, cancellationToken)
                ?? throw new InvalidOperationException($"Claim {usedPart.ClaimId} not found.");
        }

        var totalBefore = claim.TotalCost ?? 0m;
        var partTotal = (usedPart.PartCost ?? 0m) * Math.Max(1, usedPart.Quantity);

        _context.UsedParts.Remove(usedPart);

        if (partTotal > 0)
        {
            var updatedTotal = totalBefore - partTotal;
            claim.TotalCost = updatedTotal <= 0 ? 0 : updatedTotal;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _context.ClaimStatusLogs.Add(new ClaimStatusLog
        {
            ClaimId = usedPart.ClaimId,
            OldStatus = claim.StatusCode,
            NewStatus = claim.StatusCode,
            ChangedByUserId = removedByUserId,
            ChangedAt = DateTime.UtcNow,
            Comment = $"Removed part {usedPart.PartId} x{usedPart.Quantity}"
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteClaimAsync(int claimId, int technicianId, DateOnly? completionDate, string? comment, CancellationToken cancellationToken = default)
    {
        var claim = await _context.WarrantyClaims.FirstOrDefaultAsync(c => c.ClaimId == claimId, cancellationToken);
        if (claim is null)
        {
            throw new InvalidOperationException($"Claim {claimId} not found.");
        }

        var oldStatus = claim.StatusCode;
        claim.StatusCode = "Completed";
        claim.TechnicianId = technicianId;
        claim.CompletionDate = completionDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (!string.IsNullOrWhiteSpace(comment))
        {
            claim.Note = comment;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _context.ClaimStatusLogs.Add(new ClaimStatusLog
        {
            ClaimId = claim.ClaimId,
            OldStatus = oldStatus,
            NewStatus = claim.StatusCode,
            ChangedByUserId = technicianId,
            ChangedAt = DateTime.UtcNow,
            Comment = comment ?? "Work completed"
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ArchiveClaimAsync(int claimId, int archiverUserId, string? comment, CancellationToken cancellationToken = default)
    {
        var claim = await _context.WarrantyClaims.FirstOrDefaultAsync(c => c.ClaimId == claimId, cancellationToken);
        if (claim is null)
        {
            throw new InvalidOperationException($"Claim {claimId} not found.");
        }

        var history = new WarrantyHistory
        {
            ClaimId = claim.ClaimId,
            Vin = claim.Vin,
            VehicleId = claim.VehicleId,
            ServiceCenterId = claim.ServiceCenterId,
            CompletedByUserId = claim.TechnicianId,
            CompletionDate = claim.CompletionDate,
            TotalCost = claim.TotalCost,
            Note = claim.Note,
            ArchivedAt = DateTime.UtcNow
        };

        _context.WarrantyHistories.Add(history);

        var oldStatus = claim.StatusCode;
        claim.StatusCode = "Closed";

        await _context.SaveChangesAsync(cancellationToken);

        _context.ClaimStatusLogs.Add(new ClaimStatusLog
        {
            ClaimId = claim.ClaimId,
            OldStatus = oldStatus,
            NewStatus = claim.StatusCode,
            ChangedByUserId = archiverUserId,
            ChangedAt = DateTime.UtcNow,
            Comment = comment ?? "Archived"
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClaimStatusLog>> GetStatusLogsAsync(int claimId, CancellationToken cancellationToken = default)
    {
        return await _context.ClaimStatusLogs
            .AsNoTracking()
            .Where(log => log.ClaimId == claimId)
            .OrderByDescending(log => log.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, int>> GetClaimCountsByStatusAsync(CancellationToken cancellationToken = default)
    {
        var data = await _context.WarrantyClaims
            .AsNoTracking()
            .GroupBy(c => c.StatusCode)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return data.ToDictionary(x => x.Key, x => x.Count);
    }

    public async Task<IReadOnlyDictionary<string, int>> GetClaimCountsByModelAsync(CancellationToken cancellationToken = default)
    {
        var data = await _context.WarrantyClaims
            .AsNoTracking()
            .GroupJoin(
                _context.Vehicles.AsNoTracking(),
                claim => claim.VehicleId,
                vehicle => vehicle.VehicleId,
                (claim, vehicles) => new { claim.ClaimId, Vehicles = vehicles })
            .SelectMany(x => x.Vehicles.DefaultIfEmpty(), (x, vehicle) => new { x.ClaimId, Model = vehicle != null ? vehicle.Model : null })
            .GroupBy(x => x.Model ?? "Unknown")
            .Select(group => new { Key = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return data.ToDictionary(x => x.Key, x => x.Count);
    }

    public async Task<IReadOnlyDictionary<string, int>> GetClaimCountsByMonthAsync(int year, CancellationToken cancellationToken = default)
    {
        var data = await _context.WarrantyClaims
            .AsNoTracking()
            .Where(c => c.CreatedAt.Year == year)
            .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
            .Select(group => new { group.Key.Year, group.Key.Month, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return data.ToDictionary(
            x => $"{x.Year:D4}-{x.Month:D2}",
            x => x.Count);
    }

    public async Task<IReadOnlyList<WarrantyHistory>> GetArchivedClaimsAsync(int userId, string role, CancellationToken cancellationToken = default)
    {
        IQueryable<WarrantyHistory> histories = _context.WarrantyHistories
            .AsNoTracking()
            .Include(h => h.CompletedByUser)
            .Include(h => h.ServiceCenter)
            .Include(h => h.Vehicle);

        if (string.Equals(role, "SC Technician", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "SC", StringComparison.OrdinalIgnoreCase))
        {
            histories = histories.Where(h => h.CompletedByUserId == userId);
        }
        else if (string.Equals(role, "SC Staff", StringComparison.OrdinalIgnoreCase))
        {
            var claimIds = _context.WarrantyClaims
                .Where(c => c.CreatedByUserId == userId)
                .Select(c => c.ClaimId);

            histories = histories.Where(h => claimIds.Contains(h.ClaimId));
        }

        return await histories
            .OrderByDescending(h => h.ArchivedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<WarrantyHistory?> GetArchivedClaimAsync(int historyId, CancellationToken cancellationToken = default)
    {
        return await _context.WarrantyHistories
            .AsNoTracking()
            .Include(h => h.CompletedByUser)
            .Include(h => h.ServiceCenter)
            .Include(h => h.Vehicle)
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.HistoryId == historyId, cancellationToken);
    }
}

