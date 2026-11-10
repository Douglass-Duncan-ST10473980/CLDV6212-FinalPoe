# Author: Tahir Ismail - ST10471483
# CoffeeNChill API Gateway - multi-stage Docker build.
# Build from the repository root:
#   docker build -f CoffeeNChill.ApiGateway/Dockerfile -t tahirismail/coffeenchill-gateway:v3.0 .

# Stage 1: lightweight ASP.NET Core runtime image that the final container runs on.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Stage 2: full .NET SDK, used only to restore, build and publish the project.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy the project file on its own first, so Docker can cache the restored NuGet
# packages and skip this step when only the source code changes.
COPY ["global.json", "./"]
COPY ["CoffeeNChill.ApiGateway/CoffeeNChill.ApiGateway.csproj", "CoffeeNChill.ApiGateway/"]
RUN dotnet restore "CoffeeNChill.ApiGateway/CoffeeNChill.ApiGateway.csproj"

# Copy the rest of the gateway source and publish a release build.
COPY ["CoffeeNChill.ApiGateway/", "CoffeeNChill.ApiGateway/"]
WORKDIR /src/CoffeeNChill.ApiGateway
RUN dotnet publish "CoffeeNChill.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

# Stage 3: copy only the published output into the runtime image.
# The SDK and source code are left behind, keeping the image small.
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Listen on port 8080 inside the container (published as 5000 by docker-compose).
ENV ASPNETCORE_HTTP_PORTS=8080

# Run as the image's built-in non-root "app" user for better security.
USER app

ENTRYPOINT ["dotnet", "CoffeeNChill.ApiGateway.dll"]