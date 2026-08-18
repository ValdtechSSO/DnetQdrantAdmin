using Dnet.QdrantAdmin.Api.Infrastructure.Exceptions;
using Dnet.QdrantAdmin.Api.Infrastructure.Factories;
using Dnet.QdrantAdmin.Application.Shared.Constants;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

namespace Dnet.QdrantAdmin.Api.Infrastructure.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IProblemDetailFactory _problemDetailFactory;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IProblemDetailFactory problemDetailFactory)
    {
        _next = next;
        _logger = logger;
        _problemDetailFactory = problemDetailFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client disconnected: there is nobody left to respond to.
        }
        catch (PointAlreadyExistsException ex)
        {
            await WriteProblemAsync(context, CreateProblemDetail(StatusCodes.Status409Conflict, ex.Message));
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            await WriteProblemAsync(context, CreateProblemDetail(StatusCodes.Status404NotFound, ex.Message));
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
        {
            await WriteProblemAsync(context, CreateProblemDetail(StatusCodes.Status400BadRequest, ex.Message));
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            await WriteProblemAsync(context, CreateProblemDetail(StatusCodes.Status409Conflict, ex.Message));
        }
        catch (ArgumentException ex)
        {
            await WriteProblemAsync(context, CreateProblemDetail(StatusCodes.Status400BadRequest, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);

            await WriteProblemAsync(context, CreateProblemDetail(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later."));
        }
    }

    private ProblemDetails CreateProblemDetail(int statusCode, string detail)
    {
        try
        {
            var type = statusCode switch
            {
                StatusCodes.Status500InternalServerError => ProblemDetailType.OPERATION_EXCEPTION,
                StatusCodes.Status404NotFound => ProblemDetailType.RESOURCE_NOT_FOUND,
                StatusCodes.Status409Conflict => ProblemDetailType.RESOURCE_ALREADY_EXISTS,
                _ => ProblemDetailType.INVALID_REQUEST_PAYLOAD
            };

            return _problemDetailFactory.GetProblemDetail(type, detail);
        }
        catch (Exception factoryEx)
        {
            // The factory must never break error handling itself.
            _logger.LogError(factoryEx, "Failed to build the problem detail for status {Status}", statusCode);

            return new ProblemDetails
            {
                Type = "https://security.datalnet.com/errors/operation-exception",
                Title = "Operation Exception",
                Status = statusCode,
                Detail = detail,
            };
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, ProblemDetails problem)
    {
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problem);
    }
}
