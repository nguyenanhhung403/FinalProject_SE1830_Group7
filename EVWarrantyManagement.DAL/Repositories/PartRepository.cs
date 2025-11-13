using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVWarrantyManagement.DAL.Repositories;

public class PartRepository : IPartRepository
{
    private readonly EVWarrantyManagementContext _context;

    public PartRepository(EVWarrantyManagementContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Part>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Parts
            .AsNoTracking()
            .OrderBy(p => p.PartName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Part?> GetByIdAsync(int partId, CancellationToken cancellationToken = default)
    {
        return await _context.Parts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PartId == partId, cancellationToken);
    }

    public async Task<Part?> GetByCodeAsync(string partCode, CancellationToken cancellationToken = default)
    {
        return await _context.Parts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PartCode == partCode, cancellationToken);
    }

    public async Task<Part> CreateAsync(Part part, CancellationToken cancellationToken = default)
    {
        part.CreatedAt = DateTime.UtcNow;
        _context.Parts.Add(part);
        await _context.SaveChangesAsync(cancellationToken);
        return part;
    }

    public async Task UpdateAsync(Part part, CancellationToken cancellationToken = default)
    {
        _context.Parts.Update(part);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int partId, CancellationToken cancellationToken = default)
    {
        var part = await _context.Parts.FirstOrDefaultAsync(p => p.PartId == partId, cancellationToken);
        if (part is null)
        {
            return;
        }

        _context.Parts.Remove(part);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Part>> GetLowStockPartsAsync(int? threshold = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Parts
            .AsNoTracking()
            .Include(p => p.PartInventory)
            .Where(p => p.PartInventory != null 
                && p.PartInventory.MinStockLevel.HasValue
                && p.PartInventory.StockQuantity < p.PartInventory.MinStockLevel.Value);

        if (threshold.HasValue)
        {
            query = query.Where(p => p.PartInventory!.StockQuantity < threshold.Value);
        }

        return await query
            .OrderBy(p => p.PartInventory!.StockQuantity)
            .ToListAsync(cancellationToken);
    }

    public async Task<PartInventory?> GetInventoryAsync(int partId, CancellationToken cancellationToken = default)
    {
        return await _context.PartInventories
            .AsNoTracking()
            .Include(pi => pi.UpdatedByUser)
            .FirstOrDefaultAsync(pi => pi.PartId == partId, cancellationToken);
    }

    public async Task ReserveStockAsync(int partId, int quantity, int claimId, int userId, CancellationToken cancellationToken = default)
    {
        var inventory = await _context.PartInventories
            .FirstOrDefaultAsync(pi => pi.PartId == partId, cancellationToken);

        if (inventory == null)
        {
            // Create inventory if doesn't exist
            inventory = new PartInventory
            {
                PartId = partId,
                StockQuantity = 0,
                LastUpdated = DateTime.UtcNow,
                UpdatedByUserId = userId
            };
            _context.PartInventories.Add(inventory);
        }

        if (inventory.StockQuantity < quantity)
        {
            throw new InvalidOperationException($"Insufficient stock. Available: {inventory.StockQuantity}, Requested: {quantity}");
        }

        // Update stock
        inventory.StockQuantity -= quantity;
        inventory.LastUpdated = DateTime.UtcNow;
        inventory.UpdatedByUserId = userId;

        // Create movement record
        var movement = new PartStockMovement
        {
            PartId = partId,
            MovementType = "RESERVED",
            Quantity = -quantity,
            ReferenceType = "CLAIM",
            ReferenceId = claimId,
            Note = $"Reserved for claim #{claimId}",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.PartStockMovements.Add(movement);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReleaseStockAsync(int partId, int quantity, int claimId, int userId, CancellationToken cancellationToken = default)
    {
        var inventory = await _context.PartInventories
            .FirstOrDefaultAsync(pi => pi.PartId == partId, cancellationToken);

        if (inventory == null)
        {
            // Create inventory if doesn't exist
            inventory = new PartInventory
            {
                PartId = partId,
                StockQuantity = 0,
                LastUpdated = DateTime.UtcNow,
                UpdatedByUserId = userId
            };
            _context.PartInventories.Add(inventory);
        }

        // Update stock
        inventory.StockQuantity += quantity;
        inventory.LastUpdated = DateTime.UtcNow;
        inventory.UpdatedByUserId = userId;

        // Create movement record
        var movement = new PartStockMovement
        {
            PartId = partId,
            MovementType = "RELEASED",
            Quantity = quantity,
            ReferenceType = "CLAIM",
            ReferenceId = claimId,
            Note = $"Released from claim #{claimId}",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.PartStockMovements.Add(movement);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AdjustStockAsync(int partId, int quantity, string movementType, string? reason, int userId, CancellationToken cancellationToken = default)
    {
        var inventory = await _context.PartInventories
            .FirstOrDefaultAsync(pi => pi.PartId == partId, cancellationToken);

        if (inventory == null)
        {
            inventory = new PartInventory
            {
                PartId = partId,
                StockQuantity = 0,
                LastUpdated = DateTime.UtcNow,
                UpdatedByUserId = userId
            };
            _context.PartInventories.Add(inventory);
        }

        // Update stock based on movement type
        if (movementType == "IN")
        {
            inventory.StockQuantity += Math.Abs(quantity);
        }
        else if (movementType == "OUT")
        {
            if (inventory.StockQuantity < Math.Abs(quantity))
            {
                throw new InvalidOperationException($"Insufficient stock for adjustment. Available: {inventory.StockQuantity}, Requested: {Math.Abs(quantity)}");
            }
            inventory.StockQuantity -= Math.Abs(quantity);
        }
        else if (movementType == "ADJUSTMENT")
        {
            inventory.StockQuantity += quantity; // Can be positive or negative
            if (inventory.StockQuantity < 0)
            {
                inventory.StockQuantity = 0;
            }
        }

        inventory.LastUpdated = DateTime.UtcNow;
        inventory.UpdatedByUserId = userId;

        // Create movement record
        var movement = new PartStockMovement
        {
            PartId = partId,
            MovementType = movementType,
            Quantity = movementType == "OUT" ? -Math.Abs(quantity) : quantity,
            ReferenceType = "ADJUSTMENT",
            ReferenceId = null,
            Note = reason ?? "Manual stock adjustment",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.PartStockMovements.Add(movement);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PartStockMovement>> GetStockMovementsAsync(int partId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var query = _context.PartStockMovements
            .AsNoTracking()
            .Include(psm => psm.CreatedByUser)
            .Where(psm => psm.PartId == partId);

        if (fromDate.HasValue)
        {
            query = query.Where(psm => psm.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(psm => psm.CreatedAt <= toDate.Value);
        }

        return await query
            .OrderByDescending(psm => psm.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PartStockMovement>> GetRecentStockMovementsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _context.PartStockMovements
            .AsNoTracking()
            .Include(psm => psm.CreatedByUser)
            .Include(psm => psm.Part)
            .OrderByDescending(psm => psm.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task ConsumeStockAsync(int partId, int quantity, int claimId, int userId, CancellationToken cancellationToken = default)
    {
        // Create movement record for consumption (OUT)
        var movement = new PartStockMovement
        {
            PartId = partId,
            MovementType = "OUT",
            Quantity = -quantity,
            ReferenceType = "CLAIM",
            ReferenceId = claimId,
            Note = $"Consumed for completed claim #{claimId}",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.PartStockMovements.Add(movement);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ConsumeStockForBookingAsync(int partId, int quantity, int bookingId, int userId, CancellationToken cancellationToken = default)
    {
        var inventory = await _context.PartInventories
            .FirstOrDefaultAsync(pi => pi.PartId == partId, cancellationToken);

        if (inventory == null)
        {
            inventory = new PartInventory
            {
                PartId = partId,
                StockQuantity = 0,
                LastUpdated = DateTime.UtcNow,
                UpdatedByUserId = userId
            };
            _context.PartInventories.Add(inventory);
        }

        if (inventory.StockQuantity < quantity)
        {
            throw new InvalidOperationException($"Insufficient stock. Available: {inventory.StockQuantity}, requested: {quantity}");
        }

        inventory.StockQuantity -= quantity;
        inventory.LastUpdated = DateTime.UtcNow;
        inventory.UpdatedByUserId = userId;

        var movement = new PartStockMovement
        {
            PartId = partId,
            MovementType = "OUT",
            Quantity = -quantity,
            ReferenceType = "BOOKING",
            ReferenceId = bookingId,
            Note = $"Used for booking #{bookingId}",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.PartStockMovements.Add(movement);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReleaseStockForBookingAsync(int partId, int quantity, int bookingId, int userId, CancellationToken cancellationToken = default)
    {
        var inventory = await _context.PartInventories
            .FirstOrDefaultAsync(pi => pi.PartId == partId, cancellationToken);

        if (inventory == null)
        {
            inventory = new PartInventory
            {
                PartId = partId,
                StockQuantity = 0,
                LastUpdated = DateTime.UtcNow,
                UpdatedByUserId = userId
            };
            _context.PartInventories.Add(inventory);
        }

        inventory.StockQuantity += quantity;
        inventory.LastUpdated = DateTime.UtcNow;
        inventory.UpdatedByUserId = userId;

        var movement = new PartStockMovement
        {
            PartId = partId,
            MovementType = "IN",
            Quantity = quantity,
            ReferenceType = "BOOKING",
            ReferenceId = bookingId,
            Note = $"Returned from booking #{bookingId}",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.PartStockMovements.Add(movement);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateMinStockLevelAsync(int partId, int? minStockLevel, int userId, CancellationToken cancellationToken = default)
    {
        var inventory = await _context.PartInventories
            .FirstOrDefaultAsync(pi => pi.PartId == partId, cancellationToken);

        if (inventory == null)
        {
            // Create inventory if it doesn't exist
            inventory = new PartInventory
            {
                PartId = partId,
                StockQuantity = 0,
                MinStockLevel = minStockLevel,
                LastUpdated = DateTime.UtcNow,
                UpdatedByUserId = userId
            };
            _context.PartInventories.Add(inventory);
        }
        else
        {
            inventory.MinStockLevel = minStockLevel;
            inventory.LastUpdated = DateTime.UtcNow;
            inventory.UpdatedByUserId = userId;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

