using System.Threading;
using EVWarrantyManagement.BO.Models;

namespace EVWarrantyManagement.BLL.Interfaces;

public interface IWarrantyClaimService
{
    Task<WarrantyClaim?> GetClaimAsync(int claimId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarrantyClaim>> GetPendingClaimsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarrantyClaim>> GetAllClaimsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarrantyClaim>> GetClaimsByServiceCenterAsync(int serviceCenterId, CancellationToken cancellationToken = default);

    Task<WarrantyClaim> CreateClaimAsync(WarrantyClaim claim, int createdByUserId, string? comment, CancellationToken cancellationToken = default);

    Task ApproveClaimAsync(int claimId, int reviewerUserId, string? note, decimal? cost, CancellationToken cancellationToken = default);

    Task RejectClaimAsync(int claimId, int reviewerUserId, string? note, CancellationToken cancellationToken = default);

    Task PutClaimOnHoldAsync(int claimId, int reviewerUserId, string? note, CancellationToken cancellationToken = default);

    Task StartRepairAsync(int claimId, int technicianUserId, string? note, CancellationToken cancellationToken = default);

    Task AddUsedPartAsync(UsedPart usedPart, int addedByUserId, CancellationToken cancellationToken = default);

    Task RemoveUsedPartAsync(int usedPartId, int removedByUserId, CancellationToken cancellationToken = default);

    Task CompleteClaimAsync(int claimId, int technicianUserId, DateOnly? completionDate, string? comment, CancellationToken cancellationToken = default);

    Task ArchiveClaimAsync(int claimId, int archiverUserId, string? comment, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClaimStatusLog>> GetStatusLogsAsync(int claimId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarrantyHistory>> GetArchivedClaimsAsync(int userId, string role, CancellationToken cancellationToken = default);

    Task<WarrantyHistory?> GetArchivedClaimAsync(int historyId, CancellationToken cancellationToken = default);
}

