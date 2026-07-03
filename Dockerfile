# SpaceSails — single-container deployment (plan §M10).
# The Server hosts everything: the Blazor WASM client, the SignalR hub, the authoritative
# session, and the departures-board API. One image, one Azure Container App.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore layers first for build caching.
COPY SpaceSails.slnx Directory.Build.props ./
COPY src/SpaceSails.Contracts/SpaceSails.Contracts.csproj src/SpaceSails.Contracts/
COPY src/SpaceSails.Core/SpaceSails.Core.csproj src/SpaceSails.Core/
COPY src/SpaceSails.Client/SpaceSails.Client.csproj src/SpaceSails.Client/
COPY src/SpaceSails.Server/SpaceSails.Server.csproj src/SpaceSails.Server/
RUN dotnet restore src/SpaceSails.Server/SpaceSails.Server.csproj

COPY src/ src/
COPY scenarios/ scenarios/
RUN dotnet publish src/SpaceSails.Server/SpaceSails.Server.csproj -c Release -o /app
# Publishing the Server copies the client's index.html with its fingerprint placeholders
# UNRESOLVED (blazor.webassembly#[.{fingerprint}].js) — only a Client publish runs the WASM
# SDK's resolution pass. Publish the client separately and overlay its finished wwwroot.
RUN dotnet publish src/SpaceSails.Client/SpaceSails.Client.csproj -c Release -o /client \
    && cp -r /client/wwwroot/. /app/wwwroot/

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# ACA's default ingress port; Kestrel binds it via ASPNETCORE_URLS.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SpaceSails.Server.dll"]
