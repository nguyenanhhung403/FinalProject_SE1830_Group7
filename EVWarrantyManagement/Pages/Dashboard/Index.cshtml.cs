using System.Linq;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.DAL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Dashboard;

[Authorize(Policy = "RequireEVM")]
public class IndexModel : PageModel
{
    private readonly IWarrantyClaimService _claimService;
    private readonly IServiceBookingService _serviceBookingService;

    public IndexModel(IWarrantyClaimService claimService, IServiceBookingService serviceBookingService)
    {
        _claimService = claimService;
        _serviceBookingService = serviceBookingService;
    }

    public IReadOnlyList<WarrantyClaim> AllClaims { get; private set; } = Array.Empty<WarrantyClaim>();

    public BookingStatisticsResult? BookingStats { get; private set; }

    public IReadOnlyList<ServiceBookingSummaryRow> RecentBookings { get; private set; } = Array.Empty<ServiceBookingSummaryRow>();

    public int SelectedYear { get; private set; }

    public async Task OnGetAsync(int? year)
    {
        SelectedYear = year ?? DateTime.UtcNow.Year;

        AllClaims = (await _claimService.GetAllClaimsAsync())
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        BookingStats = await _serviceBookingService.GetStatisticsAsync(SelectedYear, HttpContext.RequestAborted);
        RecentBookings = await _serviceBookingService.GetRecentBookingsSummaryAsync(5, HttpContext.RequestAborted);
    }
}

