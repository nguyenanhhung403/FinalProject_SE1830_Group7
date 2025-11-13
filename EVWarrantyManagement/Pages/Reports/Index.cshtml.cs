using System.Text;
using EVWarrantyManagement.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Reports;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IReportingService _reportingService;

    public IndexModel(IReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    public int Year { get; private set; } = DateTime.UtcNow.Year;
    public int TotalClaims => ByStatus.Values.Sum();
    public decimal TotalRevenue { get; private set; }
    public decimal AverageClaimValue => TotalClaims > 0 ? TotalRevenue / TotalClaims : 0;
    public IReadOnlyDictionary<string, int> ByStatus { get; private set; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> ByModel { get; private set; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> ByMonth { get; private set; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, decimal> RevenueByMonth { get; private set; } = new Dictionary<string, decimal>();
    public IReadOnlyDictionary<string, decimal> RevenueByServiceCenter { get; private set; } = new Dictionary<string, decimal>();

    public int GetStatus(string key) => ByStatus.TryGetValue(key, out var v) ? v : 0;

    public async Task OnGetAsync(int? year)
    {
        Year = year ?? DateTime.UtcNow.Year;
        ByStatus = await _reportingService.GetClaimCountsByStatusAsync();
        ByModel = await _reportingService.GetClaimCountsByModelAsync();
        ByMonth = await _reportingService.GetClaimCountsByMonthAsync(Year);
        TotalRevenue = await _reportingService.GetTotalRevenueAsync();
        RevenueByMonth = await _reportingService.GetRevenueByMonthAsync(Year);
        RevenueByServiceCenter = await _reportingService.GetRevenueByServiceCenterAsync(Year);
    }

    public async Task<FileResult> OnPostExportCsvAsync()
    {
        ByStatus = await _reportingService.GetClaimCountsByStatusAsync();
        var sb = new StringBuilder();
        sb.AppendLine("Metric,Value");
        foreach (var kv in ByStatus)
        {
            sb.AppendLine($"{kv.Key},{kv.Value}");
        }
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"claims_by_status_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    public async Task<FileResult> OnPostExportPdfAsync()
    {
        // Simple placeholder PDF (actually a text file with PDF mime) – replace with real generator later
        var content = "EV Warranty Report - export placeholder";
        var bytes = Encoding.UTF8.GetBytes(content);
        return File(bytes, "application/pdf", $"report_{DateTime.UtcNow:yyyyMMdd}.pdf");
    }
}

