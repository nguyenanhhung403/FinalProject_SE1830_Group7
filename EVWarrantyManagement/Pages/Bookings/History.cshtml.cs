using System.Security.Claims;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Bookings;

[Authorize(Roles = "Customer")]
public class HistoryModel : PageModel
{
    private readonly IServiceBookingService _serviceBookingService;
    private readonly ICustomerService _customerService;
    private readonly IAuthService _authService;

    public HistoryModel(
        IServiceBookingService serviceBookingService,
        ICustomerService customerService,
        IAuthService authService)
    {
        _serviceBookingService = serviceBookingService;
        _customerService = customerService;
        _authService = authService;
    }

    public IReadOnlyList<ServiceBooking> Bookings { get; private set; } = Array.Empty<ServiceBooking>();

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetUserId();
        if (userId == 0)
        {
            ErrorMessage = "Unable to resolve your account.";
            return Page();
        }

        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
        {
            ErrorMessage = "Your user profile is missing contact details. Please contact support.";
            return Page();
        }

        var customer = await _customerService.GetCustomerByEmailAsync(user.Email);
        if (customer == null)
        {
            ErrorMessage = "No customer record is linked to your account.";
            return Page();
        }

        Bookings = await _serviceBookingService.GetCompletedBookingsForCustomerAsync(customer.CustomerId, HttpContext.RequestAborted);
        return Page();
    }

    private int GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var userId) ? userId : 0;
    }
}
