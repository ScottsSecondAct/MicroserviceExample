FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SharedLibrary.Auth/SharedLibrary.Auth.csproj", "SharedLibrary.Auth/"]
COPY ["SharedLibrary.Messaging/SharedLibrary.Messaging.csproj", "SharedLibrary.Messaging/"]
COPY ["AuthService/src/AuthService/AuthService.csproj", "AuthService/src/AuthService/"]
RUN dotnet restore "AuthService/src/AuthService/AuthService.csproj"
COPY SharedLibrary.Auth/ SharedLibrary.Auth/
COPY SharedLibrary.Messaging/ SharedLibrary.Messaging/
COPY AuthService/src/AuthService/ AuthService/src/AuthService/
WORKDIR "/src/AuthService/src/AuthService"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthService.dll"]
