using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Constants;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.DAL.Interfaces;

namespace EVWarrantyManagement.BLL.Services;

public class ServiceBookingService : IServiceBookingService
{
    private readonly IServiceBookingRepository _repository;
    private readonly IPartService _partService;

    private static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(1);

    public ServiceBookingService(IServiceBookingRepository repository, IPartService partService)
    {
        _repository = repository;
        _partService = partService;
    }

    public Task<ServiceBooking?> GetBookingAsync(int bookingId, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(bookingId, cancellationToken);

    public async Task<int> CreateBookingAsync(ServiceBooking booking, int customerId, int vehicleId, int serviceCenterId, CancellationToken cancellationToken = default)
    {
        booking.CustomerId = customerId;
        booking.VehicleId = vehicleId;
        booking.ServiceCenterId = serviceCenterId;
        booking.Status = ServiceBookingStatuses.Pending;
        booking.EstimatedDurationMinutes = booking.EstimatedDurationMinutes > 0 ? booking.EstimatedDurationMinutes : (int)DefaultDuration.TotalMinutes;

        var bookingId = await _repository.CreateAsync(booking, cancellationToken);

        await LogStatusChangeAsync(bookingId, null, ServiceBookingStatuses.Pending, null, cancellationToken);
        return bookingId;
    }

    public async Task UpdateBookingAsync(ServiceBooking booking, CancellationToken cancellationToken = default)
    {
        booking.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(booking, cancellationToken);
    }

    public async Task ApproveBookingAsync(int bookingId, int approverUserId, int? technicianUserId, DateTime? confirmedStart, TimeSpan? duration, string? internalNote, CancellationToken cancellationToken = default)
    {
        var booking = await EnsureBookingAsync(bookingId, cancellationToken);

        if (booking.Status is ServiceBookingStatuses.Rejected or ServiceBookingStatuses.Cancelled)
        {
            throw new InvalidOperationException("Cannot approve a booking that is rejected or cancelled.");
        }

        var oldStatus = booking.Status;
        booking.Status = ServiceBookingStatuses.Approved;
        booking.ApprovedAt = DateTime.UtcNow;
        booking.ApprovedByUserId = approverUserId;
        booking.AssignedTechnicianId = technicianUserId ?? booking.AssignedTechnicianId;
        booking.InternalNote = internalNote ?? booking.InternalNote;

        if (confirmedStart.HasValue)
        {
            booking.ConfirmedStart = confirmedStart.Value;
            booking.PreferredStart = confirmedStart.Value;
        }

        var effectiveDuration = duration ?? TimeSpan.FromMinutes(booking.EstimatedDurationMinutes <= 0 ? DefaultDuration.TotalMinutes : booking.EstimatedDurationMinutes);
        booking.EstimatedDurationMinutes = (int)Math.Max(15, Math.Round(effectiveDuration.TotalMinutes));
        booking.PreferredEnd = booking.PreferredStart.AddMinutes(booking.EstimatedDurationMinutes);
        booking.ConfirmedEnd = booking.ConfirmedStart?.AddMinutes(booking.EstimatedDurationMinutes);

        await _repository.UpdateAsync(booking, cancellationToken);
        await LogStatusChangeAsync(bookingId, oldStatus, booking.Status, approverUserId, cancellationToken);
    }

    public async Task RejectBookingAsync(int bookingId, int approverUserId, string? rejectionReason, CancellationToken cancellationToken = default)
    {
        var booking = await EnsureBookingAsync(bookingId, cancellationToken);

        if (booking.Status is ServiceBookingStatuses.Completed or ServiceBookingStatuses.Cancelled)
        {
            throw new InvalidOperationException("Cannot reject a completed or cancelled booking.");
        }

        var oldStatus = booking.Status;
        booking.Status = ServiceBookingStatuses.Rejected;
        booking.RejectionReason = rejectionReason;
        booking.ApprovedByUserId = approverUserId;
        booking.ApprovedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(booking, cancellationToken);
        await LogStatusChangeAsync(bookingId, oldStatus, booking.Status, approverUserId, cancellationToken);
    }

    public async Task StartBookingAsync(int bookingId, int technicianUserId, CancellationToken cancellationToken = default)
    {
        var booking = await EnsureBookingAsync(bookingId, cancellationToken);

        if (booking.Status != ServiceBookingStatuses.Approved && booking.Status != ServiceBookingStatuses.Pending)
        {
            throw new InvalidOperationException("Only approved (or pending) bookings can be started.");
        }

        var oldStatus = booking.Status;
        booking.Status = ServiceBookingStatuses.InProgress;
        booking.AssignedTechnicianId = technicianUserId;
        booking.ConfirmedStart ??= DateTime.UtcNow;

        if (booking.EstimatedDurationMinutes <= 0)
        {
            booking.EstimatedDurationMinutes = (int)DefaultDuration.TotalMinutes;
        }

        booking.ConfirmedEnd ??= booking.ConfirmedStart.Value.AddMinutes(booking.EstimatedDurationMinutes);

        await _repository.UpdateAsync(booking, cancellationToken);
        await LogStatusChangeAsync(bookingId, oldStatus, booking.Status, technicianUserId, cancellationToken);
    }

    public async Task CompleteBookingAsync(int bookingId, int technicianUserId, string? internalNote, CancellationToken cancellationToken = default)
    {
        var booking = await EnsureBookingAsync(bookingId, cancellationToken);

        if (booking.Status != ServiceBookingStatuses.InProgress)
        {
            throw new InvalidOperationException("Only in-progress bookings can be completed.");
        }

        var oldStatus = booking.Status;
        booking.Status = ServiceBookingStatuses.Completed;
        booking.CompletedAt = DateTime.UtcNow;
        booking.InternalNote = internalNote ?? booking.InternalNote;
        booking.AssignedTechnicianId ??= technicianUserId;
        booking.ConfirmedEnd = booking.CompletedAt;

        await _repository.UpdateAsync(booking, cancellationToken);
        await LogStatusChangeAsync(bookingId, oldStatus, booking.Status, technicianUserId, cancellationToken);
    }

    public async Task CancelBookingAsync(int bookingId, int userId, string? reason, CancellationToken cancellationToken = default)
    {
        var booking = await EnsureBookingAsync(bookingId, cancellationToken);

        if (booking.Status == ServiceBookingStatuses.Completed)
        {
            throw new InvalidOperationException("Completed bookings cannot be cancelled.");
        }

        var oldStatus = booking.Status;
        booking.Status = ServiceBookingStatuses.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        booking.CancelledByUserId = userId;
        booking.InternalNote = reason ?? booking.InternalNote;

        await _repository.UpdateAsync(booking, cancellationToken);
        await LogStatusChangeAsync(bookingId, oldStatus, booking.Status, userId, cancellationToken);
    }

    public Task<IReadOnlyList<ServiceBooking>> GetCustomerBookingsAsync(int customerId, CancellationToken cancellationToken = default)
        => _repository.GetByCustomerAsync(customerId, cancellationToken);

    public Task<IReadOnlyList<ServiceBooking>> GetTechnicianBookingsAsync(int technicianUserId, string? statusFilter, CancellationToken cancellationToken = default)
        => _repository.GetTechnicianBookingsAsync(technicianUserId, statusFilter, cancellationToken);

    public Task<IReadOnlyList<ServiceBooking>> GetPendingBookingsAsync(CancellationToken cancellationToken = default)
        => _repository.GetPendingAsync(cancellationToken);

    public Task<IReadOnlyList<ServiceBooking>> GetServiceCenterBookingsAsync(int serviceCenterId, string? statusFilter, CancellationToken cancellationToken = default)
        => _repository.GetByServiceCenterAsync(serviceCenterId, statusFilter, cancellationToken);

    public Task<IReadOnlyList<ServiceBooking>> GetCompletedBookingsForCustomerAsync(int customerId, CancellationToken cancellationToken = default)
        => _repository.GetCompletedForCustomerAsync(customerId, cancellationToken);

    public Task<IReadOnlyList<ServiceBooking>> GetAllBookingsAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    public Task<IReadOnlyList<ServiceBookingPart>> GetBookingPartsAsync(int bookingId, CancellationToken cancellationToken = default)
        => _repository.GetBookingPartsAsync(bookingId, cancellationToken);

    public async Task<ServiceBookingPart> AddBookingPartAsync(int bookingId, int partId, int quantity, decimal? partCost, string? note, int userId, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        var booking = await EnsureBookingAsync(bookingId, cancellationToken);

        if (!string.Equals(booking.Status, ServiceBookingStatuses.InProgress, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Parts can only be added when the booking is in progress.");
        }

        if (booking.AssignedTechnicianId != userId)
        {
            throw new InvalidOperationException("You are not assigned to this booking.");
        }

        var part = await _partService.GetPartAsync(partId, cancellationToken);
        if (part is null)
        {
            throw new InvalidOperationException("Part not found.");
        }

        var effectiveCost = partCost ?? ((part.UnitPrice ?? 0m) * quantity);

        await _partService.ConsumeStockForBookingAsync(partId, quantity, bookingId, userId, cancellationToken);

        var bookingPart = new ServiceBookingPart
        {
            ServiceBookingId = bookingId,
            PartId = partId,
            Quantity = quantity,
            PartCost = effectiveCost,
            Note = note,
            CreatedByUserId = userId
        };

        var created = await _repository.AddBookingPartAsync(bookingPart, cancellationToken);

        booking.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(booking, cancellationToken);

        return created;
    }

    public async Task RemoveBookingPartAsync(int bookingPartId, int userId, CancellationToken cancellationToken = default)
    {
        var bookingPart = await _repository.GetBookingPartAsync(bookingPartId, cancellationToken);
        if (bookingPart == null)
        {
            throw new InvalidOperationException("Booking part not found.");
        }

        var booking = await EnsureBookingAsync(bookingPart.ServiceBookingId, cancellationToken);

        if (!string.Equals(booking.Status, ServiceBookingStatuses.InProgress, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Parts can only be removed when the booking is in progress.");
        }

        if (booking.AssignedTechnicianId != userId)
        {
            throw new InvalidOperationException("You are not assigned to this booking.");
        }

        await _repository.RemoveBookingPartAsync(bookingPartId, cancellationToken);
        await _partService.ReleaseStockForBookingAsync(bookingPart.PartId, bookingPart.Quantity, bookingPart.ServiceBookingId, userId, cancellationToken);

        booking.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(booking, cancellationToken);
    }

    public Task<IReadOnlyList<ServiceCenterTechnician>> GetAvailableTechniciansAsync(int serviceCenterId, DateTime preferredStart, TimeSpan? duration, int? excludeBookingId, CancellationToken cancellationToken = default)
        => _repository.GetAvailableTechniciansAsync(serviceCenterId, preferredStart, duration ?? DefaultDuration, excludeBookingId, cancellationToken);

    public Task<IReadOnlyList<ServiceBookingStatusLog>> GetStatusLogsAsync(int bookingId, CancellationToken cancellationToken = default)
        => _repository.GetStatusLogsAsync(bookingId, cancellationToken);

    public Task<BookingStatisticsResult> GetStatisticsAsync(int? year, CancellationToken cancellationToken = default)
        => _repository.GetStatisticsAsync(year, cancellationToken);

    public Task<IReadOnlyList<ServiceBookingSummaryRow>> GetRecentBookingsSummaryAsync(int count, CancellationToken cancellationToken = default)
        => _repository.GetRecentBookingsSummaryAsync(count, cancellationToken);

    private async Task<ServiceBooking> EnsureBookingAsync(int bookingId, CancellationToken cancellationToken)
    {
        var booking = await _repository.GetByIdAsync(bookingId, cancellationToken);
        if (booking is null)
        {
            throw new InvalidOperationException($"Service booking #{bookingId} was not found.");
        }

        return booking;
    }

    private Task LogStatusChangeAsync(int bookingId, string? oldStatus, string newStatus, int? changedByUserId, CancellationToken cancellationToken)
    {
        var log = new ServiceBookingStatusLog
        {
            ServiceBookingId = bookingId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow
        };

        return _repository.AddStatusLogAsync(log, cancellationToken);
    }
}
