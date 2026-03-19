using AuthService.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace AuthService.Tests.Services;

public class TenantResolverTests
{
    private static readonly Guid ConfigTenantId = new("aaaaaaaa-0000-0000-0000-000000000001");

    private static TenantResolver CreateResolver(string? configuredTenantId = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DefaultTenant:TenantId"] = configuredTenantId
            })
            .Build();
        return new TenantResolver(config);
    }

    private static HttpContext ContextWithHeader(string? headerValue)
    {
        var ctx = new DefaultHttpContext();
        if (headerValue != null)
            ctx.Request.Headers["X-Tenant-Id"] = headerValue;
        return ctx;
    }

    [Fact]
    public void Resolve_WithValidTenantIdHeader_ReturnsTenantFromHeader()
    {
        var resolver = CreateResolver(ConfigTenantId.ToString());
        var ctx = ContextWithHeader(ConfigTenantId.ToString());

        var result = resolver.Resolve(ctx);

        result.Should().Be(ConfigTenantId);
    }

    [Fact]
    public void Resolve_WithNullContext_ReturnsTenantFromConfig()
    {
        var resolver = CreateResolver(ConfigTenantId.ToString());

        var result = resolver.Resolve(null);

        result.Should().Be(ConfigTenantId);
    }

    [Fact]
    public void Resolve_WithContextButNoXTenantIdHeader_ReturnsTenantFromConfig()
    {
        var resolver = CreateResolver(ConfigTenantId.ToString());
        var ctx = new DefaultHttpContext(); // no header

        var result = resolver.Resolve(ctx);

        result.Should().Be(ConfigTenantId);
    }

    [Fact]
    public void Resolve_WithContextAndNonGuidHeader_ReturnsTenantFromConfig()
    {
        var resolver = CreateResolver(ConfigTenantId.ToString());
        var ctx = ContextWithHeader("not-a-guid");

        var result = resolver.Resolve(ctx);

        result.Should().Be(ConfigTenantId);
    }

    [Fact]
    public void Resolve_WhenDefaultTenantNotConfigured_ReturnsEmptyGuid()
    {
        var resolver = CreateResolver(configuredTenantId: null);
        var ctx = new DefaultHttpContext(); // no header

        var result = resolver.Resolve(ctx);

        result.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Resolve_WhenDefaultTenantIsInvalidGuid_ReturnsEmptyGuid()
    {
        var resolver = CreateResolver("not-a-guid");
        var ctx = new DefaultHttpContext();

        var result = resolver.Resolve(ctx);

        result.Should().Be(Guid.Empty);
    }
}
