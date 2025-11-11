using System.Threading;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.DAL.Interfaces;

namespace EVWarrantyManagement.BLL.Services;

public class WarrantyClaimService : IWarrantyClaimService
{
    private readonly IWarrantyClaimRepository _warrantyClaimRepository;
    private readonly INotificationService _notificationService;

    public WarrantyClaimService(
        IWarrantyClaimRepository warrantyClaimRepository,
        INotificationService notificationService)
    {
        _warrantyClaimRepository = warrantyClaimRepository;
        _notificationService = notificationService;
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

    public async Task<WarrantyClaim> CreateClaimAsync(WarrantyClaim claim, int createdByUserId, string? comment, CancellationToken cancellationToken = default)
    {
        var createdClaim = await _warrantyClaimRepository.CreateAsync(claim, createdByUserId, comment, cancellationToken);

        // Send real-time notification to EVM Staff
        await _notificationService.NotifyNewClaimCreatedAsync(
            createdClaim.ClaimId,
            createdClaim.Vin ?? "Unknown",
            createdClaim.ServiceCenter?.Name ?? "Unknown Service Center");

        return createdClaim;
    }

    public async Task ApproveClaimAsync(int claimId, int reviewerUserId, string? note, decimal? cost, CancellationToken cancellationToken = default)
    {
        await _warrantyClaimRepository.UpdateStatusAsync(claimId, "Approved", reviewerUserId, note, cost, cancellationToken);

        // Send real-time notification
        await _notificationService.NotifyClaimStatusChangedAsync(claimId, "Approved", "Pending", note);
    }

    public async Task RejectClaimAsync(int claimId, int reviewerUserId, string? note, CancellationToken cancellationToken = default)
    {
        await _warrantyClaimRepository.UpdateStatusAsync(claimId, "Rejected", reviewerUserId, note, null, cancellationToken);

        // Send real-time notification
        await _notificationService.NotifyClaimStatusChangedAsync(claimId, "Rejected", "Pending", note);
    }

    public async Task PutClaimOnHoldAsync(int claimId, int reviewerUserId, string? note, CancellationToken cancellationToken = default)
    {
        await _warrantyClaimRepository.UpdateStatusAsync(claimId, "OnHold", reviewerUserId, note, null, cancellationToken);

        // Send real-time notification
        await _notificationService.NotifyClaimStatusChangedAsync(claimId, "OnHold", "Pending", note);
    }

    public async Task StartRepairAsync(int claimId, int technicianUserId, string? note, CancellationToken cancellationToken = default)
    {
        await _warrantyClaimRepository.UpdateStatusAsync(claimId, "InProgress", technicianUserId, note, null, cancellationToken);

        // Send real-time notification
        await _notificationService.NotifyClaimStatusChangedAsync(claimId, "InProgress", "Approved", note);
    }

    public Task AddUsedPartAsync(UsedPart usedPart, int addedByUserId, CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.AddUsedPartAsync(usedPart, addedByUserId, cancellationToken);
    }

    public async Task CompleteClaimAsync(int claimId, int technicianUserId, DateOnly? completionDate, string? comment, CancellationToken cancellationToken = default)
    {
        await _warrantyClaimRepository.CompleteClaimAsync(claimId, technicianUserId, completionDate, comment, cancellationToken);

        // Send real-time notification
        await _notificationService.NotifyClaimStatusChangedAsync(claimId, "Completed", "InProgress", comment);
    }

    public async Task ArchiveClaimAsync(int claimId, int archiverUserId, string? comment, CancellationToken cancellationToken = default)
    {
        await _warrantyClaimRepository.ArchiveClaimAsync(claimId, archiverUserId, comment, cancellationToken);

        // Send real-time notification
        await _notificationService.NotifyClaimStatusChangedAsync(claimId, "Closed", "Completed", comment);
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

