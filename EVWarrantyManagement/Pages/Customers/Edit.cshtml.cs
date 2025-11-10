using System.ComponentModel.DataAnnotations;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Customers;

[Authorize]
public class EditModel : PageModel
{
    private readonly ICustomerService _customerService;

    public EditModel(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [BindProperty]
    public int Id { get; set; }

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

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var customer = await _customerService.GetCustomerAsync(id);
        if (customer == null) return RedirectToPage("Index");

        Id = id;
        Input = new CustomerInputModel
        {
            FullName = customer.FullName ?? string.Empty,
            Email = customer.Email,
            Phone = customer.Phone,
            Address = customer.Address
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var customer = await _customerService.GetCustomerAsync(Id);
        if (customer == null) return RedirectToPage("Index");

        customer.FullName = Input.FullName;
        customer.Email = Input.Email;
        customer.Phone = Input.Phone;
        customer.Address = Input.Address;

        await _customerService.UpdateCustomerAsync(customer);
        TempData["Success"] = "Customer updated.";
        return RedirectToPage("Index");
    }
}

