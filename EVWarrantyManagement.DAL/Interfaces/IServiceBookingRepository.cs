using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EVWarrantyManagement.BO.Models;

namespace EVWarrantyManagement.DAL.Interfaces;

public interface IServiceBookingRepository
{
    Task<ServiceBooking?> GetByIdAsync(int bookingId, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(ServiceBooking booking, CancellationToken cancellationToken = default);

    Task UpdateAsync(ServiceBooking booking, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceBooking>> GetByCustomerAsync(int customerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceBooking>> GetTechnicianBookingsAsync(int technicianUserId, string? statusFilter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceBooking>> GetPendingAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceBooking>> GetByServiceCenterAsync(int serviceCenterId, string? statusFilter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceBooking>> GetCompletedForCustomerAsync(int customerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceBooking>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceCenterTechnician>> GetAvailableTechniciansAsync(int serviceCenterId, DateTime preferredStart, TimeSpan? duration, int? excludeBookingId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceBookingPart>> GetBookingPartsAsync(int bookingId, CancellationToken cancellationToken = default);

    Task<ServiceBookingPart?> GetBookingPartAsync(int bookingPartId, CancellationToken cancellationToken = default);

    Task<ServiceBookingPart> AddBookingPartAsync(ServiceBookingPart bookingPart, CancellationToken cancellationToken = default);

    Task RemoveBookingPartAsync(int bookingPartId, CancellationToken cancellationToken = default);

    Task AddStatusLogAsync(ServiceBookingStatusLog statusLog, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceBookingStatusLog>> GetStatusLogsAsync(int bookingId, CancellationToken cancellationToken = default);

    Task<BookingStatisticsResult> GetStatisticsAsync(int? year, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceBookingSummaryRow>> GetRecentBookingsSummaryAsync(int count, CancellationToken cancellationToken = default);
}

public record BookingStatisticsResult(
    int TotalBookings,
    int Pending,
    int Approved,
    int InProgress,
    int Completed,
    int Cancelled,
    int Rejected,
    IReadOnlyDictionary<string, int> BookingsByServiceType,
    IReadOnlyDictionary<string, int> BookingsByServiceCenter,
    IReadOnlyDictionary<int, int> BookingsByMonth
);

public record ServiceBookingSummaryRow(
    int ServiceBookingId,
    string CustomerName,
    string ServiceCenterName,
    string ServiceType,
    string Status,
    DateTime PreferredStart,
    DateTime? CompletedAt
);
