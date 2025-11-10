using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Customers;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ICustomerService _customerService;

    public IndexModel(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    public IReadOnlyList<Customer> Customers { get; private set; } = Array.Empty<Customer>();

    public async Task OnGetAsync()
    {
        Customers = await _customerService.GetCustomersAsync();
    }
}

