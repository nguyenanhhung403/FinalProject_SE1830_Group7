using System.Collections.Generic;
using System.Threading;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.DAL.Interfaces;

namespace EVWarrantyManagement.BLL.Interfaces;

public interface IServiceCenterService
{
    Task<IReadOnlyList<ServiceCenter>> GetAllServiceCentersAsync(CancellationToken cancellationToken = default);

    Task<ServiceCenter?> GetServiceCenterByIdAsync(int serviceCenterId, CancellationToken cancellationToken = default);

    Task<ServiceCenter> CreateServiceCenterAsync(ServiceCenter serviceCenter, CancellationToken cancellationToken = default);

    Task UpdateServiceCenterAsync(ServiceCenter serviceCenter, CancellationToken cancellationToken = default);

    Task DeleteServiceCenterAsync(int serviceCenterId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceCenterTechnician>> GetTechniciansAsync(int serviceCenterId, CancellationToken cancellationToken = default);

    Task AssignTechnicianAsync(int serviceCenterId, int userId, int assignedByUserId, CancellationToken cancellationToken = default);

    Task UnassignTechnicianAsync(int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetAvailableTechniciansAsync(CancellationToken cancellationToken = default);

    Task<ServiceCenterStats> GetServiceCenterStatsAsync(int serviceCenterId, CancellationToken cancellationToken = default);
}

