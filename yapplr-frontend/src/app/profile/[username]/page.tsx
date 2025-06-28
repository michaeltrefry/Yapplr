'use client';

import { use, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'next/navigation';
import { userApi, blockApi, messageApi } from '@/lib/api';
import { useAuth } from '@/contexts/AuthContext';
import { formatDate } from '@/lib/utils';
import { Calendar, Shield, ShieldOff, MessageCircle } from 'lucide-react';

import UserTimeline from '@/components/UserTimeline';
import Sidebar from '@/components/Sidebar';
import UserAvatar from '@/components/UserAvatar';

interface ProfilePageProps {
  params: Promise<{
    username: string;
  }>;
}

export default function ProfilePage({ params }: ProfilePageProps) {
  const { user: currentUser } = useAuth();
  const { username } = use(params);
  const router = useRouter();
  const queryClient = useQueryClient();
  const [showBlockConfirm, setShowBlockConfirm] = useState(false);

  const { data: profile, isLoading: profileLoading } = useQuery({
    queryKey: ['profile', username],
    queryFn: () => userApi.getUserProfile(username),
  });



  const { data: blockStatus } = useQuery({
    queryKey: ['blockStatus', profile?.id],
    queryFn: () => blockApi.getBlockStatus(profile!.id),
    enabled: !!profile?.id && !!currentUser && profile.id !== currentUser.id,
  });

  const followMutation = useMutation({
    mutationFn: (userId: number) => userApi.followUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['profile', username] });
    },
  });

  const unfollowMutation = useMutation({
    mutationFn: (userId: number) => userApi.unfollowUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['profile', username] });
    },
  });

  const blockMutation = useMutation({
    mutationFn: (userId: number) => blockApi.blockUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['blockStatus', profile?.id] });
      queryClient.invalidateQueries({ queryKey: ['profile', username] }); // Refresh profile to show unfollow
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['userTimeline', profile?.id] });
      queryClient.invalidateQueries({ queryKey: ['following'] }); // Refresh following list
      setShowBlockConfirm(false);
    },
  });

  const unblockMutation = useMutation({
    mutationFn: (userId: number) => blockApi.unblockUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['blockStatus', profile?.id] });
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['userTimeline', profile?.id] });
    },
  });

  const handleFollowToggle = () => {
    if (!profile) return;

    if (profile.isFollowedByCurrentUser) {
      unfollowMutation.mutate(profile.id);
    } else {
      followMutation.mutate(profile.id);
    }
  };

  const handleBlockToggle = () => {
    if (!profile) return;

    if (blockStatus?.isBlocked) {
      unblockMutation.mutate(profile.id);
    } else {
      setShowBlockConfirm(true);
    }
  };

  const handleConfirmBlock = () => {
    if (!profile) return;
    blockMutation.mutate(profile.id);
  };

  const messageMutation = useMutation({
    mutationFn: (userId: number) => messageApi.getOrCreateConversation(userId),
    onSuccess: (conversation) => {
      router.push(`/messages/${conversation.id}`);
    },
    onError: () => {
      alert('Unable to start conversation. User may have blocked you.');
    },
  });

  const handleMessage = () => {
    if (!profile) return;
    messageMutation.mutate(profile.id);
  };

  if (profileLoading) {
    return (
      <div className="min-h-screen bg-gray-50">
        <div className="max-w-6xl mx-auto flex">
          <div className="w-16 lg:w-64 fixed h-full z-10">
            <Sidebar />
          </div>
          <div className="flex-1 ml-16 lg:ml-64">
            <div className="max-w-2xl mx-auto lg:border-x border-gray-200 min-h-screen bg-white">
              <div className="p-8 text-center">
                <div className="text-gray-500">Loading profile...</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="min-h-screen bg-gray-50">
        <div className="max-w-6xl mx-auto flex">
          <div className="w-16 lg:w-64 fixed h-full z-10">
            <Sidebar />
          </div>
          <div className="flex-1 ml-16 lg:ml-64">
            <div className="max-w-2xl mx-auto lg:border-x border-gray-200 min-h-screen bg-white">
              <div className="p-8 text-center">
                <div className="text-red-500">User not found</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  const isOwnProfile = currentUser?.username === username;

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-6xl mx-auto flex">
        {/* Sidebar */}
        <div className="w-16 lg:w-64 fixed h-full z-10">
          <Sidebar />
        </div>

        {/* Main Content */}
        <div className="flex-1 ml-16 lg:ml-64">
          <div className="max-w-2xl mx-auto lg:border-x border-gray-200 min-h-screen bg-white">
            {/* Header */}
            <div className="sticky top-0 bg-white/80 backdrop-blur-md border-b border-gray-200 p-4">
              <h1 className="text-xl font-bold text-gray-900">{profile.username}</h1>
              <p className="text-sm text-gray-500">{profile.postCount} posts</p>
            </div>

            {/* Profile Info */}
            <div className="p-6 border-b border-gray-200">
              {/* Avatar and basic info */}
              <div className="flex items-start justify-between mb-4">
                <UserAvatar
                  user={{
                    id: profile.id,
                    email: '',
                    username: profile.username,
                    bio: profile.bio,
                    birthday: profile.birthday,
                    pronouns: profile.pronouns,
                    tagline: profile.tagline,
                    profileImageFileName: profile.profileImageFileName,
                    createdAt: profile.createdAt
                  }}
                  size="xl"
                  clickable={false}
                />
                
                {isOwnProfile ? (
                  <button
                    onClick={() => router.push('/profile/edit')}
                    className="px-4 py-2 border border-gray-300 rounded-full font-semibold hover:bg-gray-50 transition-colors text-gray-900"
                  >
                    Edit profile
                  </button>
                ) : (
                  <div className="flex space-x-2">
                    <button
                      onClick={handleFollowToggle}
                      disabled={followMutation.isPending || unfollowMutation.isPending}
                      className={`px-4 py-2 rounded-full font-semibold transition-colors disabled:opacity-50 ${
                        profile.isFollowedByCurrentUser
                          ? 'bg-gray-200 text-gray-800 hover:bg-gray-300'
                          : 'bg-blue-500 text-white hover:bg-blue-600'
                      }`}
                    >
                      {followMutation.isPending || unfollowMutation.isPending
                        ? '...'
                        : profile.isFollowedByCurrentUser
                        ? 'Unfollow'
                        : 'Follow'
                      }
                    </button>
                    {!blockStatus?.isBlocked && (
                      <button
                        onClick={handleMessage}
                        disabled={messageMutation.isPending}
                        className="px-3 py-2 rounded-full font-semibold transition-colors disabled:opacity-50 bg-green-100 text-green-800 hover:bg-green-200 border border-green-300"
                        title="Send message"
                      >
                        {messageMutation.isPending ? (
                          '...'
                        ) : (
                          <MessageCircle className="w-4 h-4" />
                        )}
                      </button>
                    )}
                    <button
                      onClick={handleBlockToggle}
                      disabled={blockMutation.isPending || unblockMutation.isPending}
                      className={`px-3 py-2 rounded-full font-semibold transition-colors disabled:opacity-50 ${
                        blockStatus?.isBlocked
                          ? 'bg-green-100 text-green-800 hover:bg-green-200 border border-green-300'
                          : 'bg-red-100 text-red-800 hover:bg-red-200 border border-red-300'
                      }`}
                      title={blockStatus?.isBlocked ? 'Unblock user' : 'Block user'}
                    >
                      {blockMutation.isPending || unblockMutation.isPending ? (
                        '...'
                      ) : blockStatus?.isBlocked ? (
                        <ShieldOff className="w-4 h-4" />
                      ) : (
                        <Shield className="w-4 h-4" />
                      )}
                    </button>
                  </div>
                )}
              </div>

              {/* Name and username */}
              <div className="mb-3">
                <h2 className="text-xl font-bold text-gray-900">
                  {profile.username}
                  {profile.pronouns && (
                    <span className="text-lg text-gray-500 font-normal"> ({profile.pronouns})</span>
                  )}
                </h2>
                <p className="text-gray-500">@{profile.username}</p>
              </div>

              {/* Bio */}
              {profile.bio && (
                <p className="text-gray-900 mb-3">{profile.bio}</p>
              )}

              {/* Tagline */}
              {profile.tagline && (
                <p className="text-gray-600 italic mb-3">&ldquo;{profile.tagline}&rdquo;</p>
              )}

              {/* Birthday */}
              {profile.birthday && (
                <p className="text-gray-600 text-sm mb-3">
                  🎂 Born {formatDate(profile.birthday)}
                </p>
              )}

              {/* Follow counts */}
              <div className="flex items-center gap-4 text-sm mb-3">
                <span className="text-gray-600">
                  <span className="font-semibold text-gray-900">{profile.followingCount}</span> Following
                </span>
                <span className="text-gray-600">
                  <span className="font-semibold text-gray-900">{profile.followerCount}</span> Followers
                </span>
              </div>

              {/* Join date */}
              <div className="flex items-center text-gray-500 text-sm">
                <Calendar className="w-4 h-4 mr-1" />
                <span>Joined {formatDate(profile.createdAt)}</span>
              </div>
            </div>

            {/* Posts and Reposts */}
            <UserTimeline userId={profile.id} isOwnProfile={isOwnProfile} />
          </div>
        </div>
      </div>

      {/* Block Confirmation Modal */}
      {showBlockConfirm && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 max-w-md w-full mx-4">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">Block @{profile?.username}?</h3>
            <p className="text-gray-600 mb-6">
              They won&apos;t be able to see your posts or interact with you. You won&apos;t see their content either.
              {profile?.isFollowedByCurrentUser && " You will also unfollow each other."}
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setShowBlockConfirm(false)}
                className="px-4 py-2 text-gray-600 hover:text-gray-800 transition-colors"
                disabled={blockMutation.isPending}
              >
                Cancel
              </button>
              <button
                onClick={handleConfirmBlock}
                disabled={blockMutation.isPending}
                className="px-4 py-2 bg-red-500 text-white rounded hover:bg-red-600 transition-colors disabled:opacity-50"
              >
                {blockMutation.isPending ? 'Blocking...' : 'Block'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
