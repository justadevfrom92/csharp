using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MvcSidebarViewComponentAndDateBinder.ModelBinding;

namespace MvcSidebarViewComponentAndDateBinder.Models;

public enum ActivityKind
{
    OrderPlaced,
    ProductUpdated,
    UserRegistered,
    InvoicePaid,
    CommentPosted
}

public sealed record RecentActivityEntry(
    Guid ActivityId,
    ActivityKind Kind,
    string ActorDisplayName,
    string ActivityDescription,
    DateTimeOffset OccurredAt);

/// <summary>
/// Complex type bound from the query string: /activity?from=2026-11-01&amp;to=2026-11-05&amp;kind=OrderPlaced.
/// The two date properties opt in to the strict ISO binder explicitly with [ModelBinder].
/// </summary>
public sealed class ActivityReportDateRangeQuery
{
    [ModelBinder(typeof(CustomIsoDateFormatModelBinder), Name = "from")]
    public DateOnly? InclusiveStartDate { get; set; }

    [ModelBinder(typeof(CustomIsoDateFormatModelBinder), Name = "to")]
    public DateOnly? InclusiveEndDate { get; set; }

    [FromQuery(Name = "kind")]
    public ActivityKind? KindFilter { get; set; }
}

public sealed record ActivityReportPageViewModel(
    ActivityReportDateRangeQuery AppliedQuery,
    IReadOnlyList<RecentActivityEntry> MatchingEntries,
    IReadOnlyList<string> QueryValidationErrors);

public sealed record DailyActivitySummaryViewModel(DateOnly SummaryDate, IReadOnlyDictionary<ActivityKind, int> CountsByKind, int TotalCount);

public sealed class RecordActivityFormViewModel
{
    [Required, StringLength(60, MinimumLength = 2)]
    [Display(Name = "Your name")]
    public string ActorDisplayName { get; set; } = string.Empty;

    [Required]
    public ActivityKind? Kind { get; set; }

    [Required, StringLength(140, MinimumLength = 3)]
    [Display(Name = "What happened?")]
    public string ActivityDescription { get; set; } = string.Empty;
}
