using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesContacts.Models;
using RazorPagesContacts.Stores;

namespace RazorPagesContacts.Pages.Contacts;

public class ContactCreatePageModel : PageModel
{
    private readonly ContactStore store;

    public ContactCreatePageModel(ContactStore store)
    {
        this.store = store;
    }

    // [BindProperty]: on POST, the form fields are bound into this property automatically.
    [BindProperty]
    public ContactModel Contact { get; set; } = new();

    public void OnGet()
    {
    }

    // Runs for POST /Contacts/Create.
    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page(); // show the same page again, with the error messages
        }
        store.Add(Contact);
        TempData["Message"] = $"Added {Contact.Name}.";
        return RedirectToPage("./Index"); // Post/Redirect/Get
    }
}
