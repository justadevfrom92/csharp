using System.ComponentModel.DataAnnotations;

namespace BlazorTodo.Models;

public class TodoItemModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "Write what needs doing.")]
    [StringLength(60, ErrorMessage = "Keep it under 60 characters.")]
    public string Title { get; set; } = "";

    public bool IsDone { get; set; }
}
