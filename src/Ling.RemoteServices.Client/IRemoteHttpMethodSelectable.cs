namespace Ling.RemoteServices.Client;

/// <summary>
/// Supports selecting an HTTP method for calls made through a generated remote service proxy view.
/// </summary>
/// <typeparam name="TService">The remote service contract type.</typeparam>
public interface IRemoteHttpMethodSelectable<out TService>
    where TService : class
{
    /// <summary>
    /// Creates a proxy view that uses the specified HTTP method.
    /// </summary>
    /// <param name="method">The HTTP method to use.</param>
    /// <returns>An immutable proxy view with the selected HTTP method.</returns>
    TService WithHttpMethod(RemoteHttpMethod method);
}
