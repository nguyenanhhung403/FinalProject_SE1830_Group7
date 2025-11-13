using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.ServiceCenters;

[Authorize(Policy = "RequireEVM")]
public class IndexModel : PageModel
{
    private readonly IServiceCenterService _serviceCenterService;

    public IndexModel(IServiceCenterService serviceCenterService)
    {
        _serviceCenterService = serviceCenterService;
    }

    public IReadOnlyList<ServiceCenter> ServiceCenters { get; private set; } = Array.Empty<ServiceCenter>();

    public async Task OnGetAsync()
    {
        ServiceCenters = await _serviceCenterService.GetAllServiceCentersAsync();
    }
}

