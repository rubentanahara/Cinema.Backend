namespace CleanTemplate.Api.IntegrationTests;

[Collection(WebAppFactoryCollection.CollectionName)]
public class TestEndpointTests(WebAppFactory factory)
{
    [Fact]
    public async Task Get_Test_ReturnsTestString()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/test");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("test");
    }
}