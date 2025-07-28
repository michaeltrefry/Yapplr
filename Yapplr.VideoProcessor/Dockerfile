# Use the official .NET 9.0 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Install HandBrake CLI and FFmpeg with comprehensive codec support
RUN apt-get update && apt-get install -y \
    # HandBrake CLI for video transcoding
    handbrake-cli \
    # FFmpeg for metadata extraction and thumbnail generation
    ffmpeg \
    curl \
    # Additional codec libraries for comprehensive video format support
    libx264-dev \
    libx265-dev \
    libvpx-dev \
    libmp3lame-dev \
    libopus-dev \
    libvorbis-dev \
    libtheora-dev \
    # Hardware acceleration libraries (if available)
    libva-dev \
    libvdpau-dev \
    # Additional multimedia libraries
    libavcodec-extra \
    libavformat-dev \
    libavutil-dev \
    libswscale-dev \
    libswresample-dev \
    # Cleanup
    && rm -rf /var/lib/apt/lists/* \
    && apt-get clean

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

# Create upload and logs directories
RUN mkdir -p /app/uploads/videos \
    && mkdir -p /app/uploads/processed \
    && mkdir -p /app/uploads/thumbnails \
    && mkdir -p /app/uploads/temp \
    && mkdir -p /app/logs \
    && chmod -R 777 /app/logs

# Copy published application
COPY --from=publish /app/publish .

# Verify HandBrake and FFmpeg installation
RUN HandBrakeCLI --version && \
    echo "HandBrake CLI verification completed successfully" && \
    ffmpeg -version && \
    ffmpeg -codecs | grep -E "(libx264|libx265|aac|libmp3lame)" && \
    echo "FFmpeg codec verification completed successfully"

# Create a non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Yapplr.VideoProcessor.dll"]
