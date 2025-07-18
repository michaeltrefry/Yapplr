# Use the official .NET 9.0 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["Yapplr.Api/Yapplr.Api.csproj", "Yapplr.Api/"]
COPY ["Yapplr.Shared/Yapplr.Shared.csproj", "Yapplr.Shared/"]
RUN dotnet restore "Yapplr.Api/Yapplr.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Yapplr.Api"
RUN dotnet build "Yapplr.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Yapplr.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app

# Create uploads and logs directories with proper permissions
RUN mkdir -p /uploads /app/uploads/images /app/uploads/videos /app/uploads/processed /app/uploads/thumbnails /app/uploads/temp /app/logs \
    && chmod -R 777 /uploads /app/uploads /app/logs

# Copy published application
COPY --from=publish /app/publish .

# Set environment variables
# Note: ASPNETCORE_ENVIRONMENT should be set by docker-compose or deployment config
ENV ASPNETCORE_URLS=http://+:8080

# Create a non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Yapplr.Api.dll"]
