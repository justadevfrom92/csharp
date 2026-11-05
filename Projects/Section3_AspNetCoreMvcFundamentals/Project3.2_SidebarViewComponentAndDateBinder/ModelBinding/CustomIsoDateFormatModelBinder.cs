using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MvcSidebarViewComponentAndDateBinder.ModelBinding;

/// <summary>
/// Binds <see cref="DateOnly"/> and <see cref="Nullable{DateOnly}"/> from route values or the query
/// string, accepting ONLY the ISO-8601 calendar format <c>yyyy-MM-dd</c>.
///
/// The default binder uses the request culture, so "03/04/2026" means 4 March for en-US and
/// 3 April for en-GB. A URL is shared, bookmarked and cached independently of the visitor's
/// culture, so dates in URLs must have one unambiguous, culture-invariant spelling.
/// </summary>
public sealed class CustomIsoDateFormatModelBinder : IModelBinder
{
    public const string RequiredIsoDateFormat = "yyyy-MM-dd";

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        Type underlyingModelType = Nullable.GetUnderlyingType(bindingContext.ModelType) ?? bindingContext.ModelType;
        if (underlyingModelType != typeof(DateOnly))
        {
            throw new InvalidOperationException(
                $"{nameof(CustomIsoDateFormatModelBinder)} can only bind DateOnly, not {bindingContext.ModelType.Name}.");
        }

        ValueProviderResult providedValue = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (providedValue == ValueProviderResult.None)
        {
            // Nothing supplied: leave the model unbound so it keeps its default (null for DateOnly?).
            return Task.CompletedTask;
        }

        // Record the raw attempted value so a re-rendered form can show what the user typed.
        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, providedValue);
        string? rawDateText = providedValue.FirstValue;

        if (string.IsNullOrWhiteSpace(rawDateText))
        {
            if (bindingContext.ModelMetadata.IsReferenceOrNullableType)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }
            return Task.CompletedTask;
        }

        if (DateOnly.TryParseExact(rawDateText.Trim(), RequiredIsoDateFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly parsedDate))
        {
            bindingContext.Result = ModelBindingResult.Success(parsedDate);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(bindingContext.ModelName,
            $"'{rawDateText}' is not a valid date for '{bindingContext.ModelName}'. Use the ISO format {RequiredIsoDateFormat} (e.g. 2026-11-05).");
        bindingContext.Result = ModelBindingResult.Failed();
        return Task.CompletedTask;
    }
}

/// <summary>
/// Registers <see cref="CustomIsoDateFormatModelBinder"/> globally for every DateOnly/DateOnly?
/// action parameter or property, so individual actions need no [ModelBinder] attribute.
/// </summary>
public sealed class CustomIsoDateFormatModelBinderProvider : IModelBinderProvider
{
    private static readonly CustomIsoDateFormatModelBinder SharedStatelessBinderInstance = new();

    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Metadata.UnderlyingOrModelType == typeof(DateOnly) ? SharedStatelessBinderInstance : null;
    }
}
