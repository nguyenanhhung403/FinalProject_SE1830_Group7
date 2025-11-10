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
}

