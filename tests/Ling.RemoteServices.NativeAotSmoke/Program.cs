using Ling.RemoteServices.AspNetCore;
using Ling.RemoteServices.NativeAotSmoke;
using Ling.RemoteServices.NativeAotSmoke.Generated;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(
        0,
        AotJsonSerializerContext.Default);
});
builder.Services.AddSingleton(new HttpClient
{
    BaseAddress = new Uri("http://127.0.0.1:5199")
});
builder.Services.AddRemoteServiceClients(options =>
{
    options.AddJsonSerializerContext(AotJsonSerializerContext.Default);
});
builder.Services.AddRemoteServices();
builder.Services.AddSingleton<IAotEchoService, AotEchoService>();

var app = builder.Build();

app.MapRemoteServices();
app.MapGet(
    "/client-smoke",
    async (
        [Microsoft.AspNetCore.Mvc.FromServices] IEnumerable<IAotEchoService> services,
        CancellationToken cancellationToken) =>
    {
        var client = services.First();
        return await client.EchoAsync(
            84,
            "generated-client",
            Guid.Parse("7d7ce27f-1e70-46dd-a137-6a6667f22f65"),
            new AotEchoRequest { Value = "client-round-trip" },
            cancellationToken);
    });
app.Run();
