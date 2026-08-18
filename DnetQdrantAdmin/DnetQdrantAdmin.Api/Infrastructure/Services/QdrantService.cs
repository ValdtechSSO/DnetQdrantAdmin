using Qdrant.Client.Grpc;
using Qdrant.Client;
using Dnet.QdrantAdmin.Application.Shared.Dtos;
using Google.Protobuf.Collections;
using Dnet.QdrantAdmin.Application.Shared.Enums;
using Value = Qdrant.Client.Grpc.Value;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text;
using Microsoft.Extensions.Options;
using Dnet.QdrantAdmin.Api.Infrastructure.Exceptions;
using Dnet.QdrantAdmin.Api.Infrastructure.Models;

namespace Dnet.QdrantAdmin.Api.Infrastructure.Services;

public class QdrantService : IQdrantService
{
    private readonly QdrantClient _client;
    private readonly IEmbeddingService _embeddingService;

    public QdrantService(IOptions<QdrantConfig> config, IEmbeddingService embeddingService)
    {
        _client = new QdrantClient(config.Value.QdrantServerHost);

        _embeddingService = embeddingService;
    }

    public async Task<bool> CreateCollectionAsync(CreateCollectionDto createCollectionDto, CancellationToken cancellationToken = default)
    {
        createCollectionDto.VectorParams ??= new VectorParams();
        createCollectionDto.VectorParams.Size = createCollectionDto.VectorParams.Size > 0 ? createCollectionDto.VectorParams.Size : 100;

        if (createCollectionDto.VectorParams.Distance == Qdrant.Client.Grpc.Distance.UnknownDistance)
        {
            createCollectionDto.VectorParams.Distance = Qdrant.Client.Grpc.Distance.Cosine;
        }

        if (createCollectionDto.OptimizersConfigDiff is not null)
        {
            if (createCollectionDto.OptimizersConfigDiff.HasMaxSegmentSize && createCollectionDto.OptimizersConfigDiff.MaxSegmentSize < 1)
            {
                createCollectionDto.OptimizersConfigDiff.ClearMaxSegmentSize();
            }

            if (createCollectionDto.OptimizersConfigDiff.HasVacuumMinVectorNumber && createCollectionDto.OptimizersConfigDiff.VacuumMinVectorNumber < 1)
            {
                createCollectionDto.OptimizersConfigDiff.ClearVacuumMinVectorNumber();
            }
        }

        await _client.CreateCollectionAsync(
            collectionName: createCollectionDto.Name,
            vectorsConfig: createCollectionDto.VectorParams,
            shardNumber: createCollectionDto.ShardNumber,
            replicationFactor: createCollectionDto.ReplicationFactor,
            writeConsistencyFactor: createCollectionDto.WriteConsistencyFactor,
            onDiskPayload: createCollectionDto.OnDiskPayload,
            hnswConfig: createCollectionDto.HnswConfigDiff,
            optimizersConfig: createCollectionDto.OptimizersConfigDiff,
            walConfig: createCollectionDto.WalConfigDiff,
            quantizationConfig: createCollectionDto.QuantizationConfig,
            initFromCollection: createCollectionDto.InitFromCollection,
            shardingMethod: createCollectionDto.ShardingMethod,
            sparseVectorsConfig: createCollectionDto.SparseVectorConfig,
            strictModeConfig: createCollectionDto.StrictModeConfig,
            timeout: createCollectionDto.Timeout,
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> UpdateCollectionAsync(UpdateCollectionDto updateCollectionDto, CancellationToken cancellationToken = default)
    {
        updateCollectionDto.HnswConfigDiff ??= new HnswConfigDiff();

        if (updateCollectionDto.HnswConfigDiff.HasM && updateCollectionDto.HnswConfigDiff.M < 1)
        {
            updateCollectionDto.HnswConfigDiff.ClearM();
        }

        if (updateCollectionDto.HnswConfigDiff.HasEfConstruct && updateCollectionDto.HnswConfigDiff.EfConstruct < 1)
        {
            updateCollectionDto.HnswConfigDiff.ClearEfConstruct();
        }

        if (updateCollectionDto.HnswConfigDiff.HasMaxIndexingThreads && updateCollectionDto.HnswConfigDiff.MaxIndexingThreads < 1)
        {
            updateCollectionDto.HnswConfigDiff.ClearMaxIndexingThreads();
        }

        updateCollectionDto.OptimizersConfigDiff ??= new OptimizersConfigDiff();

        if (updateCollectionDto.OptimizersConfigDiff.HasIndexingThreshold && updateCollectionDto.OptimizersConfigDiff.IndexingThreshold < 1)
        {
            updateCollectionDto.OptimizersConfigDiff.ClearIndexingThreshold();
        }

        updateCollectionDto.CollectionParamsDiff ??= new CollectionParamsDiff();

        if (updateCollectionDto.CollectionParamsDiff.HasReplicationFactor && updateCollectionDto.CollectionParamsDiff.ReplicationFactor < 1)
        {
            updateCollectionDto.CollectionParamsDiff.ClearReplicationFactor();
        }

        if (updateCollectionDto.CollectionParamsDiff.HasWriteConsistencyFactor && updateCollectionDto.CollectionParamsDiff.WriteConsistencyFactor < 1)
        {
            updateCollectionDto.CollectionParamsDiff.ClearWriteConsistencyFactor();
        }

        await _client.UpdateCollectionAsync(
            collectionName: updateCollectionDto.Name,
            optimizersConfig: updateCollectionDto.OptimizersConfigDiff,
            collectionParams: updateCollectionDto.CollectionParamsDiff,
            hnswConfig: updateCollectionDto.HnswConfigDiff,
            timeout: updateCollectionDto.Timeout,
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task<CollectionInfoDto> GetCollectionInfoAsync(string collectionName)
    {
        var result = await _client.GetCollectionInfoAsync(collectionName);

        var vectorsConfig = result.Config.Params.VectorsConfig;

        ulong dimension = 0;
        var distance = string.Empty;
        string? vectorName = null;

        switch (vectorsConfig.ConfigCase)
        {
            case VectorsConfig.ConfigOneofCase.Params:
                dimension = vectorsConfig.Params.Size;
                distance = vectorsConfig.Params.Distance.ToString();
                break;

            case VectorsConfig.ConfigOneofCase.ParamsMap:
                var firstVector = vectorsConfig.ParamsMap.Map.FirstOrDefault().Value;
                dimension = firstVector?.Size ?? 0;
                distance = firstVector?.Distance.ToString() ?? string.Empty;
                vectorName = vectorsConfig.ParamsMap.Map.FirstOrDefault().Key;
                break;
        }

        var collectionInfo = new CollectionInfoDto()
        {
            Name = collectionName,
            Status = result.Status.ToString(),
            VectorsCount = result.HasIndexedVectorsCount ? result.IndexedVectorsCount : result.HasPointsCount ? result.PointsCount : 0,
            SegmentsCount = result.SegmentsCount,
            PointsCount = result.PointsCount,
            IndexedVectorsCount = result.IndexedVectorsCount,
            M = result.Config.HnswConfig.M,
            EfConstruct = result.Config.HnswConfig.EfConstruct,
            FullScanThreshold = result.Config.HnswConfig.FullScanThreshold,
            MaxIndexingThreads = result.Config.HnswConfig.MaxIndexingThreads,
            OnDisk = result.Config.HnswConfig.Memory != Memory.Pinned,
            IndexingThreshold = result.Config.OptimizerConfig.IndexingThreshold,
            OnDiskPayload = result.Config.Params.Payload?.Memory == Memory.Cold,
            Dimension = dimension,
            Distance = distance,
            VectorName = vectorName,
            ReplicationFactor = result.Config.Params.ReplicationFactor,
            WriteConsistencyFactor = result.Config.Params.WriteConsistencyFactor,
            WalCapacityMb = result.Config.WalConfig.WalCapacityMb,
        };

        return collectionInfo;
    }

    public async Task DeleteCollectionAsync(string collectionName)
    {
        await _client.DeleteCollectionAsync(collectionName);
    }

    public async Task<List<CollectionDto>> ListCollectionsAsync()
    {
        var result = await _client.ListCollectionsAsync();

        var collections = new List<CollectionDto>();

        foreach (var item in result)
        {
            var collection = new CollectionDto()
            {
                Name = item
            };

            collections.Add(collection);
        }

        return collections;
    }

    public async Task<UpdateResult> InsertVectorsAsync(string collectionName, QpointDto pointDto, ReadOnlyMemory<float> vector)
    {
        var point = new PointStruct
        {
            Id = await CreatePointIdForInsertAsync(collectionName, pointDto),
            Vectors = vector.ToArray()
        };

        if (!string.IsNullOrEmpty(pointDto.PayloadString)) JsonToMapField(pointDto.PayloadString, point.Payload);

        var points = new List<PointStruct>
        {
            point
        };

        return await _client.UpsertAsync(collectionName, points);
    }

    public async Task<UpdateResult> UpdatePointAsync(string collectionName, QpointDto pointDto, ReadOnlyMemory<float> vector)
    {
        var point = new PointStruct
        {
            Id = BuildExplicitPointId(pointDto),
            Vectors = vector.ToArray()
        };

        if (!string.IsNullOrEmpty(pointDto.PayloadString)) JsonToMapField(pointDto.PayloadString, point.Payload);

        var points = new List<PointStruct>
        {
            point
        };

        return await _client.UpsertAsync(collectionName, points);
    }

    public async Task<UpdateResult> InsertVectorsBulkAsync(CreatePointsDto createPointsDto)
    {
        var points = new List<PointStruct>();

        var inputs = createPointsDto.pointDtos.Select(p => p.Text).ToList();

        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(inputs, createPointsDto.ProviderName, createPointsDto.LlmModel, createPointsDto.Dimension);

        for (int i = 0; i < createPointsDto.pointDtos.Count; i++)
        {
            var pointDto = createPointsDto.pointDtos[i];

            var embedding = embeddings[i];

            var point = new PointStruct
            {
                Id = BuildInsertPointId(pointDto),
                Vectors = embedding.ToArray()
            };

            if (!string.IsNullOrEmpty(pointDto.PayloadString)) JsonToMapField(pointDto.PayloadString, point.Payload);

            points.Add(point);
        }

        return await _client.UpsertAsync(createPointsDto.CollectionName, points);
    }

    private async Task<PointId> CreatePointIdForInsertAsync(string collectionName, QpointDto pointDto)
    {
        // GUID ids can be generated safely on the client side.
        if (pointDto.HasUuid)
        {
            return Guid.NewGuid();
        }

        // Numeric ids must be provided explicitly: deriving them from the collection
        // count collides with existing points after deletions and under concurrency.
        if (!ulong.TryParse(pointDto.QpointId, out ulong numericId))
        {
            throw new ArgumentException("A numeric point id is required when GUID ids are disabled. Provide a point id or enable GUID ids.");
        }

        var existing = await _client.RetrieveAsync(collectionName, numericId, withPayload: false, withVectors: false);

        if (existing.Count > 0)
        {
            throw new PointAlreadyExistsException(pointDto.QpointId);
        }

        return numericId;
    }

    private static PointId BuildInsertPointId(QpointDto pointDto)
    {
        // Bulk imports default to generated GUID ids; explicit ids are respected when provided.
        if (pointDto.PointId is not null)
        {
            return pointDto.PointId;
        }

        return Guid.NewGuid();
    }

    private static PointId BuildExplicitPointId(QpointDto pointDto)
    {
        if (pointDto.PointId is not null)
        {
            return pointDto.PointId;
        }

        if (pointDto.HasUuid && Guid.TryParse(pointDto.QpointId, out Guid uuid))
        {
            return uuid;
        }

        if (pointDto.HasNum && ulong.TryParse(pointDto.QpointId, out ulong numericId))
        {
            return numericId;
        }

        throw new ArgumentException($"The point id '{pointDto.QpointId}' is not valid for the selected id type");
    }

    public MapField<string, Value> JsonToMapField(string json, MapField<string, Value> result)
    {
        using JsonDocument doc = JsonDocument.Parse(json);

        foreach (JsonProperty property in doc.RootElement.EnumerateObject())
        {
            result[property.Name] = ConvertJsonElementToValue(property.Value);
        }

        return result;
    }

    public Value JsonToValue(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);

        return ConvertJsonElementToValue(doc.RootElement);
    }

    private Value ConvertJsonElementToValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var structValue = new Struct();
                foreach (JsonProperty prop in element.EnumerateObject())
                {
                    structValue.Fields[prop.Name] = ConvertJsonElementToValue(prop.Value);
                }
                return new Value { StructValue = structValue };

            case JsonValueKind.Array:
                var listValue = new ListValue();
                foreach (JsonElement item in element.EnumerateArray())
                {
                    listValue.Values.Add(ConvertJsonElementToValue(item));
                }
                return new Value { ListValue = listValue };

            case JsonValueKind.Number:
                if (element.TryGetInt64(out long intValue))
                {
                    return new Value { IntegerValue = intValue };
                }
                else
                {
                    return new Value { DoubleValue = element.GetDouble() };
                }

            case JsonValueKind.String:
                return new Value { StringValue = element.GetString() };

            case JsonValueKind.True:
            case JsonValueKind.False:
                return new Value { BoolValue = element.GetBoolean() };

            case JsonValueKind.Null:
                return new Value { NullValue = NullValue.NullValue };

            default:
                throw new ArgumentException($"Unsupported JSON value kind: {element.ValueKind}");
        }
    }

    public async Task<List<QpointDto>> ScrollAsync(ScrollDto scrollDto)
    {
        var filter = ParseFilter(scrollDto.FilterString);

        var orderBy = string.IsNullOrWhiteSpace(scrollDto.OrderByPayloadField)
            ? null
            : new OrderBy
            {
                Key = scrollDto.OrderByPayloadField,
                Direction = scrollDto.OrderByDescending ? Direction.Desc : Direction.Asc
            };

        var scrollResponse = await _client.ScrollAsync(
            collectionName: scrollDto.CollectionName,
            filter: filter,
            limit: scrollDto.Limit,
            offset: scrollDto.Offset,
            payloadSelector: null,
            vectorsSelector: null,
            orderBy: orderBy,
            cancellationToken: CancellationToken.None);

        var collections = new List<QpointDto>();

        foreach (var item in scrollResponse.Result)
        {
            var collection = new QpointDto()
            {
                CollectionName = scrollDto.CollectionName,
                QpointId = item.Id.HasNum ? item.Id.Num.ToString() : item.Id.HasUuid ? item.Id.Uuid : string.Empty,
                PayloadString = MapFieldToJson(item.Payload),
                HasNum = item.Id.HasNum,
                HasUuid = item.Id.HasUuid,
                PointIdType = item.Id.HasNum ? PointIdType.Numerical : item.Id.HasUuid ? PointIdType.Uuid : PointIdType.None,
                PointId = item.Id,
            };

            collections.Add(collection);
        }

        return collections;
    }

    public async Task<QpointDto?> RetrieveAsync(ScrollDto scrollDto)
    {
        var scrollResponse = new List<RetrievedPoint>();

        switch (scrollDto.PointIdType)
        {
            case PointIdType.None:
                break;

            case PointIdType.Numerical:

                bool result = ulong.TryParse(scrollDto.QpointId, out ulong value);

                if (result)
                {
                    scrollResponse = (await _client.RetrieveAsync(scrollDto.CollectionName, value, scrollDto.WithPayload, scrollDto.WithVector)).ToList();
                }

                break;

            case PointIdType.Uuid:

                bool result1 = Guid.TryParse(scrollDto.QpointId, out Guid guid);

                if (result1)
                {
                    scrollResponse = (await _client.RetrieveAsync(scrollDto.CollectionName, guid, scrollDto.WithPayload, scrollDto.WithVector)).ToList();
                }

                break;
        }

        var collections = new List<QpointDto>();

        foreach (var item in scrollResponse)
        {
            var collection = new QpointDto()
            {
                CollectionName = scrollDto.CollectionName,
                QpointId = item.Id.HasNum ? item.Id.Num.ToString() : item.Id.HasUuid ? item.Id.Uuid : string.Empty,
                Vectors = scrollDto.WithVector ? GetVectorData(item.Vectors) : null,
                PayloadString = MapFieldToJson(item.Payload),
            };

            collections.Add(collection);
        }

        return collections.Any() ? collections.FirstOrDefault() : new QpointDto();
    }

    public async Task<IReadOnlyList<ScoredPoint>> SearchAsync(SimilaritySearchDto similaritySearchDto, ReadOnlyMemory<float> vector)
    {
        var filter = ParseFilter(similaritySearchDto.FilterString);
        Query query = similaritySearchDto.SparseIndices is { Length: > 0 } sparseIndices
            ? (vector.ToArray(), sparseIndices)
            : vector.ToArray();

        var points = await _client.QueryAsync(
            collectionName: similaritySearchDto.CollectionName,
            query: query,
            prefetch: null,
            usingVector: similaritySearchDto.VectorName,
            filter: filter,
            scoreThreshold: similaritySearchDto.ScoreThreshold,
            searchParams: similaritySearchDto.SearchParams,
            limit: similaritySearchDto.Limit,
            offset: similaritySearchDto.Offset,
            payloadSelector: similaritySearchDto.WithPayloadSelector,
            vectorsSelector: similaritySearchDto.WithVectorsSelector,
            readConsistency: similaritySearchDto.ReadConsistency,
            shardKeySelector: similaritySearchDto.ShardKeySelector,
            lookupFrom: null,
            timeout: similaritySearchDto.Timeout,
            cancellationToken: CancellationToken.None);

        return points;
    }

    private static float[]? GetVectorData(VectorsOutput vectors)
    {
        var vector = vectors.VectorsOptionsCase switch
        {
            VectorsOutput.VectorsOptionsOneofCase.Vector => vectors.Vector,
            VectorsOutput.VectorsOptionsOneofCase.Vectors => vectors.Vectors.Vectors.Values.FirstOrDefault(v => v.GetDenseVector() is not null),
            _ => null
        };

        return vector?.GetDenseVector()?.Data.ToArray();
    }

    private static Filter? ParseFilter(string? filterString)
    {
        if (string.IsNullOrWhiteSpace(filterString)) return null;

        return Filter.Parser.ParseJson(NormalizeFilterJson(filterString));
    }

    private static string NormalizeFilterJson(string filterString)
    {
        var filterNode = JsonNode.Parse(filterString);

        if (filterNode is not JsonObject filterObject) return filterString;

        NormalizeConditions(filterObject, "must");
        NormalizeConditions(filterObject, "should");
        NormalizeConditions(filterObject, "must_not");

        return filterObject.ToJsonString();
    }

    private static void NormalizeConditions(JsonObject filterObject, string conditionName)
    {
        if (filterObject[conditionName] is not JsonArray conditions) return;

        for (var index = 0; index < conditions.Count; index++)
        {
            if (conditions[index] is JsonObject condition && condition.ContainsKey("key"))
            {
                conditions[index] = new JsonObject
                {
                    ["field"] = condition.DeepClone()
                };
            }
        }
    }

    public async Task<UpdateResult> DeletePointsAsync(DeletePointDto deletePointDto)
    {
        List<ulong> ulongIds = [];
        List<Guid> guidIds = [];

        foreach (var id in deletePointDto.Ids)
        {
            if (ulong.TryParse(id, out ulong longId))
            {
                ulongIds.Add(longId);
            }
            else if (Guid.TryParse(id, out Guid guidId))
            {
                guidIds.Add(guidId);
            }
            else
            {
                throw new ArgumentException($"Invalid ID format: {id}");
            }
        }

        UpdateResult updateResult = new();

        if (ulongIds.Any())
        {
            updateResult = await _client.DeleteAsync(deletePointDto.CollectionName, ulongIds, deletePointDto.Wait, deletePointDto.WriteOrderingType, deletePointDto.ShardKeySelector);
        }

        if (guidIds.Any())
        {
            updateResult = await _client.DeleteAsync(deletePointDto.CollectionName, guidIds, deletePointDto.Wait, deletePointDto.WriteOrderingType, deletePointDto.ShardKeySelector);
        }

        return updateResult;
    }

    public string MapFieldToJson(MapField<string, Value> mapField)
    {
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); // Start of JSON object

            // Qdrant stores payloads as hash maps whose iteration order is not stable:
            // sort the keys so the JSON is deterministic and easy to compare between points.
            foreach (var entry in mapField.OrderBy(e => e.Key, StringComparer.Ordinal))
            {
                writer.WritePropertyName(entry.Key);
                WriteValue(writer, entry.Value); // Write each Value object to JSON
            }

            writer.WriteEndObject(); // End of JSON object
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteValue(Utf8JsonWriter writer, Value value)
    {
        switch (value.KindCase)
        {
            case Value.KindOneofCase.StringValue:
                writer.WriteStringValue(value.StringValue);
                break;
            case Value.KindOneofCase.IntegerValue:
                writer.WriteNumberValue(value.IntegerValue);
                break;
            case Value.KindOneofCase.DoubleValue:
                writer.WriteNumberValue(value.DoubleValue);
                break;
            case Value.KindOneofCase.BoolValue:
                writer.WriteBooleanValue(value.BoolValue);
                break;
            case Value.KindOneofCase.ListValue:
                writer.WriteStartArray(); // Start of a list
                foreach (var item in value.ListValue.Values)
                {
                    WriteValue(writer, item); // Recursively write the list items
                }
                writer.WriteEndArray(); // End of a list
                break;
            case Value.KindOneofCase.StructValue:
                writer.WriteStartObject(); // Start of an object
                foreach (var field in value.StructValue.Fields.OrderBy(f => f.Key, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(field.Key);
                    WriteValue(writer, field.Value); // Recursively write the object properties
                }
                writer.WriteEndObject(); // End of an object
                break;
            case Value.KindOneofCase.NullValue:
                writer.WriteNullValue();
                break;
            // Add other cases as needed
            default:
                throw new ArgumentException($"Unsupported Value kind: {value.KindCase}");
        }
    }

    // Snapshots

    public async Task<List<SnapshotDto>> ListSnapshotsAsync(string collectionName)
    {
        var snapshots = await _client.ListSnapshotsAsync(collectionName);

        return snapshots.Select(s => new SnapshotDto
        {
            Name = s.Name,
            Size = s.Size,
            CreationTime = s.CreationTime.ToDateTime().ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
        }).ToList();
    }

    public async Task<SnapshotDto> CreateSnapshotAsync(string collectionName)
    {
        var snapshot = await _client.CreateSnapshotAsync(collectionName);

        return new SnapshotDto
        {
            Name = snapshot.Name,
            Size = snapshot.Size,
            CreationTime = snapshot.CreationTime.ToDateTime().ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    public async Task DeleteSnapshotAsync(string collectionName, string snapshotName)
    {
        await _client.DeleteSnapshotAsync(collectionName, snapshotName);
    }

    // Aliases

    public async Task<List<AliasDto>> ListAliasesAsync()
    {
        var aliases = await _client.ListAliasesAsync();

        return aliases.Select(a => new AliasDto
        {
            Name = a.AliasName,
            CollectionName = a.CollectionName
        }).ToList();
    }

    public async Task CreateAliasAsync(CreateAliasDto createAliasDto)
    {
        await _client.CreateAliasAsync(createAliasDto.AliasName, createAliasDto.CollectionName);
    }

    public async Task DeleteAliasAsync(string aliasName)
    {
        await _client.DeleteAliasAsync(aliasName);
    }

    // Payload indexes

    public async Task CreatePayloadIndexAsync(CreatePayloadIndexDto createPayloadIndexDto)
    {
        if (!Enum.TryParse<PayloadSchemaType>(createPayloadIndexDto.FieldType, ignoreCase: true, out var schemaType) || schemaType == PayloadSchemaType.UnknownType)
        {
            throw new ArgumentException($"'{createPayloadIndexDto.FieldType}' is not a valid payload index type");
        }

        await _client.CreatePayloadIndexAsync(createPayloadIndexDto.CollectionName, createPayloadIndexDto.FieldName, schemaType, indexParams: null, wait: true);
    }

    public async Task DeletePayloadIndexAsync(DeletePayloadIndexDto deletePayloadIndexDto)
    {
        await _client.DeletePayloadIndexAsync(deletePayloadIndexDto.CollectionName, deletePayloadIndexDto.FieldName, wait: true);
    }

    // Cluster / shards

    public async Task<int> GetCollectionShardCountAsync(string collectionName)
    {
        var clusterInfo = await _client.GetCollectionClusterSetupInfoAsync(collectionName);

        return (int)clusterInfo.ShardCount;
    }
}
