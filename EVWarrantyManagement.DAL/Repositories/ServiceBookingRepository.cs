using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EVWarrantyManagement.BO.Constants;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVWarrantyManagement.DAL.Repositories;

public class ServiceBookingRepository : IServiceBookingRepository
{
    private static readonly string[] ActiveStatusesForScheduling =
    {
        ServiceBookingStatuses.Pending,
        ServiceBookingStatuses.Approved,
        ServiceBookingStatuses.InProgress
    };

    private readonly EVWarrantyManagementContext _context;

    public ServiceBookingRepository(EVWarrantyManagementContext context)
    {
        _context = context;
    }

    public async Task<ServiceBooking?> GetByIdAsync(int bookingId, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceBookings
            .AsNoTracking()
            .Include(b => b.Customer)
            .Include(b => b.Vehicle)
                .ThenInclude(v => v.Customer)
            .Include(b => b.ServiceCenter)
            .Include(b => b.AssignedTechnician)
            .Include(b => b.ApprovedByUser)
            .Include(b => b.StatusLogs)
                .ThenInclude(log => log.ChangedByUser)
            .Include(b => b.ServiceBookingParts)
                .ThenInclude(p => p.Part)
            .Include(b => b.ServiceBookingParts)
                .ThenInclude(p => p.CreatedByUser)
            .FirstOrDefaultAsync(b => b.ServiceBookingId == bookingId, cancellationToken);
    }

