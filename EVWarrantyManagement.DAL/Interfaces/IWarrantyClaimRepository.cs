using System.Collections.Generic;
using System.Threading;
using EVWarrantyManagement.BO.Models;

namespace EVWarrantyManagement.DAL.Interfaces;

public interface IWarrantyClaimRepository
{
    Task<WarrantyClaim?> GetByIdAsync(int claimId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarrantyClaim>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarrantyClaim>> GetPendingClaimsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarrantyClaim>> GetClaimsByServiceCenterAsync(int serviceCenterId, CancellationToken cancellationToken = default);

    Task<WarrantyClaim> CreateAsync(WarrantyClaim claim, int createdByUserId, string? comment, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(int claimId, string newStatus, int changedByUserId, string? note, decimal? cost, CancellationToken cancellationToken = default);

    Task AddUsedPartAsync(UsedPart usedPart, int addedByUserId, CancellationToken cancellationToken = default);

    Task CompleteClaimAsync(int claimId, int technicianId, DateOnly? completionDate, string? comment, CancellationToken cancellationToken = default);

    Task ArchiveClaimAsync(int claimId, int archiverUserId, string? comment, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClaimStatusLog>> GetStatusLogsAsync(int claimId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, int>> GetClaimCountsByStatusAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, int>> GetClaimCountsByModelAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, int>> GetClaimCountsByMonthAsync(int year, CancellationToken cancellationToken = default);

    Task<decimal> GetTotalRevenueAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, decimal>> GetRevenueByMonthAsync(int year, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, decimal>> GetRevenueByServiceCenterAsync(int? year, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarrantyHistory>> GetArchivedClaimsAsync(int userId, string role, CancellationToken cancellationToken = default);

    Task<WarrantyHistory?> GetArchivedClaimAsync(int historyId, CancellationToken cancellationToken = default);

    Task RemoveUsedPartAsync(int usedPartId, int removedByUserId, CancellationToken cancellationToken = default);

    Task AssignTechnicianAsync(int claimId, int technicianId, int assignedByUserId, CancellationToken cancellationToken = default);

    Task RevertToPendingAsync(int claimId, int userId, string? note, CancellationToken cancellationToken = default);
}

