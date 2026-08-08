using Dnet.QdrantAdmin.Api.Infrasctructure.Models;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Dnet.QdrantAdmin.Api.Infrasctructure.Services;

public class OpenAiService : IOpenAiService
{
    private readonly IOptions<LlmProviderConfig> _llmProviderConfig;

    public OpenAiService(IOptions<LlmProviderConfig> llmProviderConfig)
    {
        _llmProviderConfig = llmProviderConfig;
    }

    public async Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(List<string> inputs, string llmModel, int dimension)
    {
        var model = _llmProviderConfig.Value.Models.FirstOrDefault(model => model.Model == llmModel);

        if (model is null)
        {
            throw new ArgumentException($"The LLM model '{llmModel}' is not configured", nameof(llmModel));
        }

        if (model.Distances.Any() && !model.Distances.Contains(dimension))
        {
            throw new ArgumentException($"The LLM model '{llmModel}' does not support {dimension}-dimension embeddings", nameof(dimension));
        }

#pragma warning disable SKEXP0010
        var embeddingGenerator = new OpenAITextEmbeddingGenerationService(llmModel, _llmProviderConfig.Value.ApiKey, null, null, null, llmModel != "text-embedding-ada-002" ? dimension : null);

        var embeddings = await embeddingGenerator.GenerateEmbeddingsAsync(inputs);

        return embeddings;
#pragma warning restore SKEXP0010
    }
}
