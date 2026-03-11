FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SharedLibrary.Messaging/SharedLibrary.Messaging.csproj", "SharedLibrary.Messaging/"]
COPY ["SharedLibrary.Contacts/SharedLibrary.Contacts.csproj", "SharedLibrary.Contacts/"]
COPY ["ContactService/src/ContactService/ContactService.csproj", "ContactService/src/ContactService/"]
RUN dotnet restore "ContactService/src/ContactService/ContactService.csproj"
COPY SharedLibrary.Messaging/ SharedLibrary.Messaging/
COPY SharedLibrary.Contacts/ SharedLibrary.Contacts/
COPY ContactService/src/ContactService/ ContactService/src/ContactService/
WORKDIR "/src/ContactService/src/ContactService"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ContactService.dll"]
