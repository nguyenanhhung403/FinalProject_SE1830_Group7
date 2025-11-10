using System.Threading;
using EVWarrantyManagement.BO.Models;

namespace EVWarrantyManagement.BLL.Interfaces;

public interface IPartService
{
    Task<IReadOnlyList<Part>> GetPartsAsync(CancellationToken cancellationToken = default);

    Task<Part?> GetPartAsync(int partId, CancellationToken cancellationToken = default);

    Task<Part?> GetPartByCodeAsync(string partCode, CancellationToken cancellationToken = default);

    Task<Part> CreatePartAsync(Part part, CancellationToken cancellationToken = default);

    Task UpdatePartAsync(Part part, CancellationToken cancellationToken = default);

    Task DeletePartAsync(int partId, CancellationToken cancellationToken = default);
}

