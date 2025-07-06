import React from 'react';
import { View } from 'react-native';
import { LinkPreview as LinkPreviewType } from '../types';
import LinkPreview from './LinkPreview';

interface LinkPreviewListProps {
  linkPreviews: LinkPreviewType[];
  style?: any;
}

export default function LinkPreviewList({ linkPreviews, style }: LinkPreviewListProps) {
  if (!linkPreviews || linkPreviews.length === 0) {
    return null;
  }

  return (
    <View style={style}>
      {linkPreviews.map((linkPreview) => (
        <LinkPreview
          key={linkPreview.id}
          linkPreview={linkPreview}
          style={{ marginBottom: 8 }}
        />
      ))}
    </View>
  );
}
