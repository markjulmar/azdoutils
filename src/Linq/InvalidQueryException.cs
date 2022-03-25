using System.Runtime.Serialization;

namespace Julmar.AzDOUtilities.Linq;

/// <summary>
/// Invalid query exception type
/// </summary>
[Serializable]
public class InvalidQueryException : Exception
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="message"></param>
    public InvalidQueryException(string message) : base(message)
    {
    }

    /// <summary>
    /// Message for this exception
    /// </summary>
    public override string Message => "The client query is invalid: " + base.Message;

    /// <summary>
    /// Inner exception form of constructor
    /// </summary>
    /// <param name="message"></param>
    /// <param name="inner"></param>
    public InvalidQueryException(string message, Exception inner) : base(message, inner)
    {
    }

    /// <summary>
    /// Serialization constructor
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    protected InvalidQueryException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}