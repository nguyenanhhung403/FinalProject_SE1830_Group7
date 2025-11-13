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

    // Inventory methods
    Task<IReadOnlyList<Part>> GetLowStockPartsAsync(int? threshold = null, CancellationToken cancellationToken = default);

    Task<PartInventory?> GetInventoryAsync(int partId, CancellationToken cancellationToken = default);

    Task ReserveStockAsync(int partId, int quantity, int claimId, int userId, CancellationToken cancellationToken = default);

    Task ReleaseStockAsync(int partId, int quantity, int claimId, int userId, CancellationToken cancellationToken = default);

    Task AdjustStockAsync(int partId, int quantity, string movementType, string? reason, int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PartStockMovement>> GetStockMovementsAsync(int partId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PartStockMovement>> GetRecentStockMovementsAsync(int limit = 50, CancellationToken cancellationToken = default);

    Task ConsumeStockAsync(int partId, int quantity, int claimId, int userId, CancellationToken cancellationToken = default);

    Task ConsumeStockForBookingAsync(int partId, int quantity, int bookingId, int userId, CancellationToken cancellationToken = default);

    Task ReleaseStockForBookingAsync(int partId, int quantity, int bookingId, int userId, CancellationToken cancellationToken = default);

    Task UpdateMinStockLevelAsync(int partId, int? minStockLevel, int userId, CancellationToken cancellationToken = default);
}

