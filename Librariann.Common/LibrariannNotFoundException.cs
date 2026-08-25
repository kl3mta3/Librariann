using System;

namespace Librariann.Common;

/// <summary>
/// Exception that is caught by the exception middleware, and returns NotFound
/// </summary>
public class LibrariannNotFoundException: Exception
{

    public LibrariannNotFoundException()
    {
    }

    public LibrariannNotFoundException(string message) : base(message)
    {
    }

    public LibrariannNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
