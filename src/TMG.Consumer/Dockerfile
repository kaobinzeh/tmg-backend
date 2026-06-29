# syntax=docker/dockerfile:1
# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release


WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["src/TMG.Consumer/TMG.Consumer.csproj", "src/TMG.Consumer/"]
COPY ["src/TMG.Contracts/TMG.Contracts.csproj", "src/TMG.Contracts/"]
COPY ["src/TMG.Domain/TMG.Domain.csproj", "src/TMG.Domain/"]
COPY ["src/TMG.Infrastructure/TMG.Infrastructure.csproj", "src/TMG.Infrastructure/"]
COPY ["src/TMG.Application/TMG.Application.csproj", "src/TMG.Application/"]
RUN --mount=type=secret,id=GITHUB_USERNAME \
    --mount=type=secret,id=GITHUB_PAT \
    GITHUB_USERNAME=$(cat /run/secrets/GITHUB_USERNAME) \
    GITHUB_PAT=$(cat /run/secrets/GITHUB_PAT) \
    dotnet restore "./src/TMG.Consumer/TMG.Consumer.csproj"
COPY . .
WORKDIR "/src/src/TMG.Consumer"
RUN dotnet build "./TMG.Consumer.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TMG.Consumer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM pyroscope/pyroscope-dotnet:0.14.2-glibc AS pyroscope

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=pyroscope /Pyroscope.Profiler.Native.so /dotnet/Pyroscope.Profiler.Native.so
COPY --from=pyroscope /Pyroscope.Linux.ApiWrapper.x64.so /dotnet/Pyroscope.Linux.ApiWrapper.x64.so
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TMG.Consumer.dll"]
