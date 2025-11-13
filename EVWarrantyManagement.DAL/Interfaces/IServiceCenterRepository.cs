using System.Collections.Generic;
using System.Threading;
using EVWarrantyManagement.BO.Models;

namespace EVWarrantyManagement.DAL.Interfaces;

public interface IServiceCenterRepository
{
    Task<IReadOnlyList<ServiceCenter>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ServiceCenter?> GetByIdAsync(int serviceCenterId, CancellationToken cancellationToken = default);

    Task<ServiceCenter> CreateAsync(ServiceCenter serviceCenter, CancellationToken cancellationToken = default);

    Task UpdateAsync(ServiceCenter serviceCenter, CancellationToken cancellationToken = default);

    Task DeleteAsync(int serviceCenterId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceCenterTechnician>> GetTechniciansAsync(int serviceCenterId, CancellationToken cancellationToken = default);

    Task AssignTechnicianAsync(int serviceCenterId, int userId, int assignedByUserId, CancellationToken cancellationToken = default);

    Task UnassignTechnicianAsync(int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetAvailableTechniciansAsync(CancellationToken cancellationToken = default);

    Task<ServiceCenterStats> GetServiceCenterStatsAsync(int serviceCenterId, CancellationToken cancellationToken = default);
}

public record ServiceCenterStats(
    int TotalClaims,
    int ActiveClaims,
    int CompletedClaimsThisMonth,
    int AssignedTechniciansCount
);

