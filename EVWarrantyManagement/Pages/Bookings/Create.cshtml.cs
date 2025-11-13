using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Constants;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;

namespace EVWarrantyManagement.Pages.Bookings;

[Authorize(Roles = "Customer")]
public class CreateModel : PageModel
{
    private static readonly IReadOnlyList<SelectListItem> DefaultServiceTypes = new List<SelectListItem>
    {
        new("Routine Maintenance", "Routine Maintenance"),
        new("Repair", "Repair"),
        new("Inspection", "Inspection"),
        new("Parts Replacement", "Parts Replacement")
    };

    private readonly IServiceBookingService _serviceBookingService;
    private readonly IServiceCenterService _serviceCenterService;
    private readonly IVehicleService _vehicleService;
    private readonly ICustomerService _customerService;
    private readonly IAuthService _authService;
    private readonly IHubContext<NotificationHub> _notificationHub;

    public CreateModel(
        IServiceBookingService serviceBookingService,
        IServiceCenterService serviceCenterService,
        IVehicleService vehicleService,
        ICustomerService customerService,
        IAuthService authService,
        IHubContext<NotificationHub> notificationHub)
    {
        _serviceBookingService = serviceBookingService;
        _serviceCenterService = serviceCenterService;
        _vehicleService = vehicleService;
        _customerService = customerService;
        _authService = authService;
        _notificationHub = notificationHub;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> VehicleOptions { get; private set; } = new();

    public List<SelectListItem> ServiceCenterOptions { get; private set; } = new();

    public List<SelectListItem> ServiceTypeOptions { get; } = DefaultServiceTypes.ToList();

    public List<ServiceCenterTechnician> AvailableTechnicians { get; private set; } = new();

    public bool AvailabilityChecked { get; private set; }

    public string? ErrorMessage { get; private set; }

    public List<ServiceBooking> RecentBookings { get; private set; } = new();

    private Customer? CurrentCustomer { get; set; }

    public class InputModel
    {
        [Required]
        [Display(Name = "Vehicle")]
        public int? VehicleId { get; set; }

        [Required]
        [Display(Name = "Service Center")]
        public int? ServiceCenterId { get; set; }

        [Required]
        [Display(Name = "Service Type")]
        public string ServiceType { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Preferred Date")]
        public DateTime? PreferredDate { get; set; }

        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "Preferred Time")]
        public TimeSpan? PreferredTime { get; set; }

        [Range(30, 480)]
        [Display(Name = "Estimated Duration (minutes)")]
        public int EstimatedDurationMinutes { get; set; } = 60;

        [Display(Name = "Additional Notes")]
        [MaxLength(1000)]
        public string? CustomerNote { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCheckAvailabilityAsync()
    {
        await LoadAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var (customer, validationError) = await EnsureCustomerContextAsync();
        if (validationError != null)
        {
            ErrorMessage = validationError;
            return Page();
        }

        if (customer == null)
        {
            ErrorMessage = "Unable to resolve your customer profile. Please contact support.";
            return Page();
        }

        var preferredStart = CombinePreferredDateTime(Input);
        var duration = TimeSpan.FromMinutes(Math.Max(30, Input.EstimatedDurationMinutes));

        AvailableTechnicians = (await _serviceBookingService.GetAvailableTechniciansAsync(
            Input.ServiceCenterId!.Value,
            preferredStart,
            duration,
            null)).ToList();

        AvailabilityChecked = true;

        if (AvailableTechnicians.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "No technicians are available for the selected time. Please choose another slot.");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        await LoadAsync();

        var (customer, validationError) = await EnsureCustomerContextAsync();
        if (validationError != null)
        {
            ErrorMessage = validationError;
            return Page();
        }

        if (customer == null)
        {
            ErrorMessage = "Unable to resolve your customer profile. Please contact support.";
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var preferredStart = CombinePreferredDateTime(Input);
        if (preferredStart < DateTime.Now.AddMinutes(-5))
        {
            ModelState.AddModelError(string.Empty, "Preferred date and time must be in the future.");
            return Page();
        }

        var vehicle = await _vehicleService.GetVehicleAsync(Input.VehicleId!.Value);
        if (vehicle == null || vehicle.CustomerId != customer.CustomerId)
        {
            ModelState.AddModelError(string.Empty, "Selected vehicle is invalid.");
            return Page();
        }

        var booking = new ServiceBooking
        {
            VehicleId = vehicle.VehicleId,
            ServiceCenterId = Input.ServiceCenterId!.Value,
            ServiceType = Input.ServiceType,
            PreferredStart = preferredStart,
            PreferredEnd = preferredStart.AddMinutes(Math.Max(30, Input.EstimatedDurationMinutes)),
            CustomerNote = Input.CustomerNote,
            EstimatedDurationMinutes = Math.Max(30, Input.EstimatedDurationMinutes)
        };

        var bookingId = await _serviceBookingService.CreateBookingAsync(
            booking,
            customer.CustomerId,
            vehicle.VehicleId,
            Input.ServiceCenterId.Value);

        var currentUser = await GetCurrentUserAsync();
        var customerName = string.IsNullOrWhiteSpace(customer.FullName) ? (currentUser?.Username ?? "Customer") : customer.FullName;
        var vehicleVin = string.IsNullOrWhiteSpace(vehicle.Vin) ? "Unknown VIN" : vehicle.Vin;

        var notificationPayload = new
        {
            Type = "booking_created",
            Title = "New Service Booking",
            Message = $"Customer {customerName} requested {Input.ServiceType} on {preferredStart:dd/MM/yyyy HH:mm}",
            BookingId = bookingId,
            ServiceType = Input.ServiceType,
            PreferredStart = preferredStart,
            VehicleVin = vehicleVin,
            CustomerName = customerName
        };

        // Send to role groups
        await _notificationHub.Clients.Groups("EVM Staff", "EVM", "Admin")
            .SendAsync("ReceiveNotification", notificationPayload);

        // Send booking-specific update
        await _notificationHub.Clients.Group($"Booking_{bookingId}")
            .SendAsync("ReceiveBookingUpdate", notificationPayload);

        // Send new booking notification
        await _notificationHub.Clients.Groups("EVM Staff", "EVM", "Admin")
            .SendAsync("ReceiveNewBooking", notificationPayload);

        TempData["Success"] = "Booking request submitted successfully! Our staff will confirm the appointment soon.";
        return RedirectToPage("/Bookings/Create");
    }

    private async Task LoadAsync()
    {
        var user = await GetCurrentUserAsync();
        CurrentCustomer = user?.Email is not null
            ? await _customerService.GetCustomerByEmailAsync(user.Email)
            : null;

        var vehicles = await _vehicleService.GetVehiclesAsync();
        VehicleOptions = CurrentCustomer == null
            ? new List<SelectListItem>()
            : vehicles
                .Where(v => v.CustomerId == CurrentCustomer.CustomerId)
                .Select(v => new SelectListItem
                {
                    Value = v.VehicleId.ToString(),
                    Text = $"{v.Vin} ({v.Model})"
                })
                .ToList();

        if (CurrentCustomer != null)
        {
            var bookings = await _serviceBookingService.GetCustomerBookingsAsync(CurrentCustomer.CustomerId);
            RecentBookings = bookings
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .ToList();
        }
        else
        {
            RecentBookings.Clear();
        }

        var centers = await _serviceCenterService.GetAllServiceCentersAsync();
        ServiceCenterOptions = centers
            .Select(sc => new SelectListItem
            {
                Value = sc.ServiceCenterId.ToString(),
                Text = sc.Name
            })
            .ToList();
    }

    private async Task<(Customer? customer, string? error)> EnsureCustomerContextAsync()
    {
        if (CurrentCustomer != null)
        {
            return (CurrentCustomer, null);
        }

        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return (null, "Unable to resolve current user.");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return (null, "Your account does not have an email address. Please contact support.");
        }

        var customer = await _customerService.GetCustomerByEmailAsync(user.Email);
        if (customer == null)
        {
            return (null, "No customer profile is linked to your account. Please contact support.");
        }

        CurrentCustomer = customer;
        return (customer, null);
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userId = GetUserId();
        if (userId == 0)
        {
            return null;
        }

        return await _authService.GetUserByIdAsync(userId);
    }

    private static DateTime CombinePreferredDateTime(InputModel input)
    {
        var date = input.PreferredDate!.Value.Date;
        var time = input.PreferredTime!.Value;
        var dateTime = date.Add(time);
        return DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
    }

    private int GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var userId) ? userId : 0;
    }
}
