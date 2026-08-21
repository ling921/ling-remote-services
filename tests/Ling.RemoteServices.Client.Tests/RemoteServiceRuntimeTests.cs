using Ling.RemoteServices.Client;

namespace Ling.RemoteServices.Client.Tests;

public class RemoteServiceRuntimeTests
{
    [Fact]
    public void GetJsonTypeInfo_uses_registered_source_generated_context()
    {
        var options = new RemoteServiceClientOptions()
            .AddJsonSerializerContext(ClientTestJsonSerializerContext.Default);

        var typeInfo = RemoteServiceClientRuntime.GetJsonTypeInfo<ClientTestPayload>(
            options.JsonSerializerOptions);

        Assert.Equal(typeof(ClientTestPayload), typeInfo.Type);
        Assert.Same(
            ClientTestJsonSerializerContext.Default,
            options.JsonSerializerOptions.TypeInfoResolverChain[0]);
    }

    [Fact]
    public void WithHttpMethod_returns_local_service_unchanged()
    {
        var service = new LocalService();

        var selected = service.WithHttpMethod(RemoteHttpMethod.Post);

        Assert.Same(service, selected);
    }

    [Fact]
    public void BuildPath_Encodes_Ordinary_And_Preserves_DoubleCatchAll_Slashes()
    {
        var values = new Dictionary<string, object?>
        {
            ["id"] = "a/b",
            ["path"] = "one/two three"
        };

        Assert.Equal(
            "/api/a%2Fb/one/two%20three",
            RemoteServiceClientRuntime.BuildPath("/api/{id}/{**path}", values));
    }

    [Fact]
    public void AddQuery_Repeats_Collections_And_Omits_Null()
    {
        var parts = new List<string>();

        RemoteServiceClientRuntime.AddQuery(parts, "tag", new[] { "a b", "c" });
        RemoteServiceClientRuntime.AddQuery(parts, "missing", null);

        Assert.Equal(new[] { "tag=a%20b", "tag=c" }, parts);
    }

    [Fact]
    public void BuildPath_Removes_Optional_Parameter_Separator()
    {
        var values = new Dictionary<string, object?>
        {
            ["name"] = "readme",
            ["extension"] = null
        };

        Assert.Equal(
            "/files/readme",
            RemoteServiceClientRuntime.BuildPath("/files/{name}.{extension?}", values));
    }

    private sealed class LocalService
    {
    }
}
