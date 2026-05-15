# syntax=docker/dockerfile:1.7
# Backend .NET 8 — multi-stage para hom/prod (DAT §8.1, §8.2)
# Atende: RNF002 (uptime), RNF003/RNF004 (segurança), RT3, RAT03

ARG DOTNET_VERSION=8.0

# ---------- Stage 1: restore + build ----------
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copiar arquivos de projeto primeiro para melhor cache de restore
COPY ["CarWash.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/CarWash.Api/CarWash.Api.csproj", "src/CarWash.Api/"]
COPY ["src/CarWash.Application/CarWash.Application.csproj", "src/CarWash.Application/"]
COPY ["src/CarWash.Domain/CarWash.Domain.csproj", "src/CarWash.Domain/"]
COPY ["src/CarWash.Infrastructure/CarWash.Infrastructure.csproj", "src/CarWash.Infrastructure/"]

RUN dotnet restore "src/CarWash.Api/CarWash.Api.csproj"

# Copiar todo o código e publicar
COPY . .
WORKDIR /src/src/CarWash.Api
RUN dotnet publish "CarWash.Api.csproj" \
    -c ${BUILD_CONFIGURATION} \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false
RUN dotnet tool install --global dotnet-ef --version ${DOTNET_VERSION}.* \
    && ln -s /root/.dotnet/tools/dotnet-ef /usr/local/bin/dotnet-ef \
    && dotnet ef migrations bundle \
        --configuration ${BUILD_CONFIGURATION} \
        --project /src/src/CarWash.Infrastructure/CarWash.Infrastructure.csproj \
        --startup-project /src/src/CarWash.Api/CarWash.Api.csproj \
        --target-runtime linux-x64 \
        --output /app/efbundle

# ---------- Stage 2: migrator (one-shot) ----------
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS migrator
WORKDIR /src
COPY --from=build /src .
COPY --from=build /app/efbundle /src/src/CarWash.Api/efbundle
WORKDIR /src/src/CarWash.Api
ENTRYPOINT ["./efbundle"]

# ---------- Stage 3: runtime final ----------
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app

# Usuário não-root para segurança (RT3)
RUN groupadd --system --gid 1001 carwash \
    && useradd  --system --uid 1001 --gid carwash carwash

COPY --from=build --chown=carwash:carwash /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

# Healthcheck para depends_on:condition:service_healthy (RNF002)
HEALTHCHECK --interval=15s --timeout=5s --start-period=30s --retries=3 \
    CMD wget --quiet --spider http://localhost:8080/health || exit 1

USER carwash
ENTRYPOINT ["dotnet", "CarWash.Api.dll"]
