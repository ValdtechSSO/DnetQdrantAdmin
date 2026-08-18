using Microsoft.AspNetCore.Mvc;

namespace Dnet.QdrantAdmin.Api.Infrastructure.Factories;

public interface IProblemDetailFactory
{
    ProblemDetails GetProblemDetail(string type, string details);
}
