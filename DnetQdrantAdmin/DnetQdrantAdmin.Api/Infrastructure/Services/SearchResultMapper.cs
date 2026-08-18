using Dnet.QdrantAdmin.Application.Shared.Dtos;
using Qdrant.Client.Grpc;

namespace Dnet.QdrantAdmin.Api.Infrastructure.Services;

public static class SearchResultMapper
{
    public static SearchResultDto Map(IQdrantService qdrantService, ScoredPoint scoredPoint)
    {
        var searchResultDto = new SearchResultDto
        {
            Score = scoredPoint.Score,
            PointId = scoredPoint.Id.HasNum ? scoredPoint.Id.Num.ToString() : scoredPoint.Id.HasUuid ? scoredPoint.Id.Uuid : string.Empty,
            PayloadString = scoredPoint.Payload is not null ? qdrantService.MapFieldToJson(scoredPoint.Payload) : string.Empty
        };

        if (scoredPoint.Payload != null)
        {
            foreach (var entry in scoredPoint.Payload)
            {
                switch (entry.Key)
                {
                    case "text":
                        searchResultDto.Text = entry.Value.StringValue;
                        break;

                    case "normalized_statement":
                        if (string.IsNullOrEmpty(searchResultDto.Text))
                        {
                            searchResultDto.Text = entry.Value.StringValue;
                        }
                        break;
                }
            }
        }

        return searchResultDto;
    }

    public static string GetPointIdString(PointId pointId)
    {
        return pointId.HasNum ? pointId.Num.ToString() : pointId.HasUuid ? pointId.Uuid : string.Empty;
    }
}
