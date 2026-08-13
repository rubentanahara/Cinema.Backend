namespace CleanTemplate.Application.Features.Test;

public interface ITestItemsRepository
{
    Task<string> GetFirstMessageAsync(CancellationToken cancellationToken);
}