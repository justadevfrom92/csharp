using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesContacts.Models;
using RazorPagesContacts.Stores;

namespace RazorPagesContacts.Pages.Contacts;

// The page's code ("PageModel"). OnGet runs for GET /Contacts; the page reads its properties.
public class ContactIndexPageModel : PageModel
{
    private readonly ContactStore store;

    public ContactIndexPageModel(ContactStore store)
    {
        this.store = store;
    }

    public List<ContactModel> Contacts { get; private set; } = new();

    public void OnGet()
    {
        Contacts = store.GetAll();
    }
}
