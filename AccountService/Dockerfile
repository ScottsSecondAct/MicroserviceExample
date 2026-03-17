FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SharedLibrary.Messaging/SharedLibrary.Messaging.csproj", "SharedLibrary.Messaging/"]
COPY ["SharedLibrary.Accounts/SharedLibrary.Accounts.csproj", "SharedLibrary.Accounts/"]
COPY ["AccountService/src/AccountService/AccountService.csproj", "AccountService/src/AccountService/"]
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore "AccountService/src/AccountService/AccountService.csproj"
COPY SharedLibrary.Messaging/ SharedLibrary.Messaging/
COPY SharedLibrary.Accounts/ SharedLibrary.Accounts/
COPY AccountService/src/AccountService/ AccountService/src/AccountService/
WORKDIR "/src/AccountService/src/AccountService"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AccountService.dll"]
