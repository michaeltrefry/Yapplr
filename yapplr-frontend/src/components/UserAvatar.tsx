import { User } from '@/types';
import Link from 'next/link';

interface UserAvatarProps {
  user: User;
  size?: 'sm' | 'md' | 'lg' | 'xl';
  clickable?: boolean;
  className?: string;
}

const sizeClasses = {
  sm: 'w-8 h-8 text-xs',
  md: 'w-10 h-10 text-sm',
  lg: 'w-16 h-16 text-lg',
  xl: 'w-20 h-20 text-xl',
};

export default function UserAvatar({ 
  user, 
  size = 'md', 
  clickable = true, 
  className = '' 
}: UserAvatarProps) {
  const sizeClass = sizeClasses[size];
  
  const getImageUrl = (fileName: string) => {
    if (!fileName) return '';
    return `${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5161'}/api/images/${fileName}`;
  };

  const avatarContent = user.profileImageFileName ? (
    <img
      src={getImageUrl(user.profileImageFileName) || ''}
      alt={`${user.username}'s profile`}
      className={`${sizeClass} rounded-full object-cover ${className}`}
      onError={(e) => {
        // Fallback to initials if image fails to load
        const target = e.target as HTMLImageElement;
        target.style.display = 'none';
        const fallback = target.nextElementSibling as HTMLElement;
        if (fallback) {
          fallback.style.display = 'flex';
        }
      }}
    />
  ) : null;

  const fallbackContent = (
    <div
      className={`${sizeClass} bg-blue-600 rounded-full flex items-center justify-center flex-shrink-0 ${className}`}
      style={{ display: user.profileImageFileName ? 'none' : 'flex' }}
    >
      <span className="text-white font-semibold">
        {user.username.charAt(0).toUpperCase()}
      </span>
    </div>
  );

  const avatar = (
    <div className="relative">
      {avatarContent}
      {fallbackContent}
    </div>
  );

  if (!clickable) {
    return avatar;
  }

  return (
    <Link href={`/profile/${user.username}`} className="cursor-pointer hover:opacity-80 transition-opacity">
      {avatar}
    </Link>
  );
}
