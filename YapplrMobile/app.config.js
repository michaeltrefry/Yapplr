const IS_DEV = process.env.APP_VARIANT === 'development';
const IS_PREVIEW = process.env.APP_VARIANT === 'preview';

export default {
  expo: {
    name: IS_DEV ? 'YapplrMobile (Dev)' : IS_PREVIEW ? 'YapplrMobile (Preview)' : 'YapplrMobile',
    slug: 'YapplrMobile',
    version: '1.0.0',
    newArchEnabled: true,
    icon: './assets/icon.png',
    splash: {
      image: './assets/splash-icon.png',
      resizeMode: 'contain',
      backgroundColor: '#ffffff'
    },
    web: {
      favicon: './assets/favicon.png'
    },
    extra: {
      eas: {
        projectId: 'c03a3065-e620-4a2a-88bf-0094e04b4712'
      }
    },
    plugins: [
      'expo-web-browser',
      [
        'expo-image-picker',
        {
          photosPermission: 'The app accesses your photos to let you share them in posts.',
          cameraPermission: 'The app accesses your camera to let you take photos for posts.'
        }
      ],
      'expo-video'
    ],
    ios: {
      infoPlist: {
        NSPhotoLibraryUsageDescription: 'This app needs access to your photo library to let you share photos in posts.',
        NSCameraUsageDescription: 'This app needs access to your camera to let you take photos for posts.'
      },
      bundleIdentifier: IS_DEV 
        ? 'com.michaeljtrefry.YapplrMobile.dev' 
        : IS_PREVIEW 
        ? 'com.michaeljtrefry.YapplrMobile.preview'
        : 'com.michaeljtrefry.YapplrMobile.prod'
    },
    android: {
      permissions: [
        'android.permission.CAMERA',
        'android.permission.READ_EXTERNAL_STORAGE',
        'android.permission.WRITE_EXTERNAL_STORAGE',
        'android.permission.RECORD_AUDIO'
      ],
      package: IS_DEV 
        ? 'com.michaeljtrefry.YapplrMobile.dev' 
        : IS_PREVIEW 
        ? 'com.michaeljtrefry.YapplrMobile.preview'
        : 'com.michaeljtrefry.YapplrMobile'
    }
  }
};
