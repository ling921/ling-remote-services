using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Ling.RemoteServices.AspNetCore;

namespace Ling.RemoteServices.AspNetCore.Tests;

public class RemoteServiceServerRuntimeTests
{
    [Fact]
    public void GetJsonTypeInfo_uses_reflection_metadata_by_default_on_jit_hosts()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var typeInfo = RemoteServiceServerRuntime.GetJsonTypeInfo<TestPayload>(options);

        Assert.Equal(typeof(TestPayload), typeInfo.Type);
        Assert.IsType<DefaultJsonTypeInfoResolver>(options.TypeInfoResolver);
    }

    private sealed record TestPayload(string Value);
}
