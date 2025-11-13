using System.Linq;
using System.Security.Claims;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Constants;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EVWarrantyManagement.Pages.Bookings;

[Authorize(Policy = "RequireTechnician")]
public class MyTasksModel : PageModel
{
    private readonly IServiceBookingService _serviceBookingService;
    private readonly IPartService _partService;
    private readonly IHubContext<NotificationHub> _notificationHub;

    public MyTasksModel(IServiceBookingService serviceBookingService, IPartService partService, IHubContext<NotificationHub> notificationHub)
    {
        _serviceBookingService = serviceBookingService;
        _partService = partService;
        _notificationHub = notificationHub;
    }

    public IReadOnlyList<ServiceBooking> UpcomingBookings { get; private set; } = Array.Empty<ServiceBooking>();

    public IReadOnlyList<ServiceBooking> InProgressBookings { get; private set; } = Array.Empty<ServiceBooking>();

    public IReadOnlyList<ServiceBooking> CompletedBookings { get; private set; } = Array.Empty<ServiceBooking>();

    public Dictionary<int, IReadOnlyList<ServiceBookingPart>> BookingParts { get; private set; } = new();

    public List<SelectListItem> PartOptions { get; private set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostStartAsync(int bookingId)
    {
        var technicianId = GetUserId();
        if (technicianId == 0)
        {
            TempData["Error"] = "Unable to resolve your user context.";
            return RedirectToPage();
        }

        try
        {
            await _serviceBookingService.StartBookingAsync(bookingId, technicianId, HttpContext.RequestAborted);

            await NotifyStatusChangeAsync(bookingId, ServiceBookingStatuses.InProgress, technicianId, "Service booking started.");

            TempData["Success"] = "Booking marked as in progress.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to start booking: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCompleteAsync(int bookingId, string? internalNote)
    {
        var technicianId = GetUserId();
        if (technicianId == 0)
        {
            TempData["Error"] = "Unable to resolve your user context.";
            return RedirectToPage();
        }

        try
        {
            await _serviceBookingService.CompleteBookingAsync(bookingId, technicianId, internalNote, HttpContext.RequestAborted);

            await NotifyStatusChangeAsync(bookingId, ServiceBookingStatuses.Completed, technicianId, internalNote);

            TempData["Success"] = "Booking marked as completed.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to complete booking: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddPartAsync(int bookingId, int partId, int quantity, string? note)
    {
        var technicianId = GetUserId();
        if (technicianId == 0)
        {
            TempData["Error"] = "Unable to resolve your user context.";
            return RedirectToPage();
        }

        if (quantity <= 0)
        {
            TempData["Error"] = "Quantity must be greater than zero.";
            return RedirectToPage();
        }

        try
        {
            await _serviceBookingService.AddBookingPartAsync(bookingId, partId, quantity, null, note, technicianId, HttpContext.RequestAborted);
            TempData["Success"] = "Added repair part to booking.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to add part: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemovePartAsync(int bookingPartId)
    {
        var technicianId = GetUserId();
        if (technicianId == 0)
        {
            TempData["Error"] = "Unable to resolve your user context.";
            return RedirectToPage();
        }

        try
        {
            await _serviceBookingService.RemoveBookingPartAsync(bookingPartId, technicianId, HttpContext.RequestAborted);
            TempData["Success"] = "Removed repair part from booking.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to remove part: {ex.Message}";
        }

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var technicianId = GetUserId();
        if (technicianId == 0)
        {
            UpcomingBookings = Array.Empty<ServiceBooking>();
            InProgressBookings = Array.Empty<ServiceBooking>();
            BookingParts = new Dictionary<int, IReadOnlyList<ServiceBookingPart>>();
            PartOptions = new List<SelectListItem>();
            return;
        }

        var bookings = await _serviceBookingService.GetTechnicianBookingsAsync(technicianId, null, HttpContext.RequestAborted);

        UpcomingBookings = bookings
            .Where(b => b.Status == ServiceBookingStatuses.Pending || b.Status == ServiceBookingStatuses.Approved)
            .OrderBy(b => b.PreferredStart)
            .ToList();

        InProgressBookings = bookings
            .Where(b => b.Status == ServiceBookingStatuses.InProgress)
            .OrderBy(b => b.ConfirmedStart ?? b.PreferredStart)
            .ToList();

        CompletedBookings = bookings
            .Where(b => b.Status == ServiceBookingStatuses.Completed)
            .OrderByDescending(b => b.CompletedAt ?? b.ConfirmedEnd ?? b.PreferredEnd ?? b.PreferredStart)
            .Take(20)
            .ToList();

        var allParts = await _partService.GetPartsAsync(HttpContext.RequestAborted);
        PartOptions = allParts
            .OrderBy(p => p.PartName)
            .Select(p => new SelectListItem
            {
                Value = p.PartId.ToString(),
                Text = $"{p.PartCode} - {p.PartName}"
            })
            .ToList();

        var bookingIds = InProgressBookings
            .Select(b => b.ServiceBookingId)
            .Distinct();

        var partsDictionary = new Dictionary<int, IReadOnlyList<ServiceBookingPart>>();
        foreach (var id in bookingIds)
        {
            var parts = await _serviceBookingService.GetBookingPartsAsync(id, HttpContext.RequestAborted);
            partsDictionary[id] = parts;
        }

        BookingParts = partsDictionary;
    }

    private async Task NotifyStatusChangeAsync(int bookingId, string newStatus, int technicianId, string? note)
    {
        var booking = await _serviceBookingService.GetBookingAsync(bookingId, HttpContext.RequestAborted);
        if (booking == null)
        {
            return;
        }

        var message = newStatus switch
        {
            ServiceBookingStatuses.InProgress => $"Technician {booking.AssignedTechnician?.FullName ?? ""} started booking #{bookingId}.",
            ServiceBookingStatuses.Completed => $"Booking #{bookingId} for {booking.Customer?.FullName} has been completed.",
            _ => $"Booking #{bookingId} updated to {newStatus}."
        };

        var notificationPayload = new
        {
            Type = newStatus == ServiceBookingStatuses.Completed ? "booking_completed" : "booking_status_changed",
            Title = $"Booking {newStatus}",
            Message = message,
            BookingId = bookingId,
            Status = newStatus,
            NewStatus = newStatus,
            TechnicianId = technicianId,
            Note = note,
            CustomerId = booking.CustomerId
        };

        // Send to role groups
        await _notificationHub.Clients.Groups("EVM Staff", "EVM", "Admin")
            .SendAsync("ReceiveNotification", notificationPayload, HttpContext.RequestAborted);

        // Send booking-specific update
        await _notificationHub.Clients.Group($"Booking_{bookingId}")
            .SendAsync("ReceiveBookingUpdate", notificationPayload, HttpContext.RequestAborted);

        if (newStatus == ServiceBookingStatuses.Completed)
        {
            var customerNotification = new
            {
                Type = "booking_completed",
                Title = "Service Completed",
                Message = $"Service booking #{bookingId} for {booking.Vehicle?.Vin} has been completed.",
                BookingId = bookingId,
                Status = newStatus
            };

            await _notificationHub.Clients.Groups("Customer")
                .SendAsync("ReceiveNotification", customerNotification, HttpContext.RequestAborted);

            // Also send to customer user if we have customer ID
            if (booking.CustomerId > 0)
            {
                await _notificationHub.Clients.User(booking.CustomerId.ToString())
                    .SendAsync("ReceiveNotification", customerNotification, HttpContext.RequestAborted);
            }
        }
    }

    private int GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var userId) ? userId : 0;
    }
}
