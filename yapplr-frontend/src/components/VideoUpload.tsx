'use client';

import { useState, useRef } from 'react';
import { useMutation } from '@tanstack/react-query';
import { videoApi } from '@/lib/api';
import { Video, X, Upload, AlertCircle, CheckCircle, Clock } from 'lucide-react';

interface VideoUploadProps {
  onVideoUploaded: (videoData: {
    fileName: string;
    videoUrl: string;
    jobId: number;
    sizeBytes: number;
  }) => void;
  onRemove: () => void;
  disabled?: boolean;
}

interface VideoUploadState {
  file: File | null;
  preview: string | null;
  fileName: string | null;
  jobId: number | null;
  uploadProgress: number;
  processingStatus: 'idle' | 'uploading' | 'processing' | 'completed' | 'failed';
  errorMessage: string | null;
}

export default function VideoUpload({ onVideoUploaded, onRemove, disabled }: VideoUploadProps) {
  const [state, setState] = useState<VideoUploadState>({
    file: null,
    preview: null,
    fileName: null,
    jobId: null,
    uploadProgress: 0,
    processingStatus: 'idle',
    errorMessage: null,
  });

  const fileInputRef = useRef<HTMLInputElement>(null);
  const videoRef = useRef<HTMLVideoElement>(null);

  const uploadMutation = useMutation({
    mutationFn: (file: File) => videoApi.uploadVideo(file, (progress) => {
      setState(prev => ({ ...prev, uploadProgress: progress }));
    }),
    onSuccess: (data) => {
      setState(prev => ({
        ...prev,
        fileName: data.fileName,
        jobId: data.jobId,
        processingStatus: 'processing',
        uploadProgress: 100,
      }));
      
      onVideoUploaded({
        fileName: data.fileName,
        videoUrl: data.videoUrl,
        jobId: data.jobId,
        sizeBytes: data.sizeBytes,
      });

      // Start polling for processing status
      pollProcessingStatus(data.jobId);
    },
    onError: (error: any) => {
      setState(prev => ({
        ...prev,
        processingStatus: 'failed',
        errorMessage: error.response?.data?.message || 'Upload failed',
      }));
    },
  });

  const pollProcessingStatus = async (jobId: number) => {
    const maxAttempts = 60; // 5 minutes with 5-second intervals
    let attempts = 0;

    const poll = async () => {
      try {
        const status = await videoApi.getProcessingStatus(jobId);
        
        setState(prev => ({ ...prev, processingStatus: status.status.toLowerCase() as any }));

        if (status.status === 'Completed') {
          setState(prev => ({ ...prev, processingStatus: 'completed' }));
          return;
        } else if (status.status === 'Failed') {
          setState(prev => ({
            ...prev,
            processingStatus: 'failed',
            errorMessage: status.errorMessage || 'Processing failed',
          }));
          return;
        }

        attempts++;
        if (attempts < maxAttempts) {
          setTimeout(poll, 5000); // Poll every 5 seconds
        } else {
          setState(prev => ({
            ...prev,
            processingStatus: 'failed',
            errorMessage: 'Processing timeout',
          }));
        }
      } catch (error) {
        console.error('Error polling processing status:', error);
        setState(prev => ({
          ...prev,
          processingStatus: 'failed',
          errorMessage: 'Failed to check processing status',
        }));
      }
    };

    poll();
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Validate file type
    const allowedTypes = ['video/mp4', 'video/webm', 'video/quicktime', 'video/x-msvideo', 'video/x-matroska'];
    if (!allowedTypes.includes(file.type)) {
      alert('Please select a valid video file (MP4, WebM, MOV, AVI, MKV)');
      return;
    }

    // Validate file size (100MB)
    if (file.size > 100 * 1024 * 1024) {
      alert('File size must be less than 100MB');
      return;
    }

    // Create preview
    const videoUrl = URL.createObjectURL(file);
    setState(prev => ({
      ...prev,
      file,
      preview: videoUrl,
      processingStatus: 'uploading',
      errorMessage: null,
      uploadProgress: 0,
    }));

    // Start upload
    uploadMutation.mutate(file);
  };

  const handleRemove = () => {
    if (state.preview) {
      URL.revokeObjectURL(state.preview);
    }
    setState({
      file: null,
      preview: null,
      fileName: null,
      jobId: null,
      uploadProgress: 0,
      processingStatus: 'idle',
      errorMessage: null,
    });
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
    onRemove();
  };

  const getStatusIcon = () => {
    switch (state.processingStatus) {
      case 'uploading':
        return <Upload className="w-4 h-4 text-blue-500 animate-pulse" />;
      case 'processing':
        return <Clock className="w-4 h-4 text-yellow-500 animate-spin" />;
      case 'completed':
        return <CheckCircle className="w-4 h-4 text-green-500" />;
      case 'failed':
        return <AlertCircle className="w-4 h-4 text-red-500" />;
      default:
        return null;
    }
  };

  const getStatusText = () => {
    switch (state.processingStatus) {
      case 'uploading':
        return `Uploading... ${state.uploadProgress}%`;
      case 'processing':
        return 'Processing video...';
      case 'completed':
        return 'Video ready!';
      case 'failed':
        return state.errorMessage || 'Processing failed';
      default:
        return '';
    }
  };

  if (state.preview) {
    return (
      <div className="relative">
        <div className="relative bg-gray-100 rounded-lg overflow-hidden">
          <video
            ref={videoRef}
            src={state.preview}
            className="w-full h-48 object-cover"
            controls={state.processingStatus === 'completed'}
            muted
          />
          
          {/* Status overlay */}
          {state.processingStatus !== 'idle' && (
            <div className="absolute inset-0 bg-black bg-opacity-50 flex items-center justify-center">
              <div className="bg-white rounded-lg p-4 flex items-center space-x-2">
                {getStatusIcon()}
                <span className="text-sm font-medium">{getStatusText()}</span>
              </div>
            </div>
          )}

          {/* Progress bar for uploading */}
          {state.processingStatus === 'uploading' && (
            <div className="absolute bottom-0 left-0 right-0 bg-gray-200 h-1">
              <div 
                className="bg-blue-500 h-1 transition-all duration-300"
                style={{ width: `${state.uploadProgress}%` }}
              />
            </div>
          )}

          {/* Remove button */}
          <button
            type="button"
            onClick={handleRemove}
            className="absolute top-2 right-2 bg-red-500 text-white rounded-full p-1 hover:bg-red-600 transition-colors"
            disabled={state.processingStatus === 'uploading'}
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Error message */}
        {state.processingStatus === 'failed' && state.errorMessage && (
          <div className="mt-2 text-sm text-red-600 bg-red-50 p-2 rounded">
            {state.errorMessage}
          </div>
        )}
      </div>
    );
  }

  return (
    <div>
      <input
        ref={fileInputRef}
        type="file"
        accept="video/mp4,video/webm,video/quicktime,video/x-msvideo,video/x-matroska"
        onChange={handleFileSelect}
        className="hidden"
        disabled={disabled}
      />
      <button
        type="button"
        onClick={() => fileInputRef.current?.click()}
        disabled={disabled}
        className="flex items-center space-x-2 px-3 py-2 text-blue-600 hover:bg-blue-50 rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
      >
        <Video className="w-5 h-5" />
        <span>Add Video</span>
      </button>
    </div>
  );
}
