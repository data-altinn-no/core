namespace Dan.Common.Attributes;

/// <summary>
/// Marks a function as not requiring an authenticated caller. Dan.Core's authentication middleware skips
/// functions carrying this attribute. Plugins have no such middleware, so the attribute is inert there; it lives
/// in Dan.Common so functions shipped from this library (the health endpoints) can carry it.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class NoAuthenticationAttribute : Attribute
{
}
