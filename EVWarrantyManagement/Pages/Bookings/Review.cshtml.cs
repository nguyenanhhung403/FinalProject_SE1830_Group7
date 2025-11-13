using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace EVWarrantyManagement.Pages.Bookings;

[Authorize(Policy = "RequireEVM")]
public class ReviewModel : PageModel
{
    private readonly IServiceBookingService _serviceBookingService;
    private readonly IServiceCenterService _serviceCenterService;
    private readonly IHubContext<NotificationHub> _notificationHub;

    public ReviewModel(
        IServiceBookingService serviceBookingService,
        IServiceCenterService serviceCenterService,
        IHubContext<NotificationHub> notificationHub)
    {
        _serviceBookingService = serviceBookingService;
        _serviceCenterService = serviceCenterService;
        _notificationHub = notificationHub;
    }

    public IReadOnlyList<ServiceBooking> PendingBookings { get; private set; } = Array.Empty<ServiceBooking>();

    [BindProperty]
    public ApproveInputModel ApproveInput { get; set; } = new();

    [BindProperty]
    public RejectInputModel RejectInput { get; set; } = new();

    public class ApproveInputModel
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime? ConfirmedDate { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan? ConfirmedTime { get; set; }

        public int? AssignedTechnicianId { get; set; }

        [Range(30, 480)]
        public int DurationMinutes { get; set; } = 60;

        [MaxLength(1000)]
        public string? InternalNote { get; set; }
    }

    public class RejectInputModel
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string RejectionReason { get; set; } = string.Empty;
    }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostApproveAsync()
    {
        ModelState.Clear();
        if (!TryValidateModel(ApproveInput, nameof(ApproveInput)))
        {
            TempData["Error"] = BuildModelStateErrorMessage("Unable to approve booking.");
            await LoadAsync();
            return Page();
    }

        var approverId = GetUserId();
        if (approverId == 0)
        {
            TempData["Error"] = "Unable to determine the current user. Please sign in again.";
            await LoadAsync();
            return Page();
        }

        var confirmedStart = CombineDateTime(ApproveInput.ConfirmedDate, ApproveInput.ConfirmedTime);
        var duration = TimeSpan.FromMinutes(Math.Max(30, ApproveInput.DurationMinutes));

        try
        {
            await _serviceBookingService.ApproveBookingAsync(
                ApproveInput.BookingId,
                approverId,
                ApproveInput.AssignedTechnicianId,
                confirmedStart,
                duration,
                ApproveInput.InternalNote);

            await NotifyBookingUpdateAsync(ApproveInput.BookingId, "Approved", ApproveInput.InternalNote);

            TempData["Success"] = "Booking approved successfully.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Unable to approve booking: {ex.Message}";
            await LoadAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostRejectAsync()
    {
        ModelState.Clear();
        if (!TryValidateModel(RejectInput, nameof(RejectInput)))
        {
            TempData["Error"] = BuildModelStateErrorMessage("Unable to reject booking.");
            await LoadAsync();
            return Page();
        }

        var approverId = GetUserId();
        if (approverId == 0)
        {
            TempData["Error"] = "Unable to determine the current user. Please sign in again.";
            await LoadAsync();
            return Page();
        }

        try
        {
            await _serviceBookingService.RejectBookingAsync(
                RejectInput.BookingId,
                approverId,
                RejectInput.RejectionReason);

            await NotifyBookingUpdateAsync(RejectInput.BookingId, "Rejected", RejectInput.RejectionReason);

            TempData["Success"] = "Booking rejected.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Unable to reject booking: {ex.Message}";
            await LoadAsync();
            return Page();
        }
    }

    private async Task LoadAsync()
    {
        PendingBookings = await _serviceBookingService.GetPendingBookingsAsync(HttpContext.RequestAborted);
    }

    private async Task NotifyBookingUpdateAsync(int bookingId, string newStatus, string? note)
    {
        var booking = await _serviceBookingService.GetBookingAsync(bookingId);
        if (booking == null)
        {
            return;
        }

        var notificationPayload = new
        {
            Type = "booking_status_changed",
            Title = $"Booking {newStatus}",
            Message = $"Booking #{bookingId} for {booking.Customer?.FullName} has been {newStatus.ToLowerInvariant()}.",
            BookingId = bookingId,
            Status = newStatus,
            NewStatus = newStatus,
            Note = note,
            CustomerId = booking.CustomerId,
            AssignedTechnicianId = booking.AssignedTechnicianId
        };

        // Send to role groups
        await _notificationHub.Clients.Groups("EVM Staff", "EVM", "Admin")
            .SendAsync("ReceiveNotification", notificationPayload, HttpContext.RequestAborted);

        // Send booking-specific update
        await _notificationHub.Clients.Group($"Booking_{bookingId}")
            .SendAsync("ReceiveBookingUpdate", notificationPayload, HttpContext.RequestAborted);

        // Notify customer if booking was approved or rejected
        if (newStatus == "Approved" || newStatus == "Rejected")
        {
            await _notificationHub.Clients.Groups("Customer")
                .SendAsync("ReceiveNotification", notificationPayload, HttpContext.RequestAborted);
        }

        // Notify assigned technician if booking was approved
        if (newStatus == "Approved" && booking.AssignedTechnicianId.HasValue)
        {
            await _notificationHub.Clients.User(booking.AssignedTechnicianId.Value.ToString())
                .SendAsync("ReceiveNotification", new
                {
                    Type = "booking_assigned",
                    Title = "Booking Assigned",
                    Message = $"You have been assigned to Booking #{bookingId}",
                    BookingId = bookingId,
                    Status = newStatus
                }, HttpContext.RequestAborted);
        }
    }

    public async Task<IActionResult> OnGetAvailableTechniciansAsync(int bookingId, DateTime date, TimeSpan time, int durationMinutes, CancellationToken cancellationToken)
    {
        var booking = await _serviceBookingService.GetBookingAsync(bookingId, cancellationToken);
        if (booking == null)
        {
            return new JsonResult(new { success = false, message = "Booking not found." }) { StatusCode = 404 };
        }

        var start = CombineDateTime(date, time);
        var duration = TimeSpan.FromMinutes(Math.Max(30, durationMinutes));

        var technicians = await _serviceBookingService.GetAvailableTechniciansAsync(
            booking.ServiceCenterId,
            start,
            duration,
            bookingId,
            cancellationToken);

        var payload = technicians
            .Select(t => new
            {
                id = t.UserId,
                name = string.IsNullOrWhiteSpace(t.User?.FullName) ? "Technician" : t.User.FullName
            })
            .ToList();

        return new JsonResult(new { success = true, technicians = payload });
    }

    private static DateTime CombineDateTime(DateTime? date, TimeSpan? time)
    {
        if (date is null || time is null)
        {
            throw new ArgumentException("Confirmed date and time are required.");
        }

        var localDate = DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Local);
        var dateTime = localDate.Add(time.Value);
        return dateTime;
    }

    private int GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var userId) ? userId : 0;
    }

    private string BuildModelStateErrorMessage(string defaultMessage)
    {
        var errors = ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .Select(kvp =>
            {
                var field = kvp.Key.Split('.').Last();
                var message = string.Join(" ", kvp.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage));
                return $"{field}: {message}";
            })
            .ToList();

        return errors.Count == 0
            ? defaultMessage
            : $"{defaultMessage} {string.Join(" ", errors)}";
    }
}
