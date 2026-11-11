using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesContacts.Models;
using RazorPagesContacts.Stores;

namespace RazorPagesContacts.Pages.Contacts;

public class ContactEditPageModel : PageModel
{
    private readonly ContactStore store;

    public ContactEditPageModel(ContactStore store)
    {
        this.store = store;
    }

    [BindProperty]
    public ContactModel Contact { get; set; } = new();

    // GET /Contacts/Edit/1 - "id" comes from the "{id:int}" route in Edit.cshtml.
    public IActionResult OnGet(int id)
    {
        ContactModel? contact = store.FindById(id);
        if (contact == null)
        {
            return NotFound();
        }
        Contact = contact;
        return Page();
    }

    // POST /Contacts/Edit/1 - the main form.
    public IActionResult OnPost(int id)
    {
        Contact.Id = id; // the id comes from the URL, not from a form field the user could change
        if (!ModelState.IsValid)
        {
            return Page();
        }
        if (!store.Update(Contact))
        {
            return NotFound();
        }
        TempData["Message"] = $"Saved {Contact.Name}.";
        return RedirectToPage("./Index");
    }

    // POST /Contacts/Edit/1?handler=Delete - the delete form (a NAMED handler).
    public IActionResult OnPostDelete(int id)
    {
        if (!store.Delete(id))
        {
            return NotFound();
        }
        TempData["Message"] = "Contact deleted.";
        return RedirectToPage("./Index");
    }
}
