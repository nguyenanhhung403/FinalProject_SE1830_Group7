using System;
using System.Collections.Generic;
using System.Linq;
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

    public Task<IReadOnlyList<Part>> GetLowStockPartsAsync(int? threshold = null, CancellationToken cancellationToken = default)
    {
        return _partRepository.GetLowStockPartsAsync(threshold, cancellationToken);
    }

    public Task<PartInventory?> GetInventoryAsync(int partId, CancellationToken cancellationToken = default)
    {
        return _partRepository.GetInventoryAsync(partId, cancellationToken);
    }

    public Task ReserveStockAsync(int partId, int quantity, int claimId, int userId, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        return _partRepository.ReserveStockAsync(partId, quantity, claimId, userId, cancellationToken);
    }

    public Task ReleaseStockAsync(int partId, int quantity, int claimId, int userId, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        return _partRepository.ReleaseStockAsync(partId, quantity, claimId, userId, cancellationToken);
    }

    public Task AdjustStockAsync(int partId, int quantity, string movementType, string? reason, int userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(movementType))
        {
            throw new ArgumentException("Movement type is required.", nameof(movementType));
        }

        if (!new[] { "IN", "OUT", "ADJUSTMENT" }.Contains(movementType.ToUpper()))
        {
            throw new ArgumentException("Invalid movement type. Must be IN, OUT, or ADJUSTMENT.", nameof(movementType));
        }

        return _partRepository.AdjustStockAsync(partId, quantity, movementType.ToUpper(), reason, userId, cancellationToken);
    }

    public Task<IReadOnlyList<PartStockMovement>> GetStockMovementsAsync(int partId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        return _partRepository.GetStockMovementsAsync(partId, fromDate, toDate, cancellationToken);
    }

    public Task<IReadOnlyList<PartStockMovement>> GetRecentStockMovementsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        return _partRepository.GetRecentStockMovementsAsync(limit, cancellationToken);
    }

    public Task ConsumeStockAsync(int partId, int quantity, int claimId, int userId, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        return _partRepository.ConsumeStockAsync(partId, quantity, claimId, userId, cancellationToken);
    }

    public Task ConsumeStockForBookingAsync(int partId, int quantity, int bookingId, int userId, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        return _partRepository.ConsumeStockForBookingAsync(partId, quantity, bookingId, userId, cancellationToken);
    }

    public Task ReleaseStockForBookingAsync(int partId, int quantity, int bookingId, int userId, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        return _partRepository.ReleaseStockForBookingAsync(partId, quantity, bookingId, userId, cancellationToken);
    }

    public Task UpdateMinStockLevelAsync(int partId, int? minStockLevel, int userId, CancellationToken cancellationToken = default)
    {
        if (minStockLevel.HasValue && minStockLevel.Value < 0)
        {
            throw new ArgumentException("Min stock level cannot be negative.", nameof(minStockLevel));
        }

        return _partRepository.UpdateMinStockLevelAsync(partId, minStockLevel, userId, cancellationToken);
    }
}

