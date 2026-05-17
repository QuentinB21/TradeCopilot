FROM debian:bookworm-slim AS build
WORKDIR /src

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl \
    && curl -fsSL https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -o packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y --no-install-recommends dotnet-sdk-8.0 \
    && rm -rf /var/lib/apt/lists/*

COPY TradeCopilot.slnx ./
COPY src/TradeCopilot.Domain/TradeCopilot.Domain.csproj src/TradeCopilot.Domain/
COPY src/TradeCopilot.Application/TradeCopilot.Application.csproj src/TradeCopilot.Application/
COPY src/TradeCopilot.Infrastructure/TradeCopilot.Infrastructure.csproj src/TradeCopilot.Infrastructure/
COPY src/TradeCopilot.Api/TradeCopilot.Api.csproj src/TradeCopilot.Api/
RUN dotnet restore src/TradeCopilot.Api/TradeCopilot.Api.csproj
COPY . .
RUN dotnet publish src/TradeCopilot.Api/TradeCopilot.Api.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    /p:PublishSingleFile=true \
    /p:PublishTrimmed=false \
    -o /app/publish

FROM debian:bookworm-slim AS final
WORKDIR /app
EXPOSE 8080
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl libicu72 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENTRYPOINT ["./TradeCopilot.Api"]
