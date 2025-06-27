# Build stage
FROM node:18-alpine AS build

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY . .

# Set environment variable for production API
ENV NEXT_PUBLIC_API_URL=https://api.yapplr.com

# Build the Next.js app
RUN npm run build

# Production stage
FROM node:18-alpine AS production

WORKDIR /app

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
