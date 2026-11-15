using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesContacts.Models;
using RazorPagesContacts.Stores;

namespace RazorPagesContacts.Pages.Variations;

// A handler PARAMETER: OnGet(string? term) gets the query value directly. Works on any verb, but the value is
// only a local variable - copy it to a property yourself if the page needs to show it.
public class SearchWithHandlerParameterPageModel : PageModel
{
    private readonly ContactStore store;

    public SearchWithHandlerParameterPageModel(ContactStore store)
    {
        this.store = store;
    }

    public string? Term { get; private set; }

    public List<ContactModel> Results { get; private set; } = new();

    public void OnGet(string? term)
    {
        Results = store.GetAll().Where(contact => string.IsNullOrEmpty(term) || contact.Name.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        // Term is NOT set: the page shows term: "" even though the search used it.
    }
}
