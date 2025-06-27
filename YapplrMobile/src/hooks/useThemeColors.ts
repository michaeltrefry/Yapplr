import { useTheme, lightTheme, darkTheme, Theme } from '../contexts/ThemeContext';

export function useThemeColors(): Theme {
  const { isDarkMode } = useTheme();
  return isDarkMode ? darkTheme : lightTheme;
}
