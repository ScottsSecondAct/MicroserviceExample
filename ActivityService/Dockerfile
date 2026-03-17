FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SharedLibrary.Messaging/SharedLibrary.Messaging.csproj", "SharedLibrary.Messaging/"]
COPY ["SharedLibrary.Activities/SharedLibrary.Activities.csproj", "SharedLibrary.Activities/"]
COPY ["ActivityService/src/ActivityService/ActivityService.csproj", "ActivityService/src/ActivityService/"]
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore "ActivityService/src/ActivityService/ActivityService.csproj"
COPY SharedLibrary.Messaging/ SharedLibrary.Messaging/
COPY SharedLibrary.Activities/ SharedLibrary.Activities/
COPY ActivityService/src/ActivityService/ ActivityService/src/ActivityService/
WORKDIR "/src/ActivityService/src/ActivityService"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ActivityService.dll"]
