# Stage 1: build and publish (the Blazor WASM client is compiled into the shell)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# The URL of the API as seen from the browser, baked into the client runtime config
ARG API_URL=http://localhost:8081/

COPY ["DnetQdrantAdmin/DnetQdrantAdmin/Dnet.QdrantAdmin.csproj", "DnetQdrantAdmin/DnetQdrantAdmin/"]
COPY ["DnetQdrantAdmin/DnetQdrantAdmin.Client/Dnet.QdrantAdmin.Client.csproj", "DnetQdrantAdmin/DnetQdrantAdmin.Client/"]
COPY ["DnetQdrantAdmin/DnetQdrantAdmin.Application.Shared/Dnet.QdrantAdmin.Application.Shared.csproj", "DnetQdrantAdmin/DnetQdrantAdmin.Application.Shared/"]
RUN dotnet restore "DnetQdrantAdmin/DnetQdrantAdmin/Dnet.QdrantAdmin.csproj"

COPY DnetQdrantAdmin/ DnetQdrantAdmin/
RUN printf '{ "QDRANTADMIN_API_URL": "%s" }' "$API_URL" > DnetQdrantAdmin/DnetQdrantAdmin.Client/wwwroot/appsettings.json

RUN dotnet publish "DnetQdrantAdmin/DnetQdrantAdmin/Dnet.QdrantAdmin.csproj" -c Release -o /app/publish /p:UseAppHost=false

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

ENTRYPOINT ["dotnet", "Dnet.QdrantAdmin.dll"]
