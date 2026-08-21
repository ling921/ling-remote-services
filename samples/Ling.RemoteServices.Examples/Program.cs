using Ling.RemoteServices.Examples;
using Ling.RemoteServices.AspNetCore;
using Ling.RemoteServices.Examples.Client.Pages;
using Ling.RemoteServices.Examples.Components;
using Ling.RemoteServices.Examples.Generated;
using Ling.RemoteServices.Examples.Shared;
using Microsoft.AspNetCore.OutputCaching;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(
        0,
        RemoteServiceJsonSerializerContext.Default);
});
builder.Services.AddRemoteServices(options =>
{
    options.AddEndpointPolicy("WeatherEndpoint", context =>
    {
        context.Endpoint.WithDescription("Returns generated weather forecast data.");
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("RemoteServices", policy =>
    {
        policy.WithOrigins("https://localhost:7163", "http://localhost:5066")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("Weather", policy =>
    {
        policy.Expire(TimeSpan.FromSeconds(30));
    });
});

builder.Services.AddScoped<IWeatherForecastService, WeatherForecastService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();

    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseCors();
app.UseAntiforgery();
app.UseOutputCache();

app.MapStaticAssets();
app.MapRemoteServices();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Ling.RemoteServices.Examples.Client._Imports).Assembly);

app.Run();
