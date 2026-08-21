using Ling.RemoteServices.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Ling.RemoteServices.AspNetCore.Tests;

public class EndpointConventionTests
{
    [Fact]
    public void Registry_exposes_service_and_operation_convention_builders()
    {
        var applicationBuilder = WebApplication.CreateBuilder();
        var application = applicationBuilder.Build();
        var group = application.MapGroup("/api/test");
        var getOperation = group.MapGet("/get", () => "ok");
        var postOperation = group.MapPost("/get", () => "ok");
        var method = new RemoteServiceMethodConventionBuilder(
            new Dictionary<RemoteHttpMethod, IEndpointConventionBuilder>
            {
                [RemoteHttpMethod.Get] = getOperation,
                [RemoteHttpMethod.Post] = postOperation
            });
        var service = new RemoteServiceEndpointConventionBuilder<ITestService>(
            group,
            new Dictionary<string, RemoteServiceMethodConventionBuilder>(StringComparer.Ordinal)
            {
                [nameof(ITestService.GetAsync)] = method
            });
        var registry = new RemoteServiceEndpointConventionRegistry();
        registry.AddService(service);

        Assert.Same(service, registry.For<ITestService>());
        Assert.Same(method, service.Operation(nameof(ITestService.GetAsync)));
        Assert.Same(
            postOperation,
            service.Operation(nameof(ITestService.GetAsync), RemoteHttpMethod.Post));

        registry.RequireAuthorization();
        service.RequireCors("Api");
        service.Operation(nameof(ITestService.GetAsync)).CacheOutput();

        var globalConventionApplied = false;
        var serviceConventionApplied = false;
        var operationConventionApplied = false;
        registry.Add(_ => globalConventionApplied = true);
        service.Add(_ => serviceConventionApplied = true);
        getOperation.Add(_ => operationConventionApplied = true);

        _ = ((IEndpointRouteBuilder)application).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .ToArray();

        Assert.True(globalConventionApplied);
        Assert.True(serviceConventionApplied);
        Assert.True(operationConventionApplied);
    }

    [Fact]
    public void Operation_reports_available_methods_when_lookup_fails()
    {
        var applicationBuilder = WebApplication.CreateBuilder();
        var application = applicationBuilder.Build();
        var group = application.MapGroup("/api/test");
        var operation = group.MapGet("/get", () => "ok");
        var method = new RemoteServiceMethodConventionBuilder(
            new Dictionary<RemoteHttpMethod, IEndpointConventionBuilder>
            {
                [RemoteHttpMethod.Get] = operation
            });
        var service = new RemoteServiceEndpointConventionBuilder<ITestService>(
            group,
            new Dictionary<string, RemoteServiceMethodConventionBuilder>(StringComparer.Ordinal)
            {
                [nameof(ITestService.GetAsync)] = method
            });

        var exception = Assert.Throws<KeyNotFoundException>(() => service.Operation("Missing"));

        Assert.Contains(nameof(ITestService.GetAsync), exception.Message);
    }

    [Fact]
    public void Policy_runtime_applies_registered_custom_policy()
    {
        Type? appliedServiceType = null;
        string? appliedMethodName = null;
        var applicationBuilder = WebApplication.CreateBuilder();
        applicationBuilder.Services.AddRemoteServices(options =>
        {
            options.AddEndpointPolicy("Custom", context =>
            {
                appliedServiceType = context.ServiceType;
                appliedMethodName = context.MethodName;
                context.WithMetadata("custom-metadata");
            });
        });
        var application = applicationBuilder.Build();
        var endpoint = application.MapGet("/api/test", () => "ok");

        RemoteServiceEndpointPolicyRuntime.Apply(
            application.Services,
            endpoint,
            typeof(ITestService),
            nameof(ITestService.GetAsync),
            new RemoteServiceEndpointPolicyMetadata
            {
                AuthorizationPolicyNames = new string?[] { null, "Named" },
                AllowAnonymous = true,
                CorsPolicyName = "Api",
                OutputCacheEnabled = true,
                OutputCachePolicyName = "Cache",
                RateLimitPolicyName = "Reads",
                RequestTimeoutPolicyName = "Fast",
                CustomPolicyNames = new[] { "Custom" }
            });

        Assert.Equal(typeof(ITestService), appliedServiceType);
        Assert.Equal(nameof(ITestService.GetAsync), appliedMethodName);
    }

    [Fact]
    public void Policy_runtime_rejects_unregistered_custom_policy()
    {
        var applicationBuilder = WebApplication.CreateBuilder();
        applicationBuilder.Services.AddRemoteServices();
        var application = applicationBuilder.Build();
        var endpoint = application.MapGet("/api/test", () => "ok");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            RemoteServiceEndpointPolicyRuntime.Apply(
                application.Services,
                endpoint,
                typeof(ITestService),
                nameof(ITestService.GetAsync),
                new RemoteServiceEndpointPolicyMetadata
                {
                    CustomPolicyNames = new[] { "Missing" }
                }));

        Assert.Contains("Missing", exception.Message);
    }

    private interface ITestService
    {
        Task<string> GetAsync();
    }
}
