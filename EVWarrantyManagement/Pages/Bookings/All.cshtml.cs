using System.ComponentModel.DataAnnotations;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Constants;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Bookings;

[Authorize(Policy = "RequireEVM")]
public class AllModel : PageModel
{
    private readonly IServiceBookingService _serviceBookingService;
    private readonly IServiceCenterService _serviceCenterService;

    public AllModel(IServiceBookingService serviceBookingService, IServiceCenterService serviceCenterService)
    {
        _serviceBookingService = serviceBookingService;
        _serviceCenterService = serviceCenterService;
    }

    public IReadOnlyList<ServiceBooking> Bookings { get; private set; } = Array.Empty<ServiceBooking>();

    public IReadOnlyList<ServiceCenter> ServiceCenters { get; private set; } = Array.Empty<ServiceCenter>();

    [BindProperty(SupportsGet = true)]
    [Display(Name = "Status")]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    [Display(Name = "Service Center")]
    public int? ServiceCenterId { get; set; }

    public IReadOnlyList<string> StatusOptions { get; } = new[]
    {
        ServiceBookingStatuses.Pending,
        ServiceBookingStatuses.Approved,
        ServiceBookingStatuses.InProgress,
        ServiceBookingStatuses.Completed,
        ServiceBookingStatuses.Rejected,
        ServiceBookingStatuses.Cancelled
    };

    public async Task OnGetAsync()
    {
        ServiceCenters = await _serviceCenterService.GetAllServiceCentersAsync(HttpContext.RequestAborted);

        var bookings = await _serviceBookingService.GetAllBookingsAsync(HttpContext.RequestAborted);

        if (!string.IsNullOrWhiteSpace(Status))
        {
            bookings = bookings
                .Where(b => string.Equals(b.Status, Status, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (ServiceCenterId.HasValue && ServiceCenterId.Value > 0)
        {
            bookings = bookings
                .Where(b => b.ServiceCenterId == ServiceCenterId.Value)
                .ToList();
        }

        Bookings = bookings
            .OrderByDescending(b => b.PreferredStart)
            .ToList();
    }
}

