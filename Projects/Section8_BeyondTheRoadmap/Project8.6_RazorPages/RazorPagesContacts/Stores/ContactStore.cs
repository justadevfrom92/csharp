using RazorPagesContacts.Models;

namespace RazorPagesContacts.Stores;

// In-memory contacts, one shared list for the app (registered as a singleton).
public class ContactStore
{
    private readonly List<ContactModel> contacts =
    [
        new ContactModel { Id = 1, Name = "Ada Lovelace", Email = "ada@example.com", Phone = "+44 20 7946 0000" },
        new ContactModel { Id = 2, Name = "Alan Turing", Email = "alan@example.com" },
    ];
    private readonly object contactsLock = new();

    public List<ContactModel> GetAll()
    {
        lock (contactsLock)
        {
            return contacts.OrderBy(contact => contact.Name).ToList();
        }
    }

    public ContactModel? FindById(int id)
    {
        lock (contactsLock)
        {
            ContactModel? found = contacts.FirstOrDefault(contact => contact.Id == id);
            // Return a COPY, so a page editing it can't change the stored contact before saving.
            return found == null ? null : new ContactModel { Id = found.Id, Name = found.Name, Email = found.Email, Phone = found.Phone };
        }
    }

    public void Add(ContactModel contact)
    {
        lock (contactsLock)
        {
            contact.Id = contacts.Count == 0 ? 1 : contacts.Max(existing => existing.Id) + 1;
            contacts.Add(contact);
        }
    }

    public bool Update(ContactModel changed)
    {
        lock (contactsLock)
        {
            int index = contacts.FindIndex(contact => contact.Id == changed.Id);
            if (index < 0)
            {
                return false;
            }
            contacts[index] = changed;
            return true;
        }
    }

    public bool Delete(int id)
    {
        lock (contactsLock)
        {
            return contacts.RemoveAll(contact => contact.Id == id) > 0;
        }
    }
}
