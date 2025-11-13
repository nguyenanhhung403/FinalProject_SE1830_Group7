using System;
using System.Threading;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.DAL.Interfaces;

namespace EVWarrantyManagement.BLL.Services;

public class ServiceCenterService : IServiceCenterService
{
    private readonly IServiceCenterRepository _serviceCenterRepository;

    public ServiceCenterService(IServiceCenterRepository serviceCenterRepository)
    {
        _serviceCenterRepository = serviceCenterRepository;
    }

    public Task<IReadOnlyList<ServiceCenter>> GetAllServiceCentersAsync(CancellationToken cancellationToken = default)
    {
        return _serviceCenterRepository.GetAllAsync(cancellationToken);
    }

    public Task<ServiceCenter?> GetServiceCenterByIdAsync(int serviceCenterId, CancellationToken cancellationToken = default)
    {
        return _serviceCenterRepository.GetByIdAsync(serviceCenterId, cancellationToken);
    }

    public Task<ServiceCenter> CreateServiceCenterAsync(ServiceCenter serviceCenter, CancellationToken cancellationToken = default)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(serviceCenter.Name))
        {
            throw new ArgumentException("Service center name is required.", nameof(serviceCenter));
        }

        return _serviceCenterRepository.CreateAsync(serviceCenter, cancellationToken);
    }

    public Task UpdateServiceCenterAsync(ServiceCenter serviceCenter, CancellationToken cancellationToken = default)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(serviceCenter.Name))
        {
            throw new ArgumentException("Service center name is required.", nameof(serviceCenter));
        }

        return _serviceCenterRepository.UpdateAsync(serviceCenter, cancellationToken);
    }

    public Task DeleteServiceCenterAsync(int serviceCenterId, CancellationToken cancellationToken = default)
    {
        return _serviceCenterRepository.DeleteAsync(serviceCenterId, cancellationToken);
    }

    public Task<IReadOnlyList<ServiceCenterTechnician>> GetTechniciansAsync(int serviceCenterId, CancellationToken cancellationToken = default)
    {
        return _serviceCenterRepository.GetTechniciansAsync(serviceCenterId, cancellationToken);
    }

    public Task AssignTechnicianAsync(int serviceCenterId, int userId, int assignedByUserId, CancellationToken cancellationToken = default)
    {
        return _serviceCenterRepository.AssignTechnicianAsync(serviceCenterId, userId, assignedByUserId, cancellationToken);
    }

    public Task UnassignTechnicianAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _serviceCenterRepository.UnassignTechnicianAsync(userId, cancellationToken);
    }

    public Task<IReadOnlyList<User>> GetAvailableTechniciansAsync(CancellationToken cancellationToken = default)
    {
        return _serviceCenterRepository.GetAvailableTechniciansAsync(cancellationToken);
    }

    public Task<ServiceCenterStats> GetServiceCenterStatsAsync(int serviceCenterId, CancellationToken cancellationToken = default)
    {
        return _serviceCenterRepository.GetServiceCenterStatsAsync(serviceCenterId, cancellationToken);
    }
}

