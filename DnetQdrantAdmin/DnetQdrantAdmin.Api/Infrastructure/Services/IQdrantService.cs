using Dnet.QdrantAdmin.Application.Shared.Dtos;
using Google.Protobuf.Collections;
using Qdrant.Client.Grpc;

namespace Dnet.QdrantAdmin.Api.Infrastructure.Services;

public interface IQdrantService
{
    Task<bool> CreateCollectionAsync(CreateCollectionDto createCollectionDto, CancellationToken cancellationToken = default);

    Task<bool> UpdateCollectionAsync(UpdateCollectionDto updateCollectionDto, CancellationToken cancellationToken = default);

    Task<List<CollectionDto>> ListCollectionsAsync();

    Task<CollectionInfoDto> GetCollectionInfoAsync(string collectionName);

    Task DeleteCollectionAsync(string collectionName);

    Task<UpdateResult> InsertVectorsAsync(string collectionName, QpointDto pointDto, ReadOnlyMemory<float> vector);

    Task<UpdateResult> UpdatePointAsync(string collectionName, QpointDto pointDto, ReadOnlyMemory<float> vector);

    Task<UpdateResult> InsertVectorsBulkAsync(CreatePointsDto createPointsDto);

    Task<IReadOnlyList<ScoredPoint>> SearchAsync(SimilaritySearchDto similaritySearchDto, ReadOnlyMemory<float> vector);

    Task<QpointDto?> RetrieveAsync(ScrollDto scrollDto);

    Task<List<QpointDto>> ScrollAsync(ScrollDto scrollDto);

    Task<UpdateResult> DeletePointsAsync(DeletePointDto deletePointDto);

    string MapFieldToJson(MapField<string, Value> mapField);

    // Snapshots

    Task<List<SnapshotDto>> ListSnapshotsAsync(string collectionName);

    Task<SnapshotDto> CreateSnapshotAsync(string collectionName);

    Task DeleteSnapshotAsync(string collectionName, string snapshotName);

    // Aliases

    Task<List<AliasDto>> ListAliasesAsync();

    Task CreateAliasAsync(CreateAliasDto createAliasDto);

    Task DeleteAliasAsync(string aliasName);

    // Payload indexes

    Task CreatePayloadIndexAsync(CreatePayloadIndexDto createPayloadIndexDto);

    Task DeletePayloadIndexAsync(DeletePayloadIndexDto deletePayloadIndexDto);

    // Cluster / shards

    Task<int> GetCollectionShardCountAsync(string collectionName);
}
