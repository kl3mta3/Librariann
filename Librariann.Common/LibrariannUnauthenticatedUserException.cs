using System;

namespace Librariann.Common;

/// <summary>
/// The user does not exist (aka unauthorized). This will be caught by middleware and Unauthorized() returned to UI
/// </summary>
/// <remarks>This will always log to Security Log</remarks>
public class LibrariannUnauthenticatedUserException : Exception
{
    public LibrariannUnauthenticatedUserException()
    { }

    public LibrariannUnauthenticatedUserException(string message) : base(message)
    { }

    public LibrariannUnauthenticatedUserException(string message, Exception inner)
        : base(message, inner) { }
}
