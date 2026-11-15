using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesContacts.Models;
using RazorPagesContacts.Stores;

namespace RazorPagesContacts.Pages.Variations;

// [BindProperty] WITHOUT SupportsGet (like the original Create/Edit pages): properties are only bound on POST.
// On GET, ?term=ann is IGNORED - Term stays null and every contact is listed. An easy mistake on search pages.
public class SearchWithoutSupportsGetPageModel : PageModel
{
    private readonly ContactStore store;

    public SearchWithoutSupportsGetPageModel(ContactStore store)
    {
        this.store = store;
    }

    [BindProperty]
    public string? Term { get; set; }

    public List<ContactModel> Results { get; private set; } = new();

    public void OnGet()
    {
        Results = store.GetAll().Where(contact => string.IsNullOrEmpty(Term) || contact.Name.Contains(Term, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
