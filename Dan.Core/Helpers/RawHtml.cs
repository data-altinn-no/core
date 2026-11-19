namespace Dan.Core.Helpers;

/// <summary>
/// Marks a view value as HTML that is inserted into the view as it is. All other view values are HTML encoded. Only
/// use this for markup we produce ourselves, where any value from a request or an external source is already encoded.
/// </summary>
/// <param name="Value">The HTML</param>
public sealed record RawHtml(string Value)
{
    /// <inheritdoc />
    public override string ToString() => Value;
}
