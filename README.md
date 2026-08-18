# DnetQdrantAdmin
Datalnet Qdrant Vector Database Administrator

## Description
`DnetQdrantAdmin` is a web application designed for managing Qdrant vector database.

## Key Features

### Collection Management
- **Creation and Management of Collections**: create collections and view detailed information about each one.
- **Editing of Collections**: update HNSW, optimizer and replication parameters of an existing collection.
- **Snapshots**: create, list, download, delete and restore (upload) collection snapshots.
- **Aliases**: create, list and delete collection aliases.
- **Payload Indexes**: create and delete payload indexes (keyword, integer, float, text, geo, ...).

### Data Point Operations
- **Creation, Editing and Deletion of Points**: add, re-embed, and remove data points within collections.
- **Import**: CSV, TSV and JSONL files with column mapping (text column selection and payload column selection) and preview.
- **Server-side pagination**: order points by a payload field and filter them with a visual filter builder.

### Search and Similarity
- **Similarity-Based Search**: search by text with a visual Qdrant filter builder (or raw JSON).
- **Similar Points**: find the points closest to a given point id.

### Embedding Providers
- **Multi-provider embeddings**: OpenAI, Azure OpenAI, Ollama and Qdrant FastEmbed through a common `IEmbeddingProvider` abstraction.
- **Llm Models Management**: each provider exposes its own models and dimensions.

### Dashboard
- Overview of collections, points, vector storage, cluster nodes, shards and collection status (green/yellow/red).

## Configuration

Embedding providers are configured under `LlmProviderConfig`:

```json
"LlmProviderConfig": {
  "Providers": [
    {
      "Name": "openai",
      "Type": "OpenAI",
      "ApiKey": "<key>",
      "Models": [
        { "Model": "text-embedding-3-large", "Dimensions": [256, 1024, 3072], "Default": true }
      ]
    },
    { "Name": "ollama", "Type": "Ollama", "Endpoint": "http://localhost:11434",
      "Models": [ { "Model": "nomic-embed-text", "Dimensions": [768] } ] },
    { "Name": "azure", "Type": "AzureOpenAI", "ApiKey": "<key>", "Endpoint": "https://<resource>.openai.azure.com",
      "Models": [ { "Model": "text-embedding-3-large", "Dimensions": [3072] } ] },
    { "Name": "fastembed", "Type": "FastEmbed", "Endpoint": "http://localhost:6333",
      "Models": [ { "Model": "BAAI/bge-small-en-v1.5", "Dimensions": [384] } ] }
  ]
}
```

Environment variables override the file configuration (e.g. `QdrantConfig__QdrantServerHost`).

## Docker

Build and run the full stack (Qdrant + API + Web UI):

```bash
docker compose up --build
```

- Web UI: http://localhost:8082
- API: http://localhost:8081
- Qdrant REST: http://localhost:6333 · gRPC: http://localhost:6334

Both application containers expose a `/healthz` endpoint used by the Docker healthchecks.

## Technologies Used
- .NET 10
- Blazor (WebAssembly client + server shell)
- Datalnet Blazor components
- Qdrant.Client (gRPC) + Qdrant REST API
- Semantic Kernel / Microsoft.Extensions.AI
