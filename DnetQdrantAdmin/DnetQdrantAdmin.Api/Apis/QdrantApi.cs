using Dnet.QdrantAdmin.Api.Infrastructure.Factories;
using Dnet.QdrantAdmin.Api.Infrastructure.Services;
using Dnet.QdrantAdmin.Application.Shared.Constants;
using Dnet.QdrantAdmin.Application.Shared.Dtos;
using CsvHelper;
using CsvHelper.Configuration;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Qdrant.Client.Grpc;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dnet.QdrantAdmin.Api.Apis;

public static class QdrantApi
{
    public static RouteGroupBuilder QdrantApis(this RouteGroupBuilder group)
    {
        group.WithTags("Qdrant");

        group.MapPost("/CreateCollection", async ([FromBody] CreateCollectionDto createCollectionDto,
                              IQdrantService qdrantService,
                              IProblemDetailFactory problemDetailFactory,
                              CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(createCollectionDto.Name))
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "Collection name can't be empty"));
            }

            var collection = await qdrantService.CreateCollectionAsync(createCollectionDto, cancellationToken);

            return Results.Ok(collection);
        })
       .WithName("CreateCollection")
       .Produces<bool>()
       .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/UpdateCollection", async ([FromBody] UpdateCollectionDto updateCollectionDto,
                              IQdrantService qdrantService,
                              IProblemDetailFactory problemDetailFactory,
                              CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(updateCollectionDto.Name))
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "Collection name can't be empty"));
            }

            var collection = await qdrantService.UpdateCollectionAsync(updateCollectionDto, cancellationToken);

            return Results.Ok(collection);
        })
       .WithName("UpdateCollection")
       .Produces<bool>()
       .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("/ListCollections", async (
                             IQdrantService qdrantService
                             ) =>
        {
            var collection = await qdrantService.ListCollectionsAsync();

            return Results.Ok(collection);
        })
      .WithName("ListCollections")
      .Produces<List<CollectionDto>>();

        group.MapGet("/GetStats", async (
                             IQdrantService qdrantService,
                             QdrantRestClient qdrantRestClient,
                             CancellationToken cancellationToken
                             ) =>
        {
            var collections = await qdrantService.ListCollectionsAsync();

            var collectionInfos = await Task.WhenAll(collections.Select(c => qdrantService.GetCollectionInfoAsync(c.Name)));

            var details = await qdrantRestClient.GetCollectionsDetailsAsync(collections.Select(c => c.Name), cancellationToken);

            var cluster = await qdrantRestClient.GetClusterStatusAsync(cancellationToken);

            ulong pointsCount = 0;
            ulong? storageBytes = null;
            var statusGreen = 0;
            var statusYellow = 0;
            var statusRed = 0;
            var shardCount = 0;

            foreach (var detail in details)
            {
                if (detail.PointsCount > 0)
                {
                    pointsCount += detail.PointsCount;
                }

                if (detail.VectorsSize is not null)
                {
                    storageBytes = (storageBytes ?? 0) + detail.VectorsSize;
                }

                switch (detail.Status.ToLowerInvariant())
                {
                    case "green":
                        statusGreen++;
                        break;
                    case "yellow":
                        statusYellow++;
                        break;
                    case "red":
                    case "grey":
                        statusRed++;
                        break;
                }
            }

            // Fall back to the gRPC point counts when the REST API reports none.
            if (pointsCount == 0)
            {
                pointsCount = (ulong)collectionInfos.Sum(i => (decimal)i.PointsCount);
            }

            var shardCounts = await Task.WhenAll(collections.Select(c => SafeShardCountAsync(qdrantService, c.Name)));

            shardCount = shardCounts.Sum();

            return Results.Ok(new DashboardStatsDto
            {
                CollectionCount = collections.Count,
                PointsCount = pointsCount,
                StorageBytes = storageBytes,
                PeerCount = cluster.PeerCount,
                ClusterEnabled = cluster.Enabled,
                StatusGreen = statusGreen,
                StatusYellow = statusYellow,
                StatusRed = statusRed,
                ShardCount = shardCount
            });
        })
      .WithName("GetStats")
      .Produces<DashboardStatsDto>();

        group.MapPost("/DeleteCollection", async ([FromBody] CollectionNameDto collectionNameDto,
                             IQdrantService qdrantService,
                             IProblemDetailFactory problemDetailFactory
                             ) =>
        {
            if (string.IsNullOrWhiteSpace(collectionNameDto.Name))
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "Collection name can't be empty"));
            }

            await qdrantService.DeleteCollectionAsync(collectionNameDto.Name);

            return Results.NoContent();
        })
      .WithName("DeleteCollection")
      .Produces(StatusCodes.Status204NoContent)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/GetCollectionInfo", async ([FromBody] CollectionNameDto collectionNameDto,
                              IQdrantService qdrantService,
                              IProblemDetailFactory problemDetailFactory
                              ) =>
        {
            if (string.IsNullOrWhiteSpace(collectionNameDto.Name))
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "Collection name can't be empty"));
            }

            var collection = await qdrantService.GetCollectionInfoAsync(collectionNameDto.Name);

            return Results.Ok(collection);
        })
       .WithName("GetCollectionInfo")
       .Produces<CollectionInfoDto>()
       .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/ScrollPoints", async ([FromBody] ScrollDto scrollDto,
                            IQdrantService qdrantService,
                            IProblemDetailFactory problemDetailFactory
                            ) =>
        {
            if (string.IsNullOrWhiteSpace(scrollDto.CollectionName))
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "Collection name can't be empty"));
            }

            List<QpointDto> collection;

            try
            {
                collection = await qdrantService.ScrollAsync(scrollDto);
            }
            catch (InvalidProtocolBufferException ex)
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, $"Invalid Qdrant filter JSON: {ex.Message}"));
            }

            return Results.Ok(collection);
        })
     .WithName("ScrollPoints")
     .Produces<List<QpointDto>>()
     .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/RetrievePoint", async ([FromBody] ScrollDto scrollDto,
                                              IQdrantService qdrantService,
                                              IProblemDetailFactory problemDetailFactory
                              ) =>
           {
               if (string.IsNullOrWhiteSpace(scrollDto.CollectionName))
               {
                   return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "Collection name can't be empty"));
               }

               var collection = await qdrantService.RetrieveAsync(scrollDto);

               return Results.Ok(collection);
           })
       .WithName("RetrievePoint")
       .Produces<QpointDto>()
       .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/CreatePoint", async ([FromBody] QpointDto pointDto,
                                  IEmbeddingService embeddingService,
                                  IQdrantService qdrantService,
                                  IProblemDetailFactory problemDetailFactory
                                  ) =>
        {
            if (string.IsNullOrWhiteSpace(pointDto.CollectionName))
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "Collection name can't be empty"));
            }

            if (string.IsNullOrWhiteSpace(pointDto.Text))
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "Text can't be empty"));
            }

            var inputs = new List<string>() { pointDto.Text };

            var embeddings = await embeddingService.GenerateEmbeddingsAsync(inputs, pointDto.ProviderName, pointDto.LlmModel, pointDto.Dimension);

            if (embeddings is null || embeddings.Count == 0)
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "Failed to generate embeddings for the provided text"));
            }

            var updateResult = await qdrantService.InsertVectorsAsync(pointDto.CollectionName, pointDto, embeddings[0]);

            return Results.Ok(updateResult);
        })
        .WithName("CreatePoint")
        .Produces<UpdateResult>()
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/UpdatePoint", async ([FromBody] QpointDto pointDto,
                                  IEmbeddingService embeddingService,
                                  IQdrantService qdrantService,
                                  IProblemDetailFactory problemDetailFactory
                                  ) =>
        {
            if (string.IsNullOrWhiteSpace(pointDto.CollectionName))
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "Collection name can't be empty"));
            }

            if (string.IsNullOrWhiteSpace(pointDto.Text))
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "Text can't be empty"));
            }

            var inputs = new List<string>() { pointDto.Text };

            var embeddings = await embeddingService.GenerateEmbeddingsAsync(inputs, pointDto.ProviderName, pointDto.LlmModel, pointDto.Dimension);

            if (embeddings is null || embeddings.Count == 0)
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "Failed to generate embeddings for the provided text"));
            }

            var updateResult = await qdrantService.UpdatePointAsync(pointDto.CollectionName, pointDto, embeddings[0]);

            return Results.Ok(updateResult);
        })
        .WithName("UpdatePoint")
        .Produces<UpdateResult>()
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/CreatePoints", async ([FromBody] CreatePointsDto createPointsDto,
                                       IQdrantService qdrantService,
                                       IProblemDetailFactory problemDetailFactory) =>
        {
            if (string.IsNullOrWhiteSpace(createPointsDto.CollectionName))
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "Collection name can't be empty"));
            }

            if (createPointsDto.pointDtos is null || createPointsDto.pointDtos.Count == 0)
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "There are no points to create"));
            }

            var updateResult = await qdrantService.InsertVectorsBulkAsync(createPointsDto);

            return Results.Ok(updateResult);
        })
        .WithName("CreatePoints")
        .Produces<UpdateResult>()
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/DeletePoint", async ([FromBody] DeletePointDto deletePointDto,
                            IQdrantService qdrantService,
                            IProblemDetailFactory problemDetailFactory
                            ) =>
        {
            if (string.IsNullOrWhiteSpace(deletePointDto.CollectionName))
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "Collection name can't be empty"));
            }

            if (deletePointDto.Ids is null || deletePointDto.Ids.Count == 0)
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "There are no point ids to delete"));
            }

            var result = await qdrantService.DeletePointsAsync(deletePointDto);

            return Results.Ok(result);
        })
        .WithName("DeletePoint")
        .Produces<UpdateResult>()
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/SimilarPoints", async ([FromBody] SimilarPointsDto similarPointsDto,
                              IQdrantService qdrantService,
                              IProblemDetailFactory problemDetailFactory
                              ) =>
        {
            if (string.IsNullOrWhiteSpace(similarPointsDto.CollectionName))
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "Collection name can't be empty"));
            }

            if (string.IsNullOrWhiteSpace(similarPointsDto.QpointId))
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "Point id can't be empty"));
            }

            var scrollDto = new ScrollDto
            {
                CollectionName = similarPointsDto.CollectionName,
                QpointId = similarPointsDto.QpointId,
                PointIdType = similarPointsDto.PointIdType,
                WithVector = true,
                WithPayload = false
            };

            var point = await qdrantService.RetrieveAsync(scrollDto);

            if (point is null || point.Vectors is null || point.Vectors.Length == 0)
            {
                return Results.NotFound(problemDetailFactory.GetProblemDetail(ProblemDetailType.RESOURCE_NOT_FOUND, $"Point '{similarPointsDto.QpointId}' was not found or has no vector"));
            }

            var collectionInfo = await qdrantService.GetCollectionInfoAsync(similarPointsDto.CollectionName);

            var searchDto = new SimilaritySearchDto
            {
                CollectionName = similarPointsDto.CollectionName,
                Limit = similarPointsDto.Limit,
                FilterString = similarPointsDto.FilterString,
                ScoreThreshold = similarPointsDto.ScoreThreshold,
                VectorName = collectionInfo.VectorName
            };

            var scoredPoints = await qdrantService.SearchAsync(searchDto, point.Vectors);

            var results = scoredPoints
                .Where(p => SearchResultMapper.GetPointIdString(p.Id) != similarPointsDto.QpointId)
                .Select(p => SearchResultMapper.Map(qdrantService, p))
                .ToList();

            return Results.Ok(results);
        })
      .WithName("SimilarPoints")
      .Produces<List<SearchResultDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/GetImportQPointData", async (HttpRequest request) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest("Multipart form data is required.");
            }

            long maxFileSize = 1024 * 1024 * 15;

            var qpoints = new List<QpointDto>();
            var errors = new List<string>();
            var skippedCount = 0;
            var unparsableRows = 0;
            var headers = new List<string>();

            var form = await request.ReadFormAsync();

            var file = form.Files["files"] ?? form.Files.FirstOrDefault();

            var importConfig = new ImportConfigDto();

            if (!string.IsNullOrWhiteSpace(form["config"]))
            {
                try
                {
                    importConfig = JsonSerializer.Deserialize<ImportConfigDto>(form["config"].ToString(), new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new ImportConfigDto();
                }
                catch (JsonException ex)
                {
                    return Results.BadRequest($"Invalid import config JSON: {ex.Message}");
                }
            }

            if (file is not null)
            {
                if (file.Length > maxFileSize)
                {
                    return Results.BadRequest($"The file exceeds the maximum allowed size of {maxFileSize / (1024 * 1024)} MB.");
                }

                var fileFormat = importConfig.FileFormat.Trim().ToLowerInvariant();

                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);

                if (fileFormat == "jsonl" || fileFormat == "ndjson")
                {
                    ParseJsonLines(reader, importConfig, qpoints, headers, errors, ref skippedCount);
                }
                else
                {
                    var delimiter = fileFormat == "tsv" ? "\t" : ",";

                    ParseDelimited(reader, delimiter, importConfig, qpoints, headers, errors, ref skippedCount, ref unparsableRows);
                }
            }

            if (unparsableRows > 0)
            {
                skippedCount += unparsableRows;
                errors.Add($"{unparsableRows} row(s) skipped: could not be parsed");
            }

            return Results.Ok(new ImportPreviewDto
            {
                Points = qpoints,
                SkippedCount = skippedCount,
                Errors = errors,
                Headers = headers
            });
        })
       .WithName("GetImportQPointData")
       .Produces<ImportPreviewDto>();

        group.MapPost("/ListSnapshots", async ([FromBody] CollectionNameDto collectionNameDto,
                              IQdrantService qdrantService
                              ) =>
        {
            var snapshots = await qdrantService.ListSnapshotsAsync(collectionNameDto.Name);

            return Results.Ok(snapshots);
        })
      .WithName("ListSnapshots")
      .Produces<List<SnapshotDto>>();

        group.MapPost("/CreateSnapshot", async ([FromBody] CollectionNameDto collectionNameDto,
                              IQdrantService qdrantService
                              ) =>
        {
            var snapshot = await qdrantService.CreateSnapshotAsync(collectionNameDto.Name);

            return Results.Ok(snapshot);
        })
      .WithName("CreateSnapshot")
      .Produces<SnapshotDto>();

        group.MapPost("/DeleteSnapshot", async ([FromBody] DeleteSnapshotDto deleteSnapshotDto,
                              IQdrantService qdrantService
                              ) =>
        {
            await qdrantService.DeleteSnapshotAsync(deleteSnapshotDto.CollectionName, deleteSnapshotDto.SnapshotName);

            return Results.NoContent();
        })
      .WithName("DeleteSnapshot")
      .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/DownloadSnapshot/{collectionName}/{snapshotName}", async (string collectionName, string snapshotName,
                              QdrantRestClient qdrantRestClient,
                              CancellationToken cancellationToken) =>
        {
            var stream = await qdrantRestClient.DownloadSnapshotAsync(collectionName, snapshotName, cancellationToken);

            return Results.Stream(stream, contentType: "application/octet-stream", fileDownloadName: snapshotName);
        })
      .WithName("DownloadSnapshot");

        group.MapPost("/UploadSnapshot", async (HttpRequest request,
                              QdrantRestClient qdrantRestClient,
                              CancellationToken cancellationToken) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest("Multipart form data is required.");
            }

            var form = await request.ReadFormAsync(cancellationToken);

            var collectionName = form["collectionName"].ToString();
            var file = form.Files["file"];

            if (string.IsNullOrWhiteSpace(collectionName))
            {
                return Results.BadRequest("The collection name is required.");
            }

            if (file is null)
            {
                return Results.BadRequest("The snapshot file is required.");
            }

            await using var stream = file.OpenReadStream();

            await qdrantRestClient.UploadSnapshotAsync(collectionName, file.FileName, stream, cancellationToken);

            return Results.Ok();
        })
      .WithName("UploadSnapshot");

        group.MapGet("/ListAliases", async (
                              IQdrantService qdrantService
                              ) =>
        {
            var aliases = await qdrantService.ListAliasesAsync();

            return Results.Ok(aliases);
        })
      .WithName("ListAliases")
      .Produces<List<AliasDto>>();

        group.MapPost("/CreateAlias", async ([FromBody] CreateAliasDto createAliasDto,
                              IQdrantService qdrantService,
                              IProblemDetailFactory problemDetailFactory
                              ) =>
        {
            if (string.IsNullOrWhiteSpace(createAliasDto.AliasName) || string.IsNullOrWhiteSpace(createAliasDto.CollectionName))
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "Alias name and collection name are required"));
            }

            await qdrantService.CreateAliasAsync(createAliasDto);

            return Results.Ok();
        })
      .WithName("CreateAlias")
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/DeleteAlias", async ([FromBody] DeleteAliasDto deleteAliasDto,
                              IQdrantService qdrantService
                              ) =>
        {
            await qdrantService.DeleteAliasAsync(deleteAliasDto.AliasName);

            return Results.NoContent();
        })
      .WithName("DeleteAlias")
      .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/ListPayloadIndexes", async ([FromBody] CollectionNameDto collectionNameDto,
                              QdrantRestClient qdrantRestClient,
                              CancellationToken cancellationToken
                              ) =>
        {
            var indexes = await qdrantRestClient.GetCollectionIndexesAsync(collectionNameDto.Name, cancellationToken);

            return Results.Ok(indexes.Select(i => new PayloadIndexDto
            {
                FieldName = i.FieldName,
                FieldType = i.FieldType
            }).ToList());
        })
      .WithName("ListPayloadIndexes")
      .Produces<List<PayloadIndexDto>>();

        group.MapPost("/CreatePayloadIndex", async ([FromBody] CreatePayloadIndexDto createPayloadIndexDto,
                              IQdrantService qdrantService,
                              IProblemDetailFactory problemDetailFactory
                              ) =>
        {
            if (string.IsNullOrWhiteSpace(createPayloadIndexDto.CollectionName) || string.IsNullOrWhiteSpace(createPayloadIndexDto.FieldName))
            {
                return Results.BadRequest(problemDetailFactory.GetProblemDetail(ProblemDetailType.INVALID_REQUEST_PAYLOAD, "Collection name and field name are required"));
            }

            await qdrantService.CreatePayloadIndexAsync(createPayloadIndexDto);

            return Results.Ok();
        })
      .WithName("CreatePayloadIndex")
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/DeletePayloadIndex", async ([FromBody] DeletePayloadIndexDto deletePayloadIndexDto,
                              IQdrantService qdrantService
                              ) =>
        {
            await qdrantService.DeletePayloadIndexAsync(deletePayloadIndexDto);

            return Results.NoContent();
        })
      .WithName("DeletePayloadIndex")
      .Produces(StatusCodes.Status204NoContent);

        return group;
    }

    private static async Task<int> SafeShardCountAsync(IQdrantService qdrantService, string collectionName)
    {
        try
        {
            return await qdrantService.GetCollectionShardCountAsync(collectionName);
        }
        catch (Exception)
        {
            // Shard information is optional for the dashboard; never fail the whole request.
            return 0;
        }
    }

    private static void ParseJsonLines(StreamReader reader, ImportConfigDto config, List<QpointDto> qpoints, List<string> headers, List<string> errors, ref int skippedCount)
    {
        var headerSet = new List<string>();
        var lineNumber = 0;

        while (reader.ReadLine() is { } line)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line)) continue;

            JsonDocument doc;

            try
            {
                doc = JsonDocument.Parse(line);
            }
            catch (JsonException ex)
            {
                skippedCount++;
                errors.Add($"Line {lineNumber}: invalid JSON ({ex.Message})");
                continue;
            }

            using (doc)
            {
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    skippedCount++;
                    errors.Add($"Line {lineNumber}: not a JSON object");
                    continue;
                }

                var properties = doc.RootElement.EnumerateObject().ToList();

                foreach (var property in properties)
                {
                    if (!headerSet.Contains(property.Name))
                    {
                        headerSet.Add(property.Name);
                    }
                }

                var textPropertyName = !string.IsNullOrWhiteSpace(config.TextField) && properties.Any(p => p.Name == config.TextField)
                    ? config.TextField
                    : properties.FirstOrDefault().Name;

                var text = ToScalarString(properties.First(p => p.Name == textPropertyName).Value);

                if (string.IsNullOrWhiteSpace(text))
                {
                    skippedCount++;
                    errors.Add($"Line {lineNumber}: text is empty");
                    continue;
                }

                var payload = new JsonObject();

                foreach (var property in properties)
                {
                    if (property.Name == textPropertyName) continue;

                    if (config.PayloadFields.Count > 0 && !config.PayloadFields.Contains(property.Name)) continue;

                    payload[property.Name] = JsonNode.Parse(property.Value.GetRawText());
                }

                qpoints.Add(new QpointDto
                {
                    Text = text,
                    PayloadString = payload.Count > 0 ? payload.ToJsonString() : string.Empty
                });
            }
        }

        headers.AddRange(headerSet);
    }

    private static void ParseDelimited(StreamReader reader, string delimiter, ImportConfigDto config, List<QpointDto> qpoints, List<string> headers, List<string> errors, ref int skippedCount, ref int unparsableRows)
    {
        var localUnparsableRows = 0;

        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter,
            HasHeaderRecord = config.HasHeader,
            MissingFieldFound = null,
            BadDataFound = _ => localUnparsableRows++,
        };

        using var csv = new CsvReader(reader, csvConfig);

        var columns = new List<string>();

        if (config.HasHeader)
        {
            csv.Read();
            csv.ReadHeader();
            columns = csv.HeaderRecord?.ToList() ?? new List<string>();
        }

        var rowNumber = 0;

        foreach (var record in csv.GetRecords<dynamic>())
        {
            rowNumber++;

            var row = (IDictionary<string, object>)record;

            var keys = row.Keys.ToList();

            if (!config.HasHeader && columns.Count == 0)
            {
                columns = keys.Select((_, index) => $"Column {index + 1}").ToList();
            }

            // Resolve the text column: explicit selection first, otherwise the first column.
            string? textKey = null;

            if (!string.IsNullOrWhiteSpace(config.TextField))
            {
                if (row.ContainsKey(config.TextField))
                {
                    textKey = config.TextField;
                }
                else if (!config.HasHeader)
                {
                    var index = columns.IndexOf(config.TextField);

                    if (index >= 0 && index < keys.Count)
                    {
                        textKey = keys[index];
                    }
                }
            }

            textKey ??= keys.FirstOrDefault();

            if (textKey is null) continue;

            var text = Convert.ToString(row[textKey], CultureInfo.InvariantCulture) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(text))
            {
                skippedCount++;
                errors.Add($"Row {rowNumber}: text is empty");
                continue;
            }

            var payload = new JsonObject();

            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];

                if (key == textKey) continue;

                var displayName = config.HasHeader ? key : columns.ElementAtOrDefault(i) ?? key;

                if (config.PayloadFields.Count > 0 && !config.PayloadFields.Contains(displayName)) continue;

                payload[displayName] = ToJsonNode(Convert.ToString(row[key], CultureInfo.InvariantCulture));
            }

            qpoints.Add(new QpointDto
            {
                Text = text,
                PayloadString = payload.Count > 0 ? payload.ToJsonString() : string.Empty
            });
        }

        headers.AddRange(columns);

        unparsableRows += localUnparsableRows;
    }

    private static string ToScalarString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Null => string.Empty,
            JsonValueKind.Object or JsonValueKind.Array => element.GetRawText(),
            _ => element.ToString()
        };
    }

    private static JsonNode? ToJsonNode(string? raw)
    {
        if (raw is null) return null;

        var trimmed = raw.Trim();

        // Values that look like JSON objects/arrays are parsed as nested payload.
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
        {
            try
            {
                return JsonNode.Parse(trimmed);
            }
            catch (JsonException)
            {
                // Not valid JSON after all: keep it as a plain string.
            }
        }

        if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
        {
            return JsonValue.Create(longValue);
        }

        if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
        {
            return JsonValue.Create(doubleValue);
        }

        if (bool.TryParse(trimmed, out var boolValue))
        {
            return JsonValue.Create(boolValue);
        }

        return JsonValue.Create(raw);
    }
}
