using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EVWarrantyManagement.BLL.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Account;

public class LoginModel : PageModel
{
    private readonly IAuthService _authService;

    public LoginModel(IAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public LoginInputModel Input { get; set; } = new();

    public class LoginInputModel
    {
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _authService.SignInAsync(Input.Username, Input.Password);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return Page();
        }

        var roleName = user.Role?.RoleName ?? string.Empty;
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, roleName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        // Redirect by role
        if (string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(roleName, "EVM Staff", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(roleName, "EVM", StringComparison.OrdinalIgnoreCase))
            return RedirectToPage("/Dashboard/Index");
        if (string.Equals(roleName, "SC Staff", StringComparison.OrdinalIgnoreCase) || string.Equals(roleName, "SC", StringComparison.OrdinalIgnoreCase))
            return RedirectToPage("/Claims/Index");
        if (string.Equals(roleName, "SC Technician", StringComparison.OrdinalIgnoreCase))
            return RedirectToPage("/Claims/Index");

        return RedirectToPage("/Claims/Index");
    }
}

