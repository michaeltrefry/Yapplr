// Simple event-based video coordination service
class VideoCoordinationService {
  private listeners: Set<(playingVideoId: string) => void> = new Set();
  private currentPlayingVideoId: string | null = null;

  // Subscribe to video play events
  subscribe(callback: (playingVideoId: string) => void): () => void {
    this.listeners.add(callback);
    
    // Return unsubscribe function
    return () => {
      this.listeners.delete(callback);
    };
  }

  // Notify that a video started playing
  notifyVideoPlaying(videoId: string) {
    console.log('🎥 VideoCoordination: Video started playing:', videoId);
    
    if (this.currentPlayingVideoId !== videoId) {
      this.currentPlayingVideoId = videoId;
      
      // Notify all listeners
      this.listeners.forEach(callback => {
        try {
          callback(videoId);
        } catch (error) {
          console.error('🎥 VideoCoordination: Error in listener callback:', error);
        }
      });
    }
  }

  // Notify that a video stopped playing
  notifyVideoStopped(videoId: string) {
    console.log('🎥 VideoCoordination: Video stopped playing:', videoId);
    
    if (this.currentPlayingVideoId === videoId) {
      this.currentPlayingVideoId = null;
    }
  }

  // Get currently playing video ID
  getCurrentPlayingVideoId(): string | null {
    return this.currentPlayingVideoId;
  }

  // Check if a specific video is currently playing
  isVideoPlaying(videoId: string): boolean {
    return this.currentPlayingVideoId === videoId;
  }
}

// Create a singleton instance
export const videoCoordinationService = new VideoCoordinationService();
