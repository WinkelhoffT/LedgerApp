FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ src/

RUN dotnet restore "src/UI/StudyHub.UI/StudyHub.UI.csproj"

RUN dotnet build "src/UI/StudyHub.UI/StudyHub.UI.csproj" \
    -c Release \
    -o /app/build \
    --no-restore

FROM build AS publish

RUN dotnet publish "src/UI/StudyHub.UI/StudyHub.UI.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "StudyHub.UI.dll"]
