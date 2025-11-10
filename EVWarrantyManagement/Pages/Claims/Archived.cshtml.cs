using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Claims;

[Authorize]
public class ArchivedModel : PageModel
{
    private readonly IWarrantyClaimService _claimService;

    public ArchivedModel(IWarrantyClaimService claimService)
    {
        _claimService = claimService;
    }

    public IReadOnlyList<WarrantyHistory> Histories { get; private set; } = Array.Empty<WarrantyHistory>();

    public string PrimaryRole { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("EVM Staff") && !User.IsInRole("EVM") &&
            !User.IsInRole("SC Technician") && !User.IsInRole("SC") && !User.IsInRole("SC Staff"))
        {
            return Forbid();
        }

        PrimaryRole = GetPrimaryRole();
        Histories = await _claimService.GetArchivedClaimsAsync(GetUserId(), PrimaryRole);
        return Page();
    }

    private int GetUserId()
    {
        var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(id, out var userId) ? userId : 0;
    }

    private string GetPrimaryRole()
    {
        if (User.IsInRole("Admin")) return "Admin";
        if (User.IsInRole("EVM Staff")) return "EVM Staff";
        if (User.IsInRole("EVM")) return "EVM";
        if (User.IsInRole("SC Technician")) return "SC Technician";
        if (User.IsInRole("SC Staff")) return "SC Staff";
        if (User.IsInRole("SC")) return "SC";
        return string.Empty;
    }
}

