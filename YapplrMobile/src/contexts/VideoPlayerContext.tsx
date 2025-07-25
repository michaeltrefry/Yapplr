import React, { createContext, useContext, useRef, ReactNode } from 'react';
import { VideoPlayerRef } from '../components/VideoPlayer';

interface VideoPlayerContextType {
  registerPlayer: (id: string, playerRef: VideoPlayerRef) => void;
  unregisterPlayer: (id: string) => void;
  pauseAllExcept: (exceptId?: string) => void;
  pauseAll: () => void;
  playVideo: (id: string) => void;
}

const VideoPlayerContext = createContext<VideoPlayerContextType | undefined>(undefined);

interface VideoPlayerProviderProps {
  children: ReactNode;
}

export function VideoPlayerProvider({ children }: VideoPlayerProviderProps) {
  const videoPlayerRefs = useRef<{ [key: string]: VideoPlayerRef }>({});

  console.log('🎥 VideoPlayerProvider: Provider initialized');

  const registerPlayer = (id: string, playerRef: VideoPlayerRef) => {
    console.log('🎥 VideoPlayerContext: Registering player:', id);
    videoPlayerRefs.current[id] = playerRef;
  };

  const unregisterPlayer = (id: string) => {
    console.log('🎥 VideoPlayerContext: Unregistering player:', id);
    delete videoPlayerRefs.current[id];
  };

  const pauseAllExcept = (exceptId?: string) => {
    console.log('🎥 VideoPlayerContext: Pausing all players except:', exceptId);
    Object.entries(videoPlayerRefs.current).forEach(([id, playerRef]) => {
      if (id !== exceptId && playerRef && playerRef.isPlaying()) {
        console.log('🎥 VideoPlayerContext: Pausing player:', id);
        playerRef.pause();
      }
    });
  };

  const pauseAll = () => {
    console.log('🎥 VideoPlayerContext: Pausing all players');
    Object.entries(videoPlayerRefs.current).forEach(([id, playerRef]) => {
      if (playerRef && playerRef.isPlaying()) {
        console.log('🎥 VideoPlayerContext: Pausing player:', id);
        playerRef.pause();
      }
    });
  };

  const playVideo = (id: string) => {
    console.log('🎥 VideoPlayerContext: Playing video:', id);
    
    // First pause all other videos
    pauseAllExcept(id);
    
    // Then play the requested video
    const playerRef = videoPlayerRefs.current[id];
    if (playerRef) {
      console.log('🎥 VideoPlayerContext: Starting playback for:', id);
      playerRef.play();
    } else {
      console.warn('🎥 VideoPlayerContext: Player not found for id:', id);
    }
  };

  const value: VideoPlayerContextType = {
    registerPlayer,
    unregisterPlayer,
    pauseAllExcept,
    pauseAll,
    playVideo,
  };

  return (
    <VideoPlayerContext.Provider value={value}>
      {children}
    </VideoPlayerContext.Provider>
  );
}

export function useVideoPlayerContext() {
  const context = useContext(VideoPlayerContext);
  if (context === undefined) {
    throw new Error('useVideoPlayerContext must be used within a VideoPlayerProvider');
  }
  return context;
}

// Optional hook that doesn't throw an error
export function useVideoPlayerContextOptional() {
  const context = useContext(VideoPlayerContext);
  return context; // Returns undefined if not within provider
}
