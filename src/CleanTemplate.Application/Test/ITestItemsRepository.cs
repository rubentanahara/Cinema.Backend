namespace CleanTemplate.Application.Test;

public interface ITestItemsRepository
{
    Task<string> GetFirstMessageAsync(CancellationToken cancellationToken);
}