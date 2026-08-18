using Dnet.QdrantAdmin.Api.Infrastructure.Factories;
using Dnet.QdrantAdmin.Api.Infrastructure.Models;
using Dnet.QdrantAdmin.Api.Infrastructure.Services;
using Dnet.QdrantAdmin.Application.Shared.Constants;
using Dnet.QdrantAdmin.Application.Shared.Dtos;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Qdrant.Client.Grpc;

namespace Dnet.QdrantAdmin.Api.Apis;

public static class LlmProviderApi
{
    public static RouteGroupBuilder LlmProviderApis(this RouteGroupBuilder group)
    {
        group.WithTags("LlmProviders");

        group.MapPost("/SimilaritySearch", async ([FromBody] SimilaritySearchDto similaritySearchDto,
                             IEmbeddingService embeddingService,
                             IQdrantService qdrantService,
                             IProblemDetailFactory problemDetailFactory,
                             IOptions<LlmProviderConfig> config,
                             HttpContext httpContext) =>
        {

            if (string.IsNullOrWhiteSpace(similaritySearchDto.CollectionName))
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "Collection can't be empty"));
            }

            if (string.IsNullOrWhiteSpace(similaritySearchDto.Text))
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "Text can't be empty"));
            }

            var collectionInfo = await qdrantService.GetCollectionInfoAsync(similaritySearchDto.CollectionName);

            var collectionDimension = checked((int)collectionInfo.Dimension);

            // Collections with named vectors require the vector name in the query.
            similaritySearchDto.VectorName ??= collectionInfo.VectorName;

            var providerConfig = string.IsNullOrWhiteSpace(similaritySearchDto.ProviderName)
                ? config.Value.Providers.FirstOrDefault(p => p.Models.Any(m => m.Model == similaritySearchDto.LlmModel))
                : config.Value.Providers.FirstOrDefault(p => string.Equals(p.Name, similaritySearchDto.ProviderName, StringComparison.OrdinalIgnoreCase));

            var model = providerConfig?.Models.FirstOrDefault(m => m.Model == similaritySearchDto.LlmModel);

            if (model is null)
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_MODEL, $"The LLM model '{similaritySearchDto.LlmModel}' is not configured"));
            }

            if (model.Dimensions.Any() && !model.Dimensions.Contains(collectionDimension))
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_MODEL, $"The LLM model '{similaritySearchDto.LlmModel}' can't create {collectionDimension}-dimension embeddings required by collection '{similaritySearchDto.CollectionName}'"));
            }

            similaritySearchDto.Dimension = collectionDimension;

            var inputs = new List<string>() { similaritySearchDto.Text };

            var embeddings = await embeddingService.GenerateEmbeddingsAsync(inputs, similaritySearchDto.ProviderName, similaritySearchDto.LlmModel, similaritySearchDto.Dimension);

            if (embeddings is null || embeddings.Count == 0)
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "Failed to generate embeddings for the provided text"));
            }

            var scoredPoints = new List<ScoredPoint>();

            var searchResultDtos = new List<SearchResultDto>();

            var embedding = embeddings[0];

            try
            {
                scoredPoints = (await qdrantService.SearchAsync(similaritySearchDto, embedding)).ToList();
            }
            catch (InvalidProtocolBufferException ex)
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, $"Invalid Qdrant filter JSON: {ex.Message}"));
            }

            foreach (var scoredPoint in scoredPoints)
            {
                searchResultDtos.Add(SearchResultMapper.Map(qdrantService, scoredPoint));
            }

            return Results.Ok(searchResultDtos);
        })
      .WithName("SimilaritySearch")
        .Produces<List<SearchResultDto>>()
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("/GetLlmModels", (
                             IOptions<LlmProviderConfig> config,
                             HttpContext httpContext
                             ) =>
         {
             var llmProvider = new LlmProviderDto();

             var models = new List<ModelDto>();

             foreach (var provider in config.Value.Providers)
             {
                 foreach (var item in provider.Models)
                 {
                     var model = new ModelDto()
                     {
                         Model = item.Model,
                         ProviderName = provider.Name,
                         Dimensions = item.Dimensions,
                         Default = item.Default
                     };

                     llmProvider.Models.Add(model);
                 }
             }

             return llmProvider;
         })
      .WithName("GetLlmModels")
      .Produces<LlmProviderDto>();

        return group;
    }
}
