using System.ComponentModel.DataAnnotations;

namespace RazorPagesContacts.Models;

public class ContactModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "Name must be 2 to 60 characters.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "That isn't a valid email address.")]
    public string Email { get; set; } = "";

    [Phone(ErrorMessage = "That isn't a valid phone number.")]
    public string? Phone { get; set; }
}
