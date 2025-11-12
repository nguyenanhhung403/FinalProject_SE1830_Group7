using EVWarrantyManagement.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Customers;

[Authorize(Policy = "RequireAdmin")]
public class DeleteModel : PageModel
{
    private readonly ICustomerService _customerService;

    public DeleteModel(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [BindProperty]
    public int Id { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Id = id;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _customerService.DeleteCustomerAsync(Id);
        TempData["Success"] = "Customer deleted.";
        return RedirectToPage("Index");
    }
}

