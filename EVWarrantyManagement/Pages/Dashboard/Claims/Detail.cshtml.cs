using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Dashboard.Claims;

[Authorize(Policy = "RequireEVM")]
public class DetailModel : PageModel
{
    public IActionResult OnGet(int id)
    {
        // Redirect to the main Claims/Details page which has all functionality
        return RedirectToPage("/Claims/Details", new { id });
    }
}

