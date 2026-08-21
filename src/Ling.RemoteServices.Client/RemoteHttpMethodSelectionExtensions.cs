namespace Ling.RemoteServices.Client;

/// <summary>
/// Provides HTTP method selection for generated remote service proxies.
/// </summary>
public static class RemoteHttpMethodSelectionExtensions
{
    /// <summary>
    /// Selects the HTTP method used by calls made through the returned proxy view.
    /// </summary>
    /// <typeparam name="TService">The remote service contract type.</typeparam>
    /// <param name="service">The remote service instance.</param>
    /// <param name="method">The HTTP method to use.</param>
    /// <returns>
    /// An immutable generated proxy view, or the original instance when the service is a local implementation.
    /// </returns>
    /// <remarks>
    /// Returning a local implementation unchanged allows shared Blazor Auto components to use this API
    /// without coupling server-side execution to an HTTP transport.
    /// </remarks>
    public static TService WithHttpMethod<TService>(
        this TService service,
        RemoteHttpMethod method)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(service);

        return service is IRemoteHttpMethodSelectable<TService> selectable
            ? selectable.WithHttpMethod(method)
            : service;
    }
}
