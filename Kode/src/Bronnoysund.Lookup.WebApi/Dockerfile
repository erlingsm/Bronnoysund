# SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

# Multi-stage build for ASP.NET Core 10.0 — final image er Alpine for minimal størrelse.
# Bygges fra repo-roten:
#   docker build -f Kode/src/Bronnoysund.Lookup.WebApi/Dockerfile -t bronnoysund-webapi Kode/

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Kopier kun csproj-filer først for å utnytte Docker layer-cache
COPY Directory.Build.props .
COPY src/Bronnoysund.Lookup.Domain/Bronnoysund.Lookup.Domain.csproj                 src/Bronnoysund.Lookup.Domain/
COPY src/Bronnoysund.Lookup.Application/Bronnoysund.Lookup.Application.csproj       src/Bronnoysund.Lookup.Application/
COPY src/Bronnoysund.Lookup.Infrastructure/Bronnoysund.Lookup.Infrastructure.csproj src/Bronnoysund.Lookup.Infrastructure/
COPY src/Bronnoysund.Lookup.WebApi/Bronnoysund.Lookup.WebApi.csproj                 src/Bronnoysund.Lookup.WebApi/

RUN dotnet restore src/Bronnoysund.Lookup.WebApi/Bronnoysund.Lookup.WebApi.csproj

# Kopier all kildekode og bygg
COPY src/Bronnoysund.Lookup.Domain/         src/Bronnoysund.Lookup.Domain/
COPY src/Bronnoysund.Lookup.Application/    src/Bronnoysund.Lookup.Application/
COPY src/Bronnoysund.Lookup.Infrastructure/ src/Bronnoysund.Lookup.Infrastructure/
COPY src/Bronnoysund.Lookup.WebApi/         src/Bronnoysund.Lookup.WebApi/

RUN dotnet publish src/Bronnoysund.Lookup.WebApi/Bronnoysund.Lookup.WebApi.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Slank runtime-image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

# Kjør ikke som root
RUN adduser -D -u 10001 appuser
WORKDIR /app
COPY --from=build --chown=appuser:appuser /app/publish .
USER appuser

# Container Apps bruker port 8080 som default
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_PRINT_TELEMETRY_MESSAGE=false

# Health-check (Container Apps probe-er denne)
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Bronnoysund.Lookup.WebApi.dll"]
