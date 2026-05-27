# SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

# Multi-stage build for ASP.NET Core 10.0 — final image er Alpine for minimal størrelse.
# Bygges fra repo-roten:
#   docker build -f Kode/src/Bronnoysund.WebApi/Dockerfile -t bronnoysund-webapi Kode/

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Kopier kun csproj-filer først for å utnytte Docker layer-cache
COPY Directory.Build.props .
COPY src/Bronnoysund.Domain/Bronnoysund.Domain.csproj                                       src/Bronnoysund.Domain/
COPY src/Bronnoysund.Application/Bronnoysund.Application.csproj                             src/Bronnoysund.Application/
COPY src/Bronnoysund.Infrastructure/Bronnoysund.Infrastructure.csproj                       src/Bronnoysund.Infrastructure/
COPY src/Bronnoysund.Infrastructure.Brreg.Generated/Bronnoysund.Infrastructure.Brreg.Generated.csproj src/Bronnoysund.Infrastructure.Brreg.Generated/
COPY src/Bronnoysund.Infrastructure.Persistence/Bronnoysund.Infrastructure.Persistence.csproj src/Bronnoysund.Infrastructure.Persistence/
COPY src/Bronnoysund.WebApi/Bronnoysund.WebApi.csproj                                       src/Bronnoysund.WebApi/

RUN dotnet restore src/Bronnoysund.WebApi/Bronnoysund.WebApi.csproj

# Kopier all kildekode og bygg
COPY src/Bronnoysund.Domain/                          src/Bronnoysund.Domain/
COPY src/Bronnoysund.Application/                     src/Bronnoysund.Application/
COPY src/Bronnoysund.Infrastructure/                  src/Bronnoysund.Infrastructure/
COPY src/Bronnoysund.Infrastructure.Brreg.Generated/  src/Bronnoysund.Infrastructure.Brreg.Generated/
COPY src/Bronnoysund.Infrastructure.Persistence/      src/Bronnoysund.Infrastructure.Persistence/
COPY src/Bronnoysund.WebApi/                          src/Bronnoysund.WebApi/

RUN dotnet publish src/Bronnoysund.WebApi/Bronnoysund.WebApi.csproj \
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

ENTRYPOINT ["dotnet", "Bronnoysund.WebApi.dll"]
