using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SidebarAndDateBinder.Models;

// Reads a date from the query string ONLY in ISO format: yyyy-MM-dd (e.g. ?day=2026-11-05).
// Use it on a parameter with: [ModelBinder(typeof(CustomIsoDateFormatModelBinder))]
public class CustomIsoDateFormatModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        string? text = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;

        if (string.IsNullOrWhiteSpace(text))
        {
            // Nothing sent: leave it empty so the action can pick a default.
            return Task.CompletedTask;
        }

        if (DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
        {
            bindingContext.Result = ModelBindingResult.Success(date);
        }
        else
        {
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"'{text}' is not a date in yyyy-MM-dd format.");
        }
        return Task.CompletedTask;
    }
}
