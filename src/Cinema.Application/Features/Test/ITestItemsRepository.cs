namespace Cinema.Application.Features.Test;

public interface ITestItemsRepository
{
    Task<string> GetFirstMessageAsync(CancellationToken cancellationToken);
}