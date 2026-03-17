FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SharedLibrary.Messaging/SharedLibrary.Messaging.csproj", "SharedLibrary.Messaging/"]
COPY ["SharedLibrary.Deals/SharedLibrary.Deals.csproj", "SharedLibrary.Deals/"]
COPY ["SharedLibrary.Activities/SharedLibrary.Activities.csproj", "SharedLibrary.Activities/"]
COPY ["SharedLibrary.Contacts/SharedLibrary.Contacts.csproj", "SharedLibrary.Contacts/"]
COPY ["ReportingService/src/ReportingService/ReportingService.csproj", "ReportingService/src/ReportingService/"]
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore "ReportingService/src/ReportingService/ReportingService.csproj"
COPY SharedLibrary.Messaging/ SharedLibrary.Messaging/
COPY SharedLibrary.Deals/ SharedLibrary.Deals/
COPY SharedLibrary.Activities/ SharedLibrary.Activities/
COPY SharedLibrary.Contacts/ SharedLibrary.Contacts/
COPY ReportingService/src/ReportingService/ ReportingService/src/ReportingService/
WORKDIR "/src/ReportingService/src/ReportingService"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ReportingService.dll"]
