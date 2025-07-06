# Use the official .NET runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Install FFmpeg
RUN apt-get update && \
    apt-get install -y ffmpeg && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Verify FFmpeg installation
RUN ffmpeg -version && ffprobe -version

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["Yapplr.VideoProcessor/Yapplr.VideoProcessor.csproj", "Yapplr.VideoProcessor/"]
COPY ["Yapplr.Api/Yapplr.Api.csproj", "Yapplr.Api/"]

# Restore dependencies
RUN dotnet restore "Yapplr.VideoProcessor/Yapplr.VideoProcessor.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/Yapplr.VideoProcessor"
RUN dotnet build "Yapplr.VideoProcessor.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Yapplr.VideoProcessor.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app

# Copy the published application
COPY --from=publish /app/publish .

# Create directories for video processing
RUN mkdir -p /app/uploads/videos && \
    mkdir -p /app/uploads/thumbnails && \
    mkdir -p /app/uploads/temp && \
    chmod 755 /app/uploads/videos && \
    chmod 755 /app/uploads/thumbnails && \
    chmod 755 /app/uploads/temp

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV VideoProcessing__InputPath=/app/uploads/videos
ENV VideoProcessing__OutputPath=/app/uploads/videos
ENV VideoProcessing__ThumbnailPath=/app/uploads/thumbnails
ENV VideoProcessing__TempPath=/app/uploads/temp

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD ffmpeg -version > /dev/null 2>&1 || exit 1

# Set the entry point
ENTRYPOINT ["dotnet", "Yapplr.VideoProcessor.dll"]
