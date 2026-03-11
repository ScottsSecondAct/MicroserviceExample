FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ApiGateway/src/ApiGateway/ApiGateway.csproj", "ApiGateway/src/ApiGateway/"]
RUN dotnet restore "ApiGateway/src/ApiGateway/ApiGateway.csproj"
COPY ApiGateway/src/ApiGateway/ ApiGateway/src/ApiGateway/
WORKDIR "/src/ApiGateway/src/ApiGateway"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
