namespace Dan.Core.Exceptions;

/// <summary>
/// The proxy exception coming from a evidence source.
/// </summary>
public class ProxiedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="T:Dan.Core.Exceptions.ProxiedException" /> class.
    /// </summary>
    /// <param name="message">
    /// The message.
    /// </param>
    public ProxiedException(string message) : base(message)
    {
    }
}