namespace CashFlow.Ledger.Application;

public sealed record EntryPeriod
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    private EntryPeriod(DateOnly from, DateOnly to, int offset, int limit)
    {
        From = from;
        To = to;
        Offset = offset;
        Limit = limit;
    }

    public DateOnly From { get; }

    public DateOnly To { get; }

    public int Offset { get; }

    public int Limit { get; }

    public static EntryPeriod Of(DateOnly from, DateOnly to, int? offset = null, int? limit = null)
    {
        if (from > to)
        {
            throw new RequestValidationException(
                $"The start date {from:yyyy-MM-dd} must not be later than the end date {to:yyyy-MM-dd}.");
        }

        var resolvedOffset = offset ?? 0;
        if (resolvedOffset < 0)
        {
            throw new RequestValidationException("The offset must not be negative.");
        }

        var resolvedLimit = limit ?? DefaultPageSize;
        if (resolvedLimit is < 1 or > MaxPageSize)
        {
            throw new RequestValidationException($"The limit must be between 1 and {MaxPageSize}.");
        }

        return new EntryPeriod(from, to, resolvedOffset, resolvedLimit);
    }
}
