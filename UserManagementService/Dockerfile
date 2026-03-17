FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SharedLibrary.Auth/SharedLibrary.Auth.csproj", "SharedLibrary.Auth/"]
COPY ["SharedLibrary.Messaging/SharedLibrary.Messaging.csproj", "SharedLibrary.Messaging/"]
COPY ["UserManagementService/src/UserManagementService/UserManagementService.csproj", "UserManagementService/src/UserManagementService/"]
RUN --mount=type=cache,id=nuget-usermgmt,target=/root/.nuget/packages \
    dotnet restore "UserManagementService/src/UserManagementService/UserManagementService.csproj"
COPY SharedLibrary.Auth/ SharedLibrary.Auth/
COPY SharedLibrary.Messaging/ SharedLibrary.Messaging/
COPY UserManagementService/src/UserManagementService/ UserManagementService/src/UserManagementService/
WORKDIR "/src/UserManagementService/src/UserManagementService"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserManagementService.dll"]
