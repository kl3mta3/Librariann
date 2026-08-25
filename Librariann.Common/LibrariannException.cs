using System;

namespace Librariann.Common;

/// <summary>
/// These are used for errors to send to the UI that should not be reported to Sentry
/// </summary>
public class LibrariannException : Exception
{
    public LibrariannException()
    { }

    public LibrariannException(string message) : base(message)
    { }

    public LibrariannException(string message, Exception inner)
        : base(message, inner) { }
}
