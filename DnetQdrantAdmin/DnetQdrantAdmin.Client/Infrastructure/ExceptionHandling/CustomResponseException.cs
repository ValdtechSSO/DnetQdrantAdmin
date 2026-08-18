using Dnet.QdrantAdmin.Client.Infrastructure.Models;

namespace Dnet.QdrantAdmin.Client.Infrastructure.ExceptionHandling;

public class CustomResponseException : Exception
{
    public ProblemDetails ProblemDetails { get; set; }


    public CustomResponseException()
    {
    }

    public CustomResponseException(string message, ProblemDetails problemDetails)
        : base(message)
    {
        ProblemDetails = problemDetails;
    }

    public CustomResponseException(string message, Exception inner, ProblemDetails problemDetails)
        : base(message, inner)
    {
        ProblemDetails = problemDetails;
    }
}
