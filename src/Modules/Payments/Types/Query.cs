namespace Cinema.Payments.Types;

[QueryType]
public static partial class PaymentsQueries
{
    public static PaymentsStatus GetPaymentsStatus() => new("payments", DateTimeOffset.UtcNow);
}
