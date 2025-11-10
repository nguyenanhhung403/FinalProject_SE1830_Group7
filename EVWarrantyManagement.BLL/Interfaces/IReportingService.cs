using System.Threading;

namespace EVWarrantyManagement.BLL.Interfaces;

public interface IReportingService
{
    Task<IReadOnlyDictionary<string, int>> GetClaimCountsByStatusAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, int>> GetClaimCountsByModelAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, int>> GetClaimCountsByMonthAsync(int year, CancellationToken cancellationToken = default);
}

