using Dnet.QdrantAdmin.Application.Shared.Dtos;

namespace Dnet.QdrantAdmin.Client.Pages.LlmProviders;

public interface ILlmProviderService
{
    Task<List<SearchResultDto>> SimilaritySearch(SimilaritySearchDto similaritySearchDto);

    Task<LlmProviderDto> GetLlmModels();
}
