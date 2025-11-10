using System.Threading;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.DAL.Interfaces;

namespace EVWarrantyManagement.BLL.Services;

public class PartService : IPartService
{
    private readonly IPartRepository _partRepository;

    public PartService(IPartRepository partRepository)
    {
        _partRepository = partRepository;
    }

    public Task<IReadOnlyList<Part>> GetPartsAsync(CancellationToken cancellationToken = default)
    {
        return _partRepository.GetAllAsync(cancellationToken);
    }

    public Task<Part?> GetPartAsync(int partId, CancellationToken cancellationToken = default)
    {
        return _partRepository.GetByIdAsync(partId, cancellationToken);
    }

    public Task<Part?> GetPartByCodeAsync(string partCode, CancellationToken cancellationToken = default)
    {
        return _partRepository.GetByCodeAsync(partCode, cancellationToken);
    }

    public Task<Part> CreatePartAsync(Part part, CancellationToken cancellationToken = default)
    {
        return _partRepository.CreateAsync(part, cancellationToken);
    }

    public Task UpdatePartAsync(Part part, CancellationToken cancellationToken = default)
    {
        return _partRepository.UpdateAsync(part, cancellationToken);
    }

    public Task DeletePartAsync(int partId, CancellationToken cancellationToken = default)
    {
        return _partRepository.DeleteAsync(partId, cancellationToken);
    }
}

