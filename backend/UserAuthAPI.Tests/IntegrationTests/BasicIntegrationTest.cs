using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using UserAuthAPI.Infrastructure.Data;

namespace UserAuthAPI.Tests.IntegrationTests;

public class BasicIntegrationTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BasicIntegrationTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task BasicTest_ShouldPass()
    {
        // Just verify we can create a client
        var client = _factory.CreateClient();
        Assert.NotNull(client);
    }
}