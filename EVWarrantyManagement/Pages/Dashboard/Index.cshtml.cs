using System.Linq;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Dashboard;

[Authorize(Policy = "RequireEVM")]
public class IndexModel : PageModel
{
    private readonly IWarrantyClaimService _claimService;

    public IndexModel(IWarrantyClaimService claimService)
    {
        _claimService = claimService;
    }

    public IReadOnlyList<WarrantyClaim> AllClaims { get; private set; } = Array.Empty<WarrantyClaim>();

    public async Task OnGetAsync()
    {
        AllClaims = (await _claimService.GetAllClaimsAsync())
            .OrderByDescending(c => c.CreatedAt)
            .ToList();
    }
}

