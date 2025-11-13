using System.Threading;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.DAL.Interfaces;

namespace EVWarrantyManagement.BLL.Services;

public class ReportingService : IReportingService
{
    private readonly IWarrantyClaimRepository _warrantyClaimRepository;

    public ReportingService(IWarrantyClaimRepository warrantyClaimRepository)
    {
        _warrantyClaimRepository = warrantyClaimRepository;
    }

    public Task<IReadOnlyDictionary<string, int>> GetClaimCountsByStatusAsync(CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.GetClaimCountsByStatusAsync(cancellationToken);
    }

    public Task<IReadOnlyDictionary<string, int>> GetClaimCountsByModelAsync(CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.GetClaimCountsByModelAsync(cancellationToken);
    }

    public Task<IReadOnlyDictionary<string, int>> GetClaimCountsByMonthAsync(int year, CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.GetClaimCountsByMonthAsync(year, cancellationToken);
    }

    public Task<decimal> GetTotalRevenueAsync(CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.GetTotalRevenueAsync(cancellationToken);
    }

    public Task<IReadOnlyDictionary<string, decimal>> GetRevenueByMonthAsync(int year, CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.GetRevenueByMonthAsync(year, cancellationToken);
    }

    public Task<IReadOnlyDictionary<string, decimal>> GetRevenueByServiceCenterAsync(int? year, CancellationToken cancellationToken = default)
    {
        return _warrantyClaimRepository.GetRevenueByServiceCenterAsync(year, cancellationToken);
    }
}

