using System.Threading;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.DAL.Interfaces;

namespace EVWarrantyManagement.BLL.Services;

public class WarrantyClaimService : IWarrantyClaimService
{
    private readonly IWarrantyClaimRepository _warrantyClaimRepository;

    public WarrantyClaimService(IWarrantyClaimRepository warrantyClaimRepository)
    {
        _warrantyClaimRepository = warrantyClaimRepository;
    }

    public Task<WarrantyClaim?> GetClaimAsync(int claimId, CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.GetByIdAsync(claimId, cancellationToken);
    }

    public Task<IReadOnlyList<WarrantyClaim>> GetPendingClaimsAsync(CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.GetPendingClaimsAsync(cancellationToken);
    }

    public Task<IReadOnlyList<WarrantyClaim>> GetAllClaimsAsync(CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.GetAllAsync(cancellationToken);
    }

    public Task<IReadOnlyList<WarrantyClaim>> GetClaimsByServiceCenterAsync(int serviceCenterId, CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.GetClaimsByServiceCenterAsync(serviceCenterId, cancellationToken);
    }

    public Task<WarrantyClaim> CreateClaimAsync(WarrantyClaim claim, int createdByUserId, string? comment, CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.CreateAsync(claim, createdByUserId, comment, cancellationToken);
    }

    public Task ApproveClaimAsync(int claimId, int reviewerUserId, string? note, decimal? cost, CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.UpdateStatusAsync(claimId, "Approved", reviewerUserId, note, cost, cancellationToken);
    }

    public Task RejectClaimAsync(int claimId, int reviewerUserId, string? note, CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.UpdateStatusAsync(claimId, "Rejected", reviewerUserId, note, null, cancellationToken);
    }

    public Task PutClaimOnHoldAsync(int claimId, int reviewerUserId, string? note, CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.UpdateStatusAsync(claimId, "OnHold", reviewerUserId, note, null, cancellationToken);
    }

    public Task StartRepairAsync(int claimId, int technicianUserId, string? note, CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.UpdateStatusAsync(claimId, "InProgress", technicianUserId, note, null, cancellationToken);
    }

    public Task AddUsedPartAsync(UsedPart usedPart, int addedByUserId, CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.AddUsedPartAsync(usedPart, addedByUserId, cancellationToken);
    }

    public Task RemoveUsedPartAsync(int usedPartId, int removedByUserId, CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.RemoveUsedPartAsync(usedPartId, removedByUserId, cancellationToken);
    }

    public Task CompleteClaimAsync(int claimId, int technicianUserId, DateOnly? completionDate, string? comment, CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.CompleteClaimAsync(claimId, technicianUserId, completionDate, comment, cancellationToken);
    }

    public Task ArchiveClaimAsync(int claimId, int archiverUserId, string? comment, CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.ArchiveClaimAsync(claimId, archiverUserId, comment, cancellationToken);
    }

    public Task<IReadOnlyList<ClaimStatusLog>> GetStatusLogsAsync(int claimId, CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.GetStatusLogsAsync(claimId, cancellationToken);
    }

    public Task<IReadOnlyList<WarrantyHistory>> GetArchivedClaimsAsync(int userId, string role, CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.GetArchivedClaimsAsync(userId, role, cancellationToken);
    }

    public Task<WarrantyHistory?> GetArchivedClaimAsync(int historyId, CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.GetArchivedClaimAsync(historyId, cancellationToken);
    }
}

