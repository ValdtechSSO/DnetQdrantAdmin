namespace Dnet.QdrantAdmin.Api.Infrastructure.Exceptions;

public class PointAlreadyExistsException : Exception
{
    public PointAlreadyExistsException(string pointId)
        : base($"A point with id '{pointId}' already exists in the collection")
    {
        PointId = pointId;
    }

    public string PointId { get; }
}
