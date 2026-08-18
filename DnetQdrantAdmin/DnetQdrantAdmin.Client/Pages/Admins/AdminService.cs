using Dnet.QdrantAdmin.Application.Shared.Dtos;
using Qdrant.Client.Grpc;
using System.Net.Http.Json;

namespace Dnet.QdrantAdmin.Client.Pages.Admin;

public class AdminService : IAdminService
{
    private readonly HttpClient _httpClient;

    public AdminService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> CreateCollection(CreateCollectionDto createCollectionDto)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/Qdrant/CreateCollection", createCollectionDto);

        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<bool> UpdateCollection(UpdateCollectionDto updateCollectionDto)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/Qdrant/UpdateCollection", updateCollectionDto);

        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<List<CollectionDto>> ListCollections()
    {
        return await _httpClient.GetFromJsonAsync<List<CollectionDto>>($"api/Qdrant/ListCollections");
    }

    public async Task<DashboardStatsDto> GetStats()
    {
        return await _httpClient.GetFromJsonAsync<DashboardStatsDto>($"api/Qdrant/GetStats");
    }

    public async Task DeleteCollection(string name)
    {
        await _httpClient.PostAsJsonAsync($"api/Qdrant/DeleteCollection", new CollectionNameDto { Name = name });
    }

    public async Task<CollectionInfoDto> GetCollectionInfo(string text)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/Qdrant/GetCollectionInfo", new CollectionNameDto { Name = text });

        return await response.Content.ReadFromJsonAsync<CollectionInfoDto>();
    }

    public async Task<List<QpointDto>> ScrollPoints(ScrollDto scrollDto)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/Qdrant/ScrollPoints", scrollDto);

        return await response.Content.ReadFromJsonAsync<List<QpointDto>>();
    }

    public async Task<QpointDto?> RetrievePoint(ScrollDto scrollDto)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/Qdrant/RetrievePoint", scrollDto);

        return await response.Content.ReadFromJsonAsync<QpointDto?>();
    }

    public async Task CreatePoint(QpointDto pointDto)
    {
        await _httpClient.PostAsJsonAsync($"api/Qdrant/CreatePoint", pointDto);
    }

    public async Task<UpdateResult> UpdatePoint(QpointDto pointDto)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/Qdrant/UpdatePoint", pointDto);

        return await response.Content.ReadFromJsonAsync<UpdateResult>();
    }

    public async Task<UpdateResult> DeletePoint(DeletePointDto deletePointDto)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/Qdrant/DeletePoint", deletePointDto);

        return await response.Content.ReadFromJsonAsync<UpdateResult>();
    }

    public async Task<List<SearchResultDto>> SimilarPoints(SimilarPointsDto similarPointsDto)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/Qdrant/SimilarPoints", similarPointsDto);

        return await response.Content.ReadFromJsonAsync<List<SearchResultDto>>();
    }

    public async Task<ImportPreviewDto> GetImportQPointData(MultipartFormDataContent content)
    {
        var response = await _httpClient.PostAsync($"api/Qdrant/GetImportQPointData", content);

        return await response.Content.ReadFromJsonAsync<ImportPreviewDto>();
    }

    public async Task CreatePoints(CreatePointsDto createPointsDto)
    {
        var url = $"api/Qdrant/CreatePoints";

        await _httpClient.PostAsJsonAsync(url, createPointsDto);
    }

    // Snapshots

    public async Task<List<SnapshotDto>> ListSnapshots(CollectionNameDto collectionNameDto)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/Qdrant/ListSnapshots", collectionNameDto);

        return await response.Content.ReadFromJsonAsync<List<SnapshotDto>>();
    }

    public async Task<SnapshotDto> CreateSnapshot(CollectionNameDto collectionNameDto)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/Qdrant/CreateSnapshot", collectionNameDto);

        return await response.Content.ReadFromJsonAsync<SnapshotDto>();
    }

    public async Task DeleteSnapshot(DeleteSnapshotDto deleteSnapshotDto)
    {
        await _httpClient.PostAsJsonAsync($"api/Qdrant/DeleteSnapshot", deleteSnapshotDto);
    }

    public string GetSnapshotDownloadUrl(string collectionName, string snapshotName)
    {
        var baseUrl = _httpClient.BaseAddress is not null ? _httpClient.BaseAddress.ToString() : string.Empty;

        return $"{baseUrl}api/Qdrant/DownloadSnapshot/{Uri.EscapeDataString(collectionName)}/{Uri.EscapeDataString(snapshotName)}";
    }

    public async Task UploadSnapshot(string collectionName, MultipartFormDataContent content)
    {
        content.Add(new StringContent(collectionName), "collectionName");

        await _httpClient.PostAsync($"api/Qdrant/UploadSnapshot", content);
    }

    // Aliases

    public async Task<List<AliasDto>> ListAliases()
    {
        return await _httpClient.GetFromJsonAsync<List<AliasDto>>($"api/Qdrant/ListAliases");
    }

    public async Task CreateAlias(CreateAliasDto createAliasDto)
    {
        await _httpClient.PostAsJsonAsync($"api/Qdrant/CreateAlias", createAliasDto);
    }

    public async Task DeleteAlias(DeleteAliasDto deleteAliasDto)
    {
        await _httpClient.PostAsJsonAsync($"api/Qdrant/DeleteAlias", deleteAliasDto);
    }

    // Payload indexes

    public async Task<List<PayloadIndexDto>> ListPayloadIndexes(CollectionNameDto collectionNameDto)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/Qdrant/ListPayloadIndexes", collectionNameDto);

        return await response.Content.ReadFromJsonAsync<List<PayloadIndexDto>>();
    }

    public async Task CreatePayloadIndex(CreatePayloadIndexDto createPayloadIndexDto)
    {
        await _httpClient.PostAsJsonAsync($"api/Qdrant/CreatePayloadIndex", createPayloadIndexDto);
    }

    public async Task DeletePayloadIndex(DeletePayloadIndexDto deletePayloadIndexDto)
    {
        await _httpClient.PostAsJsonAsync($"api/Qdrant/DeletePayloadIndex", deletePayloadIndexDto);
    }
}
