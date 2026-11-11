using Ling.RemoteServices.Examples.Client.Generated;
using Ling.RemoteServices.Examples.Shared;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddRemoteServiceClients(options =>
{
    options.AddJsonSerializerContext(RemoteServiceJsonSerializerContext.Default);
});

await builder.Build().RunAsync();
