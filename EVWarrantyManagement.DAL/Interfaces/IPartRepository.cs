using System.Threading;
using EVWarrantyManagement.BO.Models;

namespace EVWarrantyManagement.DAL.Interfaces;

public interface IPartRepository
{
    Task<IReadOnlyList<Part>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Part?> GetByIdAsync(int partId, CancellationToken cancellationToken = default);

    Task<Part?> GetByCodeAsync(string partCode, CancellationToken cancellationToken = default);

    Task<Part> CreateAsync(Part part, CancellationToken cancellationToken = default);

    Task UpdateAsync(Part part, CancellationToken cancellationToken = default);

    Task DeleteAsync(int partId, CancellationToken cancellationToken = default);
}

