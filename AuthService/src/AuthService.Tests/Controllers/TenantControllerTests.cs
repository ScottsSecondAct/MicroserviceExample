using AuthService.Controllers;
using AuthService.Models.DTOs;
using AuthService.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SharedLibrary.DTOs;

namespace AuthService.Tests.Controllers;

public class TenantControllerTests
{
    private const string ValidSecret = "super-secret-bootstrap";

    private static (TenantController controller, Mock<ITenantProvisioningService> service) Create(
        string? configuredSecret = ValidSecret)
    {
        var serviceMock = new Mock<ITenantProvisioningService>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TenantProvisioning:BootstrapSecret"] = configuredSecret
            })
            .Build();
        var logger = new Mock<ILogger<TenantController>>().Object;
        var controller = new TenantController(serviceMock.Object, config, logger);
        return (controller, serviceMock);
    }

    private static ProvisionTenantRequest ValidRequest() => new()
    {
        Slug = "acme",
        DisplayName = "Acme Corp",
        AdminEmail = "admin@acme.com",
        AdminPassword = "SecurePass1!"
    };

    [Fact]
    public async Task Provision_WithMissingBootstrapSecret_Returns401()
    {
        var (controller, _) = Create();

        var result = await controller.Provision(ValidRequest(), bootstrapSecret: null);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Provision_WithWrongBootstrapSecret_Returns401()
    {
        var (controller, _) = Create();

        var result = await controller.Provision(ValidRequest(), bootstrapSecret: "wrong-secret");

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Provision_WhenSecretNotConfigured_Returns401()
    {
        var (controller, _) = Create(configuredSecret: null);

        var result = await controller.Provision(ValidRequest(), bootstrapSecret: ValidSecret);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Provision_WithValidSecret_AndServiceSuccess_Returns201()
    {
        var (controller, service) = Create();
        service.Setup(s => s.ProvisionAsync(It.IsAny<ProvisionTenantRequest>()))
            .ReturnsAsync(ServiceResult.Success(new TenantDto { Slug = "acme" }, "Created", 201));

        var result = await controller.Provision(ValidRequest(), bootstrapSecret: ValidSecret);

        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Provision_WithValidSecret_AndServiceFailure_ReturnsErrorCode()
    {
        var (controller, service) = Create();
        service.Setup(s => s.ProvisionAsync(It.IsAny<ProvisionTenantRequest>()))
            .ReturnsAsync(ServiceResult.Failure("Slug already exists.", 409));

        var result = await controller.Provision(ValidRequest(), bootstrapSecret: ValidSecret);

        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(409);
    }
}
