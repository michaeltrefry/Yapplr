'use client';

import { use } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useRouter } from 'next/navigation';
import { userApi, postApi } from '@/lib/api';
import { useAuth } from '@/contexts/AuthContext';
import { formatDate } from '@/lib/utils';
import { Calendar } from 'lucide-react';
import PostCard from '@/components/PostCard';
import Sidebar from '@/components/Sidebar';

interface ProfilePageProps {
  params: Promise<{
    username: string;
  }>;
}

export default function ProfilePage({ params }: ProfilePageProps) {
  const { user: currentUser } = useAuth();
  const { username } = use(params);
  const router = useRouter();

  const { data: profile, isLoading: profileLoading } = useQuery({
    queryKey: ['profile', username],
    queryFn: () => userApi.getUserProfile(username),
  });

  const { data: posts, isLoading: postsLoading } = useQuery({
    queryKey: ['userPosts', profile?.id],
    queryFn: () => postApi.getUserPosts(profile!.id),
    enabled: !!profile?.id,
  });

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
              <h1 className="text-xl font-bold">{profile.username}</h1>
              <p className="text-sm text-gray-500">{profile.postCount} posts</p>
            </div>

            {/* Profile Info */}
            <div className="p-6 border-b border-gray-200">
              {/* Avatar and basic info */}
              <div className="flex items-start justify-between mb-4">
                <div className="w-20 h-20 bg-blue-600 rounded-full flex items-center justify-center">
                  <span className="text-white font-bold text-2xl">
                    {profile.username.charAt(0).toUpperCase()}
                  </span>
                </div>
                
                {isOwnProfile && (
                  <button
                    onClick={() => router.push('/profile/edit')}
                    className="px-4 py-2 border border-gray-300 rounded-full font-semibold hover:bg-gray-50 transition-colors"
                  >
                    Edit profile
                  </button>
                )}
              </div>

              {/* Name and username */}
              <div className="mb-3">
                <h2 className="text-xl font-bold text-gray-900">
                  {profile.username}
                </h2>
                <p className="text-gray-500">@{profile.username}</p>
              </div>

              {/* Bio */}
              {profile.bio && (
                <p className="text-gray-900 mb-3">{profile.bio}</p>
              )}

              {/* Pronouns */}
              {profile.pronouns && (
                <p className="text-gray-600 text-sm mb-3">
                  Pronouns: {profile.pronouns}
                </p>
              )}

              {/* Tagline */}
              {profile.tagline && (
                <p className="text-gray-600 italic mb-3">&ldquo;{profile.tagline}&rdquo;</p>
              )}

              {/* Join date */}
              <div className="flex items-center text-gray-500 text-sm">
                <Calendar className="w-4 h-4 mr-1" />
                <span>Joined {formatDate(profile.createdAt)}</span>
              </div>
            </div>

            {/* Posts */}
            <div>
              {postsLoading ? (
                <div className="p-8 text-center">
                  <div className="text-gray-500">Loading posts...</div>
                </div>
              ) : posts && posts.length > 0 ? (
                posts.map((post) => (
                  <PostCard key={post.id} post={post} showCommentsDefault={true} />
                ))
              ) : (
                <div className="p-8 text-center">
                  <div className="text-gray-500">
                    {isOwnProfile ? "You haven't posted anything yet" : "No posts yet"}
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
