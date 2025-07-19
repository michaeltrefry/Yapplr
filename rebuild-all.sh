docker-compose -f docker-compose.local.yml down -d

docker-compose -f docker-compose.local.yml build --no-cache

docker-compose -f docker-compose.local.yml up -d

docker-compose -f docker-compose.local.yml ps