    public async Task<int> CreateAsync(ServiceBooking booking, CancellationToken cancellationToken = default)
    {
        booking.CreatedAt = booking.CreatedAt == default ? DateTime.UtcNow : booking.CreatedAt;
        booking.UpdatedAt = DateTime.UtcNow;
        if (booking.EstimatedDurationMinutes <= 0)
        {
            booking.EstimatedDurationMinutes = 60;
        }
        booking.PreferredEnd ??= booking.PreferredStart.AddMinutes(booking.EstimatedDurationMinutes);

        await _context.ServiceBookings.AddAsync(booking, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return booking.ServiceBookingId;
    }

    public async Task UpdateAsync(ServiceBooking booking, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ServiceBookings
            .FirstOrDefaultAsync(b => b.ServiceBookingId == booking.ServiceBookingId, cancellationToken);

        if (existing == null)
        {
            throw new InvalidOperationException($"Service booking #{booking.ServiceBookingId} not found.");
        }

        _context.Entry(existing).CurrentValues.SetValues(booking);

        existing.UpdatedAt = DateTime.UtcNow;
        if (existing.EstimatedDurationMinutes <= 0)
        {
            existing.EstimatedDurationMinutes = 60;
        }

        existing.PreferredEnd ??= existing.PreferredStart.AddMinutes(existing.EstimatedDurationMinutes);
        existing.ConfirmedEnd ??= existing.ConfirmedStart?.AddMinutes(existing.EstimatedDurationMinutes);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceBooking>> GetByCustomerAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceBookings
            .AsNoTracking()
            .Where(b => b.CustomerId == customerId)
            .Include(b => b.ServiceCenter)
            .Include(b => b.AssignedTechnician)
            .OrderByDescending(b => b.PreferredStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceBooking>> GetTechnicianBookingsAsync(int technicianUserId, string? statusFilter, CancellationToken cancellationToken = default)
    {
        IQueryable<ServiceBooking> query = _context.ServiceBookings
            .AsNoTracking()
            .Where(b => b.AssignedTechnicianId == technicianUserId)
            .Include(b => b.Customer)
            .Include(b => b.Vehicle)
            .Include(b => b.ServiceCenter);

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(b => b.Status == statusFilter);
        }

        return await query
            .OrderBy(b => b.PreferredStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceBooking>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ServiceBookings
            .AsNoTracking()
            .Where(b => b.Status == ServiceBookingStatuses.Pending)
            .Include(b => b.Customer)
            .Include(b => b.Vehicle)
            .Include(b => b.ServiceCenter)
            .OrderBy(b => b.PreferredStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceBooking>> GetByServiceCenterAsync(int serviceCenterId, string? statusFilter, CancellationToken cancellationToken = default)
    {
        IQueryable<ServiceBooking> query = _context.ServiceBookings
            .AsNoTracking()
            .Where(b => b.ServiceCenterId == serviceCenterId)
            .Include(b => b.Customer)
            .Include(b => b.Vehicle)
            .Include(b => b.AssignedTechnician);

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(b => b.Status == statusFilter);
        }

        return await query
            .OrderBy(b => b.PreferredStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceBooking>> GetCompletedForCustomerAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceBookings
            .AsNoTracking()
            .Where(b => b.CustomerId == customerId && b.Status == ServiceBookingStatuses.Completed)
            .Include(b => b.ServiceCenter)
            .Include(b => b.AssignedTechnician)
            .Include(b => b.StatusLogs)
            .OrderByDescending(b => b.CompletedAt ?? b.PreferredStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceBooking>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ServiceBookings
            .AsNoTracking()
            .Include(b => b.Customer)
            .Include(b => b.Vehicle)
            .Include(b => b.ServiceCenter)
            .Include(b => b.AssignedTechnician)
            .Include(b => b.ServiceBookingParts)
                .ThenInclude(p => p.Part)
            .Include(b => b.ServiceBookingParts)
                .ThenInclude(p => p.CreatedByUser)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceBookingPart>> GetBookingPartsAsync(int bookingId, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceBookingParts
            .AsNoTracking()
            .Where(p => p.ServiceBookingId == bookingId)
            .Include(p => p.Part)
            .Include(p => p.CreatedByUser)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceBookingPart?> GetBookingPartAsync(int bookingPartId, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceBookingParts
            .AsNoTracking()
            .Include(p => p.Part)
            .FirstOrDefaultAsync(p => p.ServiceBookingPartId == bookingPartId, cancellationToken);
    }

    public async Task<ServiceBookingPart> AddBookingPartAsync(ServiceBookingPart bookingPart, CancellationToken cancellationToken = default)
    {
        bookingPart.CreatedAt = DateTime.UtcNow;
        await _context.ServiceBookingParts.AddAsync(bookingPart, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return bookingPart;
    }

    public async Task RemoveBookingPartAsync(int bookingPartId, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ServiceBookingParts
            .FirstOrDefaultAsync(p => p.ServiceBookingPartId == bookingPartId, cancellationToken);

        if (existing == null)
        {
            return;
        }

        _context.ServiceBookingParts.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceCenterTechnician>> GetAvailableTechniciansAsync(int serviceCenterId, DateTime preferredStart, TimeSpan? duration, int? excludeBookingId, CancellationToken cancellationToken = default)
    {
        var effectiveDuration = duration ?? TimeSpan.FromHours(1);
        var dayStart = preferredStart.Date;
        var dayEnd = dayStart.AddDays(1);

        var techniciansQuery = _context.ServiceCenterTechnicians
            .AsNoTracking()
            .Include(sct => sct.User)
            .Where(sct => sct.ServiceCenterId == serviceCenterId && sct.IsActive);

        var candidateBookings = await _context.ServiceBookings
            .AsNoTracking()
            .Where(b => b.ServiceCenterId == serviceCenterId)
            .Where(b => b.AssignedTechnicianId != null)
            .Where(b => ActiveStatusesForScheduling.Contains(b.Status))
            .Where(b => excludeBookingId == null || b.ServiceBookingId != excludeBookingId)
            .Where(b => (b.ConfirmedStart ?? b.PreferredStart) >= dayStart && (b.ConfirmedStart ?? b.PreferredStart) < dayEnd)
            .Select(b => new
            {
                b.AssignedTechnicianId,
                Start = b.ConfirmedStart ?? b.PreferredStart,
                End = b.ConfirmedEnd ?? b.PreferredEnd,
                b.EstimatedDurationMinutes
            })
            .ToListAsync(cancellationToken);

        var requestedStart = preferredStart;
        var requestedEnd = requestedStart.Add(effectiveDuration);

        var unavailableTechnicians = new HashSet<int>();

        foreach (var booking in candidateBookings)
        {
            if (!booking.AssignedTechnicianId.HasValue)
            {
                continue;
            }

            var start = booking.Start;
            var end = booking.End ?? booking.Start.AddMinutes(Math.Max(booking.EstimatedDurationMinutes, 15));

            var overlaps = start < requestedEnd && end > requestedStart;
            if (overlaps)
            {
                unavailableTechnicians.Add(booking.AssignedTechnicianId.Value);
            }
        }

        var technicians = await techniciansQuery.ToListAsync(cancellationToken);
        return technicians
            .Where(sct => !unavailableTechnicians.Contains(sct.UserId))
            .OrderBy(sct => sct.User.FullName)
            .ToList();
    }

    public async Task AddStatusLogAsync(ServiceBookingStatusLog statusLog, CancellationToken cancellationToken = default)
    {
        await _context.ServiceBookingStatusLogs.AddAsync(statusLog, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceBookingStatusLog>> GetStatusLogsAsync(int bookingId, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceBookingStatusLogs
            .AsNoTracking()
            .Where(log => log.ServiceBookingId == bookingId)
            .Include(log => log.ChangedByUser)
            .OrderByDescending(log => log.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<BookingStatisticsResult> GetStatisticsAsync(int? year, CancellationToken cancellationToken = default)
    {
        var bookingsQuery = _context.ServiceBookings.AsNoTracking();

        if (year.HasValue)
        {
            var yearStart = new DateTime(year.Value, 1, 1);
            var yearEnd = yearStart.AddYears(1);
            bookingsQuery = bookingsQuery.Where(b => b.PreferredStart >= yearStart && b.PreferredStart < yearEnd);
        }

        var bookings = await bookingsQuery.ToListAsync(cancellationToken);

        var byStatus = bookings.GroupBy(b => b.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        var byServiceType = bookings.GroupBy(b => b.ServiceType)
            .ToDictionary(g => g.Key, g => g.Count());

        var byServiceCenterRaw = bookings.GroupBy(b => b.ServiceCenterId)
            .ToDictionary(g => g.Key, g => g.Count());

        var serviceCenterIds = byServiceCenterRaw.Keys.Where(id => id > 0).ToList();
        var serviceCenterNames = await _context.ServiceCenters
            .AsNoTracking()
            .Where(sc => serviceCenterIds.Contains(sc.ServiceCenterId))
            .ToDictionaryAsync(sc => sc.ServiceCenterId, sc => sc.Name, cancellationToken);

        var byServiceCenter = byServiceCenterRaw
            .ToDictionary(
                kvp => serviceCenterNames.TryGetValue(kvp.Key, out var name) ? name : $"Center #{kvp.Key}",
                kvp => kvp.Value);

        var byMonth = bookings
            .GroupBy(b => b.PreferredStart.Month)
            .ToDictionary(g => g.Key, g => g.Count());

        byStatus.TryGetValue(ServiceBookingStatuses.Pending, out var pending);
        byStatus.TryGetValue(ServiceBookingStatuses.Approved, out var approved);
        byStatus.TryGetValue(ServiceBookingStatuses.InProgress, out var inProgress);
        byStatus.TryGetValue(ServiceBookingStatuses.Completed, out var completed);
        byStatus.TryGetValue(ServiceBookingStatuses.Cancelled, out var cancelled);
        byStatus.TryGetValue(ServiceBookingStatuses.Rejected, out var rejected);

        return new BookingStatisticsResult(
            bookings.Count,
            pending,
            approved,
            inProgress,
            completed,
            cancelled,
            rejected,
            byServiceType,
            byServiceCenter,
            byMonth
        );
    }

    public async Task<IReadOnlyList<ServiceBookingSummaryRow>> GetRecentBookingsSummaryAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceBookings
            .AsNoTracking()
            .Include(b => b.Customer)
            .Include(b => b.ServiceCenter)
            .OrderByDescending(b => b.CreatedAt)
            .Take(count)
            .Select(b => new ServiceBookingSummaryRow(
                b.ServiceBookingId,
                b.Customer.FullName,
                b.ServiceCenter.Name,
                b.ServiceType,
                b.Status,
                b.PreferredStart,
                b.CompletedAt))
            .ToListAsync(cancellationToken);
    }
}
