#!/bin/bash

# Staging SSL Certificate Setup Script
# This script sets up SSL certificates for staging domains using Certbot

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Helper functions
log_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

log_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

log_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

log_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Configuration
STAGING_API_DOMAIN="stg-api.yapplr.com"
STAGING_FRONTEND_DOMAIN="stg.yapplr.com"
EMAIL="your-email@example.com"  # Change this to your email
WEBROOT_PATH="/var/www/html"

echo "🔒 Setting up SSL certificates for staging"
echo "=========================================="

# Check if running as root
if [ "$EUID" -ne 0 ]; then
    log_error "This script must be run as root (use sudo)"
    exit 1
fi

# Install certbot if not already installed
install_certbot() {
    log_info "Installing Certbot..."
    
    if command -v certbot &> /dev/null; then
        log_success "Certbot is already installed"
        return 0
    fi
    
    # Install certbot based on OS
    if [ -f /etc/debian_version ]; then
        # Debian/Ubuntu
        apt update
        apt install -y certbot python3-certbot-nginx
    elif [ -f /etc/redhat-release ]; then
        # CentOS/RHEL/Fedora
        yum install -y certbot python3-certbot-nginx
    else
        log_error "Unsupported operating system"
        exit 1
    fi
    
    log_success "Certbot installed successfully"
}

# Create webroot directory
setup_webroot() {
    log_info "Setting up webroot directory..."
    
    mkdir -p "$WEBROOT_PATH"
    chown -R www-data:www-data "$WEBROOT_PATH" 2>/dev/null || chown -R nginx:nginx "$WEBROOT_PATH" 2>/dev/null || true
    chmod -R 755 "$WEBROOT_PATH"
    
    log_success "Webroot directory created at $WEBROOT_PATH"
}

# Generate certificates
generate_certificates() {
    log_info "Generating SSL certificates..."
    
    # Stop nginx temporarily to avoid conflicts
    systemctl stop nginx 2>/dev/null || docker-compose -f docker-compose.staging.yml stop nginx 2>/dev/null || true
    
    # Generate certificates using standalone mode (easier for staging)
    log_info "Generating certificate for $STAGING_API_DOMAIN..."
    certbot certonly \
        --standalone \
        --email "$EMAIL" \
        --agree-tos \
        --no-eff-email \
        --staging \
        -d "$STAGING_API_DOMAIN"
    
    log_info "Generating certificate for $STAGING_FRONTEND_DOMAIN..."
    certbot certonly \
        --standalone \
        --email "$EMAIL" \
        --agree-tos \
        --no-eff-email \
        --staging \
        -d "$STAGING_FRONTEND_DOMAIN"
    
    log_success "SSL certificates generated successfully"
}

# Copy certificates to nginx directory
setup_nginx_certificates() {
    log_info "Setting up certificates for nginx..."
    
    # Create nginx ssl directory
    mkdir -p ./nginx/ssl
    
    # Copy certificates
    cp "/etc/letsencrypt/live/$STAGING_API_DOMAIN/fullchain.pem" "./nginx/ssl/${STAGING_API_DOMAIN}.crt"
    cp "/etc/letsencrypt/live/$STAGING_API_DOMAIN/privkey.pem" "./nginx/ssl/${STAGING_API_DOMAIN}.key"
    
    cp "/etc/letsencrypt/live/$STAGING_FRONTEND_DOMAIN/fullchain.pem" "./nginx/ssl/${STAGING_FRONTEND_DOMAIN}.crt"
    cp "/etc/letsencrypt/live/$STAGING_FRONTEND_DOMAIN/privkey.pem" "./nginx/ssl/${STAGING_FRONTEND_DOMAIN}.key"
    
    # Set proper permissions
    chmod 644 ./nginx/ssl/*.crt
    chmod 600 ./nginx/ssl/*.key
    
    log_success "Certificates copied to nginx directory"
}

# Create nginx configuration for staging with SSL
create_nginx_config() {
    log_info "Creating nginx configuration with SSL..."
    
    cat > ./nginx/nginx-staging-ssl.conf << 'EOF'
events {
    worker_connections 1024;
}

http {
    upstream api {
        server yapplr-api:8080;
    }
    
    upstream frontend {
        server yapplr-frontend:3000;
    }
    
    # Redirect HTTP to HTTPS
    server {
        listen 80;
        server_name stg-api.yapplr.com stg.yapplr.com;
        return 301 https://$server_name$request_uri;
    }
    
    # API Server (HTTPS)
    server {
        listen 443 ssl;
        server_name stg-api.yapplr.com;
        
        ssl_certificate /etc/nginx/ssl/stg-api.yapplr.com.crt;
        ssl_certificate_key /etc/nginx/ssl/stg-api.yapplr.com.key;
        
        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers HIGH:!aNULL:!MD5;
        
        location / {
            proxy_pass http://api;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }
        
        # Serve uploaded files
        location /uploads/ {
            alias /var/www/uploads/;
            expires 1y;
            add_header Cache-Control "public, immutable";
        }
    }
    
    # Frontend Server (HTTPS)
    server {
        listen 443 ssl;
        server_name stg.yapplr.com;
        
        ssl_certificate /etc/nginx/ssl/stg.yapplr.com.crt;
        ssl_certificate_key /etc/nginx/ssl/stg.yapplr.com.key;
        
        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers HIGH:!aNULL:!MD5;
        
        location / {
            proxy_pass http://frontend;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }
    }
}
EOF
    
    log_success "Nginx SSL configuration created"
}

# Setup certificate renewal
setup_renewal() {
    log_info "Setting up automatic certificate renewal..."
    
    # Create renewal script
    cat > /etc/cron.d/certbot-staging << 'EOF'
# Renew staging certificates twice daily
0 */12 * * * root certbot renew --staging --quiet --post-hook "systemctl reload nginx || docker-compose -f /opt/Yapplr/docker-compose.staging.yml restart nginx"
EOF
    
    log_success "Automatic renewal configured"
}

# Main execution
main() {
    log_info "Starting SSL setup for staging environment..."
    
    # Prompt for email if not set
    if [ "$EMAIL" = "your-email@example.com" ]; then
        read -p "Enter your email address for Let's Encrypt: " EMAIL
    fi
    
    install_certbot
    setup_webroot
    generate_certificates
    setup_nginx_certificates
    create_nginx_config
    setup_renewal
    
    echo ""
    log_success "🎉 SSL certificates have been generated for staging!"
    echo ""
    log_info "Next steps:"
    echo "1. Update your staging nginx configuration to use SSL"
    echo "2. Update your .env.staging file with HTTPS URLs:"
    echo "   API_BASE_URL=https://stg-api.yapplr.com"
    echo "   WS_BASE_URL=wss://stg-api.yapplr.com"
    echo "   FRONTEND_URL=https://stg.yapplr.com"
    echo "3. Restart your staging deployment"
    echo ""
    log_warning "Note: These are staging certificates from Let's Encrypt staging environment"
    log_warning "For production, remove the --staging flag from certbot commands"
}

# Parse command line arguments
case "${1:-setup}" in
    "setup")
        main
        ;;
    "renew")
        log_info "Renewing staging certificates..."
        certbot renew --staging
        log_success "Certificate renewal completed"
        ;;
    "test")
        log_info "Testing certificate configuration..."
        certbot certificates
        ;;
    *)
        echo "Usage: $0 {setup|renew|test}"
        echo ""
        echo "Commands:"
        echo "  setup - Generate SSL certificates for staging"
        echo "  renew - Renew existing certificates"
        echo "  test  - Test certificate configuration"
        exit 1
        ;;
esac
