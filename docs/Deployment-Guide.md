# Deployment Guide

This guide covers deploying the UserAuth API to various environments and platforms.

## Table of Contents

- [Local Development](#local-development)
- [Windows Server (IIS)](#windows-server-iis)
- [Linux Server (Ubuntu)](#linux-server-ubuntu)
- [Docker Deployment](#docker-deployment)
- [Azure App Service](#azure-app-service)
- [Environment Configuration](#environment-configuration)
- [Database Setup](#database-setup)
- [Monitoring & Maintenance](#monitoring--maintenance)

## Local Development

### Prerequisites

- .NET 8.0 SDK or later
- Git
- Code editor (VS Code, Visual Studio)

### Setup Steps

```bash
# Clone repository
git clone <repository-url>
cd csharpapi/backend

# Restore dependencies
dotnet restore

# Apply database migrations
dotnet ef database update --project UserAuthAPI.Infrastructure --startup-project UserAuthAPI.Api

# Run the application
dotnet run --project UserAuthAPI.Api
```

### Development Environment Variables

```bash
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS=http://localhost:5098
```

## Windows Server (IIS)

### Prerequisites

- Windows Server 2019 or later
- IIS with ASP.NET Core Module
- .NET 8.0 Hosting Bundle

### Installation Steps

#### 1. Install .NET Hosting Bundle

```powershell
# Download and install .NET 8.0 Hosting Bundle
Invoke-WebRequest -Uri "https://download.microsoft.com/download/..." -OutFile "dotnet-hosting-bundle.exe"
./dotnet-hosting-bundle.exe
```

#### 2. Configure IIS

```powershell
# Enable IIS features
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole, IIS-WebServer, IIS-CommonHttpFeatures, IIS-HttpErrors, IIS-HttpLogging, IIS-RequestFiltering, IIS-StaticContent, IIS-Security, IIS-RequestFiltering, IIS-DefaultDocument, IIS-DirectoryBrowsing, IIS-ASPNET45

# Install ASP.NET Core Module
Install-Module -Name AspNetCoreModuleV2
```

#### 3. Publish Application

```bash
# Publish for production
dotnet publish UserAuthAPI.Api -c Release -o C:\inetpub\wwwroot\userauth-api
```

#### 4. Create IIS Site

```powershell
# Create application pool
New-WebAppPool -Name "UserAuthAPI" -Force
Set-ItemProperty -Path "IIS:\AppPools\UserAuthAPI" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"

# Create website
New-Website -Name "UserAuthAPI" -ApplicationPool "UserAuthAPI" -PhysicalPath "C:\inetpub\wwwroot\userauth-api" -Port 80
```

#### 5. Configure web.config

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\UserAuthAPI.Api.dll"
                  stdoutLogEnabled="false"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
```

### Production Configuration (Windows)

```json
{
	"ConnectionStrings": {
		"DefaultConnection": "Server=.\\SQLEXPRESS;Database=UserAuthProd;Trusted_Connection=true;TrustServerCertificate=true;"
	},
	"Jwt": {
		"SecretKey": "your-production-secret-key-from-environment-or-key-vault",
		"Issuer": "UserAuthAPI",
		"Audience": "UserAuthAPI",
		"AccessTokenExpirationMinutes": 15,
		"RefreshTokenExpirationDays": 7
	}
}
```

## Linux Server (Ubuntu)

### Prerequisites

- Ubuntu 20.04 LTS or later
- .NET 8.0 Runtime
- Nginx (reverse proxy)
- SQLite or PostgreSQL

### Installation Steps

#### 1. Install .NET Runtime

```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb

# Install .NET runtime
sudo apt update
sudo apt install -y aspnetcore-runtime-8.0
```

#### 2. Create Service User

```bash
# Create dedicated user for the application
sudo useradd -r -d /var/www/userauth-api -s /bin/false userauth
sudo mkdir -p /var/www/userauth-api
sudo chown userauth:userauth /var/www/userauth-api
```

#### 3. Deploy Application

```bash
# Publish application locally, then copy to server
dotnet publish UserAuthAPI.Api -c Release -o ./publish

# Copy to server (using SCP or similar)
scp -r ./publish/* user@server:/var/www/userauth-api/

# Set permissions
sudo chown -R userauth:userauth /var/www/userauth-api
sudo chmod +x /var/www/userauth-api/UserAuthAPI.Api
```

#### 4. Create systemd Service

```bash
# Create service file
sudo nano /etc/systemd/system/userauth-api.service
```

```ini
[Unit]
Description=UserAuth API
After=network.target

[Service]
Type=notify
User=userauth
WorkingDirectory=/var/www/userauth-api
ExecStart=/usr/bin/dotnet /var/www/userauth-api/UserAuthAPI.Api.dll
Restart=always
RestartSec=10
SyslogIdentifier=userauth-api
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target
```

#### 5. Configure Nginx

```bash
# Install Nginx
sudo apt install nginx

# Create site configuration
sudo nano /etc/nginx/sites-available/userauth-api
```

```nginx
server {
    listen 80;
    server_name your-domain.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

#### 6. Enable and Start Services

```bash
# Enable and start the API service
sudo systemctl enable userauth-api.service
sudo systemctl start userauth-api.service

# Enable Nginx site
sudo ln -s /etc/nginx/sites-available/userauth-api /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

### SSL Configuration with Let's Encrypt

```bash
# Install Certbot
sudo apt install certbot python3-certbot-nginx

# Obtain SSL certificate
sudo certbot --nginx -d your-domain.com

# Automatic renewal (crontab)
0 12 * * * /usr/bin/certbot renew --quiet
```

## Docker Deployment

### Dockerfile

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["UserAuthAPI.Api/UserAuthAPI.Api.csproj", "UserAuthAPI.Api/"]
COPY ["UserAuthAPI.Application/UserAuthAPI.Application.csproj", "UserAuthAPI.Application/"]
COPY ["UserAuthAPI.Domain/UserAuthAPI.Domain.csproj", "UserAuthAPI.Domain/"]
COPY ["UserAuthAPI.Infrastructure/UserAuthAPI.Infrastructure.csproj", "UserAuthAPI.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "UserAuthAPI.Api/UserAuthAPI.Api.csproj"

# Copy source code
COPY . .

# Build application
WORKDIR "/src/UserAuthAPI.Api"
RUN dotnet build "UserAuthAPI.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "UserAuthAPI.Api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

# Copy published app
COPY --from=publish /app/publish .

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/api/health || exit 1

# Start application
ENTRYPOINT ["dotnet", "UserAuthAPI.Api.dll"]
```

### Docker Compose

```yaml
version: '3.8'

services:
  userauth-api:
    build: .
    ports:
      - '80:8080'
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/userauth.db
      - Jwt__SecretKey=${JWT_SECRET_KEY}
    volumes:
      - ./data:/app/data
      - ./logs:/app/logs
    restart: unless-stopped
    healthcheck:
      test: ['CMD', 'curl', '-f', 'http://localhost:8080/api/health']
      interval: 30s
      timeout: 10s
      retries: 3
    networks:
      - userauth-network

  nginx:
    image: nginx:alpine
    ports:
      - '443:443'
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/nginx/ssl
    depends_on:
      - userauth-api
    restart: unless-stopped
    networks:
      - userauth-network

networks:
  userauth-network:
    driver: bridge

volumes:
  userauth-data:
  userauth-logs:
```

### Build and Run

```bash
# Build image
docker build -t userauth-api .

# Run container
docker run -d \
  --name userauth-api \
  -p 80:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e JWT_SECRET_KEY="your-secret-key" \
  -v $(pwd)/data:/app/data \
  userauth-api

# Using Docker Compose
docker-compose up -d
```

## Azure App Service

### Prerequisites

- Azure subscription
- Azure CLI installed

### Deployment Steps

#### 1. Create Azure Resources

```bash
# Login to Azure
az login

# Create resource group
az group create --name userauth-rg --location "East US"

# Create App Service plan
az appservice plan create \
  --name userauth-plan \
  --resource-group userauth-rg \
  --sku B1 \
  --is-linux

# Create web app
az webapp create \
  --resource-group userauth-rg \
  --plan userauth-plan \
  --name userauth-api-unique \
  --runtime "DOTNETCORE:8.0"
```

#### 2. Configure Application Settings

```bash
# Set application settings
az webapp config appsettings set \
  --resource-group userauth-rg \
  --name userauth-api-unique \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    Jwt__SecretKey="your-production-secret-key" \
    Jwt__Issuer="UserAuthAPI" \
    Jwt__Audience="UserAuthAPI" \
    Features__EnableSwagger=false
```

#### 3. Deploy Application

```bash
# Publish application
dotnet publish UserAuthAPI.Api -c Release -o ./publish

# Create deployment package
cd publish
zip -r ../app.zip .
cd ..

# Deploy to Azure
az webapp deployment source config-zip \
  --resource-group userauth-rg \
  --name userauth-api-unique \
  --src app.zip
```

#### 4. Configure Custom Domain (Optional)

```bash
# Add custom domain
az webapp config hostname add \
  --webapp-name userauth-api-unique \
  --resource-group userauth-rg \
  --hostname api.yourdomain.com

# Enable HTTPS
az webapp update \
  --resource-group userauth-rg \
  --name userauth-api-unique \
  --https-only true
```

## Environment Configuration

### Development Environment

```json
{
	"Serilog": {
		"MinimumLevel": {
			"Default": "Debug"
		}
	},
	"Features": {
		"EnableSwagger": true,
		"EnableDatabaseSeeding": true,
		"EnableDetailedErrors": true
	},
	"Database": {
		"EnableSensitiveDataLogging": true
	}
}
```

### Production Environment

```json
{
	"Serilog": {
		"MinimumLevel": {
			"Default": "Information"
		}
	},
	"Features": {
		"EnableSwagger": false,
		"EnableDatabaseSeeding": false,
		"EnableDetailedErrors": false
	},
	"Security": {
		"RequireHttps": true,
		"StrictTransportSecurity": true
	}
}
```

### Environment Variables (Production)

```bash
# Essential environment variables
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS=https://+:443;http://+:80
export JWT_SECRET_KEY="your-very-secure-secret-key-minimum-32-characters"
export DATABASE_CONNECTION_STRING="your-production-database-connection"
```

## Database Setup

### SQLite (Development/Small Production)

```json
{
	"ConnectionStrings": {
		"DefaultConnection": "Data Source=/app/data/userauth.db"
	}
}
```

### SQL Server (Production)

```json
{
	"ConnectionStrings": {
		"DefaultConnection": "Server=your-server;Database=UserAuthProd;User Id=userauth;Password=secure-password;TrustServerCertificate=true;"
	}
}
```

### PostgreSQL (Linux Production)

```json
{
	"ConnectionStrings": {
		"DefaultConnection": "Host=localhost;Database=userauth;Username=userauth;Password=secure-password"
	}
}
```

### Migration Commands

```bash
# Create migration
dotnet ef migrations add MigrationName --project UserAuthAPI.Infrastructure --startup-project UserAuthAPI.Api

# Apply migrations
dotnet ef database update --project UserAuthAPI.Infrastructure --startup-project UserAuthAPI.Api

# Production migration script
dotnet ef migrations script --project UserAuthAPI.Infrastructure --startup-project UserAuthAPI.Api --output migration.sql
```

## Monitoring & Maintenance

### Health Check Monitoring

```bash
# Simple health check
curl -f http://your-domain.com/api/health || echo "API is down!"

# Advanced monitoring script
#!/bin/bash
HEALTH_URL="http://your-domain.com/api/health"
EXPECTED_STATUS="healthy"

response=$(curl -s $HEALTH_URL)
status=$(echo $response | jq -r '.status')

if [ "$status" != "$EXPECTED_STATUS" ]; then
    echo "Health check failed: $response"
    # Send alert (email, Slack, etc.)
fi
```

### Log Management

```bash
# Log rotation (Linux)
sudo nano /etc/logrotate.d/userauth-api
```

```
/var/www/userauth-api/logs/*.txt {
    daily
    missingok
    rotate 30
    compress
    delaycompress
    notifempty
    create 644 userauth userauth
}
```

### Backup Strategy

```bash
# Database backup script
#!/bin/bash
BACKUP_DIR="/backups/userauth"
DATE=$(date +%Y%m%d_%H%M%S)

# Create backup directory
mkdir -p $BACKUP_DIR

# Backup SQLite database
cp /app/data/userauth.db "$BACKUP_DIR/userauth_$DATE.db"

# Cleanup old backups (keep 30 days)
find $BACKUP_DIR -name "userauth_*.db" -mtime +30 -delete
```

### Performance Monitoring

```bash
# Monitor application performance
dotnet-counters monitor --process-id $(pgrep -f UserAuthAPI.Api)

# Monitor specific metrics
dotnet-counters monitor \
  --counters System.Runtime,Microsoft.AspNetCore.Hosting \
  --process-id $(pgrep -f UserAuthAPI.Api)
```

## Security Checklist

### Pre-Deployment Security

- [ ] JWT secret key is secure and environment-specific
- [ ] Database connection strings don't contain hardcoded credentials
- [ ] HTTPS is enforced in production
- [ ] Swagger is disabled in production
- [ ] Detailed error messages are disabled in production
- [ ] CORS origins are properly configured
- [ ] Security headers are enabled

### Post-Deployment Security

- [ ] Regular security updates applied
- [ ] SSL certificates are valid and renewed
- [ ] Database access is restricted
- [ ] Application logs are monitored
- [ ] Backup and recovery procedures tested

## Troubleshooting

### Common Issues

#### Application Won't Start

```bash
# Check logs
journalctl -u userauth-api.service -f

# Check application logs
tail -f /var/www/userauth-api/logs/userauth-*.txt
```

#### Database Connection Issues

```bash
# Test database connectivity
dotnet ef database update --project UserAuthAPI.Infrastructure --startup-project UserAuthAPI.Api --dry-run
```

#### Performance Issues

```bash
# Monitor resource usage
top -p $(pgrep -f UserAuthAPI.Api)

# Check memory usage
cat /proc/$(pgrep -f UserAuthAPI.Api)/status | grep VmRSS
```

---

_This deployment guide provides comprehensive instructions for deploying the UserAuth API to various environments. Choose the deployment method that best fits your infrastructure and requirements._
