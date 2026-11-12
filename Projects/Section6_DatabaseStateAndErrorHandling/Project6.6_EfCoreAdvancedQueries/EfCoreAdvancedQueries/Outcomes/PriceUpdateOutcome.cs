namespace EfCoreAdvancedQueries.Outcomes;

public enum PriceUpdateOutcome
{
    Updated,
    NotFound,
    ConflictSomeoneElseChangedIt,
}
