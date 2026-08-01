FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# curl is used by docker-compose's healthcheck against StudyHub.Api's /health endpoint.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ src/

RUN dotnet restore "src/UI/StudyHub.UI/StudyHub.UI.csproj"
RUN dotnet restore "src/UI/StudyHub.Api/StudyHub.Api.csproj"

FROM build AS publish-ui

RUN dotnet publish "src/UI/StudyHub.UI/StudyHub.UI.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

FROM build AS publish-api

RUN dotnet publish "src/UI/StudyHub.Api/StudyHub.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

FROM base AS final-ui

WORKDIR /app

COPY --from=publish-ui /app/publish .

ENTRYPOINT ["dotnet", "StudyHub.UI.dll"]

FROM base AS final-api

WORKDIR /app

COPY --from=publish-api /app/publish .

ENTRYPOINT ["dotnet", "StudyHub.Api.dll"]
