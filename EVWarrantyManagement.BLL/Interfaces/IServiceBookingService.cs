using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.DAL.Interfaces;

namespace EVWarrantyManagement.BLL.Interfaces;

public interface IServiceBookingService
{
    Task<ServiceBooking?> GetBookingAsync(int bookingId, CancellationToken cancellationToken = default);

    Task<int> CreateBookingAsync(ServiceBooking booking, int customerId, int vehicleId, int serviceCenterId, CancellationToken cancellationToken = default);

    Task UpdateBookingAsync(ServiceBooking booking, CancellationToken cancellationToken = default);

    Task ApproveBookingAsync(int bookingId, int approverUserId, int? technicianUserId, DateTime? confirmedStart, TimeSpan? duration, string? internalNote, CancellationToken cancellationToken = default);

    Task RejectBookingAsync(int bookingId, int approverUserId, string? rejectionReason, CancellationToken cancellationToken = default);

    Task StartBookingAsync(int bookingId, int technicianUserId, CancellationToken cancellationToken = default);

    Task CompleteBookingAsync(int bookingId, int technicianUserId, string? internalNote, CancellationToken cancellationToken = default);

    Task CancelBookingAsync(int bookingId, int userId, string? reason, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceBooking>> GetCustomerBookingsAsync(int customerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceBooking>> GetTechnicianBookingsAsync(int technicianUserId, string? statusFilter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceBooking>> GetPendingBookingsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceBooking>> GetServiceCenterBookingsAsync(int serviceCenterId, string? statusFilter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceBooking>> GetCompletedBookingsForCustomerAsync(int customerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceBooking>> GetAllBookingsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceBookingPart>> GetBookingPartsAsync(int bookingId, CancellationToken cancellationToken = default);

    Task<ServiceBookingPart> AddBookingPartAsync(int bookingId, int partId, int quantity, decimal? partCost, string? note, int userId, CancellationToken cancellationToken = default);

    Task RemoveBookingPartAsync(int bookingPartId, int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceCenterTechnician>> GetAvailableTechniciansAsync(int serviceCenterId, DateTime preferredStart, TimeSpan? duration, int? excludeBookingId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceBookingStatusLog>> GetStatusLogsAsync(int bookingId, CancellationToken cancellationToken = default);

    Task<BookingStatisticsResult> GetStatisticsAsync(int? year, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceBookingSummaryRow>> GetRecentBookingsSummaryAsync(int count, CancellationToken cancellationToken = default);
}
