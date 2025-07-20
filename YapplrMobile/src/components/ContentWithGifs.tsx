import React from 'react';
import { View, Text, Image, TouchableOpacity, StyleSheet, Linking } from 'react-native';
import { parseContentParts, type ContentPart } from '../utils/gifUtils';

interface ContentWithGifsProps {
  content: string;
  style?: any;
  textStyle?: any;
  maxGifWidth?: number;
  onMentionPress?: (username: string) => void;
  onHashtagPress?: (hashtag: string) => void;
  onLinkPress?: (url: string) => void;
}

export default function ContentWithGifs({
  content,
  style,
  textStyle,
  maxGifWidth = 200,
  onMentionPress,
  onHashtagPress,
  onLinkPress,
}: ContentWithGifsProps) {
  const parts = parseContentParts(content);

  const renderTextWithHighlights = (text: string) => {
    // Split text by mentions, hashtags, and links
    const regex = /(@\w+)|(#\w+)|(https?:\/\/[^\s]+)/g;
    const parts = [];
    let lastIndex = 0;
    let match;

    while ((match = regex.exec(text)) !== null) {
      // Add text before the match
      if (match.index > lastIndex) {
        parts.push(
          <Text key={`text-${lastIndex}`} style={textStyle}>
            {text.slice(lastIndex, match.index)}
          </Text>
        );
      }

      // Add the highlighted match
      const matchText = match[0];
      if (matchText.startsWith('@')) {
        // Mention
        parts.push(
          <TouchableOpacity
            key={`mention-${match.index}`}
            onPress={() => onMentionPress?.(matchText.slice(1))}
          >
            <Text style={[textStyle, styles.mention]}>{matchText}</Text>
          </TouchableOpacity>
        );
      } else if (matchText.startsWith('#')) {
        // Hashtag
        parts.push(
          <TouchableOpacity
            key={`hashtag-${match.index}`}
            onPress={() => onHashtagPress?.(matchText.slice(1))}
          >
            <Text style={[textStyle, styles.hashtag]}>{matchText}</Text>
          </TouchableOpacity>
        );
      } else if (matchText.startsWith('http')) {
        // Link
        parts.push(
          <TouchableOpacity
            key={`link-${match.index}`}
            onPress={() => onLinkPress?.(matchText) || Linking.openURL(matchText)}
          >
            <Text style={[textStyle, styles.link]}>{matchText}</Text>
          </TouchableOpacity>
        );
      }

      lastIndex = match.index + match[0].length;
    }

    // Add remaining text
    if (lastIndex < text.length) {
      parts.push(
        <Text key={`text-${lastIndex}`} style={textStyle}>
          {text.slice(lastIndex)}
        </Text>
      );
    }

    return parts.length > 0 ? parts : <Text style={textStyle}>{text}</Text>;
  };

  return (
    <View style={style}>
      {parts.map((part, index) => {
        if (part.type === 'text') {
          return (
            <View key={index} style={styles.textContainer}>
              {renderTextWithHighlights(part.content)}
            </View>
          );
        } else if (part.type === 'gif' && part.gif) {
          const { gif } = part;
          // Calculate display dimensions while maintaining aspect ratio
          const aspectRatio = gif.height / gif.width;
          const displayWidth = Math.min(gif.width, maxGifWidth);
          const displayHeight = displayWidth * aspectRatio;

          return (
            <View key={index} style={styles.gifContainer}>
              <TouchableOpacity
                onPress={() => {
                  // Open full-size GIF in browser
                  Linking.openURL(gif.url);
                }}
              >
                <View style={styles.gifWrapper}>
                  <Image
                    source={{ uri: gif.previewUrl }}
                    style={[
                      styles.gifImage,
                      {
                        width: displayWidth,
                        height: displayHeight,
                      },
                    ]}
                    resizeMode="cover"
                  />
                  <View style={styles.gifBadge}>
                    <Text style={styles.gifBadgeText}>GIF</Text>
                  </View>
                </View>
              </TouchableOpacity>
            </View>
          );
        }
        return null;
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  textContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
  },
  mention: {
    color: '#007AFF',
    fontWeight: '600',
  },
  hashtag: {
    color: '#007AFF',
    fontWeight: '600',
  },
  link: {
    color: '#007AFF',
    textDecorationLine: 'underline',
  },
  gifContainer: {
    marginVertical: 8,
  },
  gifWrapper: {
    position: 'relative',
    alignSelf: 'flex-start',
  },
  gifImage: {
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#e0e0e0',
  },
  gifBadge: {
    position: 'absolute',
    bottom: 4,
    left: 4,
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
    paddingHorizontal: 4,
    paddingVertical: 2,
    borderRadius: 4,
  },
  gifBadgeText: {
    color: '#fff',
    fontSize: 10,
    fontWeight: '600',
  },
});
