# Stage 1: build and publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies (cache-friendly)
COPY ["DnetQdrantAdmin/DnetQdrantAdmin.Api/Dnet.QdrantAdmin.Api.csproj", "DnetQdrantAdmin/DnetQdrantAdmin.Api/"]
COPY ["DnetQdrantAdmin/DnetQdrantAdmin.Application.Shared/Dnet.QdrantAdmin.Application.Shared.csproj", "DnetQdrantAdmin/DnetQdrantAdmin.Application.Shared/"]
RUN dotnet restore "DnetQdrantAdmin/DnetQdrantAdmin.Api/Dnet.QdrantAdmin.Api.csproj"

# Build and publish
COPY DnetQdrantAdmin/ DnetQdrantAdmin/
RUN dotnet publish "DnetQdrantAdmin/DnetQdrantAdmin.Api/Dnet.QdrantAdmin.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: runtime (Ubuntu-based image so curl is available for the healthcheck)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=20s --retries=5 \
    CMD curl -sf http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "Dnet.QdrantAdmin.Api.dll"]
