# Use the official .NET 9.0 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Install FFmpeg and other dependencies
RUN apt-get update && apt-get install -y \
    ffmpeg \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files and restore as distinct layers
COPY ["Yapplr.VideoProcessor/Yapplr.VideoProcessor.csproj", "Yapplr.VideoProcessor/"]
COPY ["Yapplr.Shared/Yapplr.Shared.csproj", "Yapplr.Shared/"]
RUN dotnet restore "Yapplr.VideoProcessor/Yapplr.VideoProcessor.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Yapplr.VideoProcessor"
RUN dotnet build "Yapplr.VideoProcessor.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Yapplr.VideoProcessor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app

# Create upload directories
RUN mkdir -p /app/uploads/videos \
    && mkdir -p /app/uploads/processed \
    && mkdir -p /app/uploads/thumbnails \
    && mkdir -p /app/uploads/temp

# Copy published application
COPY --from=publish /app/publish .

# Create a non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Yapplr.VideoProcessor.dll"]
