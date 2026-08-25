namespace Cinema.Payments.Types;

[QueryType]
public static partial class ServiceQueries
{
    public static PaymentsStatus GetPaymentsStatus() => new("payments", DateTimeOffset.UtcNow);
}
