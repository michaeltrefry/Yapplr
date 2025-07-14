# Build stage
FROM node:18-alpine AS build

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY . .

# Cache busting argument to force rebuild
ARG CACHE_BUST=1

# Define build arguments for environment variables
ARG NEXT_PUBLIC_API_URL=https://api.yapplr.com
ARG NEXT_PUBLIC_ENABLE_SIGNALR=true

# Set environment variables from build arguments
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_ENABLE_SIGNALR=$NEXT_PUBLIC_ENABLE_SIGNALR

# Use cache bust to ensure fresh build
RUN echo "Cache bust: $CACHE_BUST"

# Build the Next.js app
RUN npm run build

# Production stage
FROM node:18-alpine AS production

WORKDIR /app

# Define build arguments for environment variables (needed for runtime)
ARG NEXT_PUBLIC_API_URL=https://api.yapplr.com
ARG NEXT_PUBLIC_ENABLE_SIGNALR=true

# Set environment variables from build arguments (for runtime)
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_ENABLE_SIGNALR=$NEXT_PUBLIC_ENABLE_SIGNALR

# Copy built app
COPY --from=build /app/.next ./.next
COPY --from=build /app/public ./public
COPY --from=build /app/package*.json ./
COPY --from=build /app/next.config.ts ./

# Install only production dependencies
RUN npm ci --only=production

# Expose port 3000 (Next.js default)
EXPOSE 3000

# Start the Next.js app
CMD ["npm", "start"]
