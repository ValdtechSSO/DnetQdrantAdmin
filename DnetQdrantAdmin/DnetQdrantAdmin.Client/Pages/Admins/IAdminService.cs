using Dnet.QdrantAdmin.Application.Shared.Dtos;
using Qdrant.Client.Grpc;

namespace Dnet.QdrantAdmin.Client.Pages.Admin;

public interface IAdminService
{
    Task<bool> CreateCollection(CreateCollectionDto createCollectionDto);

    Task<bool> UpdateCollection(UpdateCollectionDto updateCollectionDto);

    Task<List<CollectionDto>> ListCollections();

    Task<DashboardStatsDto> GetStats();

    Task DeleteCollection(string name);

    Task<CollectionInfoDto> GetCollectionInfo(string text);

    Task<List<QpointDto>> ScrollPoints(ScrollDto scrollDto);

    Task<QpointDto?> RetrievePoint(ScrollDto scrollDto);

    Task CreatePoint(QpointDto pointDto);

    Task<UpdateResult> UpdatePoint(QpointDto pointDto);

    Task<UpdateResult> DeletePoint(DeletePointDto deletePointDto);

    Task<List<SearchResultDto>> SimilarPoints(SimilarPointsDto similarPointsDto);

    Task<ImportPreviewDto> GetImportQPointData(MultipartFormDataContent content);

    Task CreatePoints(CreatePointsDto createPointsDto);

    // Snapshots

    Task<List<SnapshotDto>> ListSnapshots(CollectionNameDto collectionNameDto);

    Task<SnapshotDto> CreateSnapshot(CollectionNameDto collectionNameDto);

    Task DeleteSnapshot(DeleteSnapshotDto deleteSnapshotDto);

    string GetSnapshotDownloadUrl(string collectionName, string snapshotName);

    Task UploadSnapshot(string collectionName, MultipartFormDataContent content);

    // Aliases

    Task<List<AliasDto>> ListAliases();

    Task CreateAlias(CreateAliasDto createAliasDto);

    Task DeleteAlias(DeleteAliasDto deleteAliasDto);

    // Payload indexes

    Task<List<PayloadIndexDto>> ListPayloadIndexes(CollectionNameDto collectionNameDto);

    Task CreatePayloadIndex(CreatePayloadIndexDto createPayloadIndexDto);

    Task DeletePayloadIndex(DeletePayloadIndexDto deletePayloadIndexDto);
}
