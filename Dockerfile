FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/StudyHub.sln .

COPY src/ .

RUN dotnet restore "StudyHub.sln"

RUN dotnet build "StudyHub.sln" \
    -c Release \
    -o /app/build

FROM build AS publish

RUN dotnet publish "StudyHub.sln" \
    -c Release \
    -o /app/publish

FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "StudyHub.UI.dll"]