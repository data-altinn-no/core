using System.Runtime.Serialization;

namespace Dan.Core.Models;

public class ConsentRequestError
{
    /// <summary>
    /// Gets or sets the error code on the exception
    /// </summary>
    [DataMember]
    public string ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the error message on the exception
    /// </summary>
    [DataMember]
    public string ErrorMessage { get; set; }
}
