using System.ComponentModel.DataAnnotations;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Customers;

[Authorize(Policy = "RequireAdmin")]
public class CreateModel : PageModel
{
    private readonly ICustomerService _customerService;

    public CreateModel(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [BindProperty]
    public CustomerInputModel Input { get; set; } = new();

    public class CustomerInputModel
    {
        [Required]
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var customer = new Customer
        {
            FullName = Input.FullName,
            Email = Input.Email,
            Phone = Input.Phone,
            Address = Input.Address
        };
        await _customerService.CreateCustomerAsync(customer);
        TempData["Success"] = "Customer created.";
        return RedirectToPage("Index");
    }
}

