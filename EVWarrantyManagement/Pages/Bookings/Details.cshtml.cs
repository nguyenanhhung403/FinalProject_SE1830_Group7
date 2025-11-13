using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Constants;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Bookings;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IServiceBookingService _serviceBookingService;
    private readonly IAuthService _authService;
    private readonly ICustomerService _customerService;
    private readonly IInvoicePdfBuilder _invoicePdfBuilder;

    public DetailsModel(
        IServiceBookingService serviceBookingService,
        IAuthService authService,
        ICustomerService customerService,
        IInvoicePdfBuilder invoicePdfBuilder)
    {
        _serviceBookingService = serviceBookingService;
        _authService = authService;
        _customerService = customerService;
        _invoicePdfBuilder = invoicePdfBuilder;
    }

    public ServiceBooking? Booking { get; private set; }

    public IReadOnlyList<ServiceBookingStatusLog> StatusLogs { get; private set; } = Array.Empty<ServiceBookingStatusLog>();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Booking = await _serviceBookingService.GetBookingAsync(id, HttpContext.RequestAborted);
        if (Booking == null)
        {
            return NotFound();
        }

        var accessResult = await EnsureAccessAsync(Booking);
        if (!accessResult)
        {
            return Forbid();
        }

        StatusLogs = await _serviceBookingService.GetStatusLogsAsync(id, HttpContext.RequestAborted);

        return Page();
    }

    public async Task<IActionResult> OnGetInvoiceAsync(int id)
    {
        var booking = await _serviceBookingService.GetBookingAsync(id, HttpContext.RequestAborted);
        if (booking == null)
        {
            return NotFound();
        }

        var hasAccess = await EnsureAccessAsync(booking);
        if (!hasAccess)
        {
            return Forbid();
        }

        if (!string.Equals(booking.Status, ServiceBookingStatuses.Completed, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Invoice is only available once the booking is completed.";
            return RedirectToPage(new { id });
        }

        var parts = booking.ServiceBookingParts?.ToList() ?? new List<ServiceBookingPart>();
        var pdfBytes = _invoicePdfBuilder.Build(booking, parts);
        var fileName = $"Booking_{booking.ServiceBookingId}_Invoice.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    private async Task<bool> EnsureAccessAsync(ServiceBooking booking)
    {
        if (!User.IsInRole("Customer"))
        {
            return true;
        }

        var userId = GetUserId();
        if (userId == 0)
        {
            return false;
        }

        var user = await _authService.GetUserByIdAsync(userId);
        if (user?.Email == null)
        {
            return false;
        }

        var customer = await _customerService.GetCustomerByEmailAsync(user.Email);
        if (customer == null || customer.CustomerId != booking.CustomerId)
        {
            return false;
        }

        return true;
    }

    private int GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var userId) ? userId : 0;
    }
}

