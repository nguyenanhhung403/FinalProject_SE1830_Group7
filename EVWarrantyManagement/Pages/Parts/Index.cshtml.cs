using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Parts;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IPartService _partService;

    public IndexModel(IPartService partService)
    {
        _partService = partService;
    }

    public IReadOnlyList<Part> Parts { get; private set; } = Array.Empty<Part>();

    public async Task OnGetAsync()
    {
        Parts = await _partService.GetPartsAsync();
    }
}

