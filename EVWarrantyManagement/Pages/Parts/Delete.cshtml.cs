using EVWarrantyManagement.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVWarrantyManagement.Pages.Parts;

[Authorize(Policy = "RequireAdmin")]
public class DeleteModel : PageModel
{
    private readonly IPartService _partService;

    public DeleteModel(IPartService partService)
    {
        _partService = partService;
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
        await _partService.DeletePartAsync(Id);
        TempData["Success"] = "Part deleted.";
        return RedirectToPage("Index");
    }
}

