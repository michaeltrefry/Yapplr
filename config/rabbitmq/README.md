# RabbitMQ Configuration

This directory contains RabbitMQ configuration files that replace deprecated environment variables.

## Files

### `rabbitmq.conf` (Staging Environment)
Used by `docker-compose.stage.yml` for staging deployments. Contains optimized settings for staging environment:
- Memory high watermark: 80%
- Disk free limit: 2GB
- Channel max: 2047
- Enhanced logging for debugging
- Classic queue type (quorum queues disabled)

### `rabbitmq-basic.conf` (Production & Local)
Used by `docker-compose.yml`, `docker-compose.local.yml`, and `docker-compose.prod.yml`. Contains standard settings:
- Memory high watermark: 40% (default)
- Disk free limit: 1GB
- Standard logging levels
- Basic performance tuning

## Migration from Environment Variables

The following deprecated environment variables have been replaced:

| Deprecated Environment Variable | New Configuration Setting |
|--------------------------------|---------------------------|
| `RABBITMQ_VM_MEMORY_HIGH_WATERMARK=0.8` | `vm_memory_high_watermark.relative = 0.8` |
| `RABBITMQ_DISK_FREE_LIMIT=2GB` | `disk_free_limit.absolute = 2GB` |
| `RABBITMQ_CHANNEL_MAX=2047` | `channel_max = 2047` |
| `RABBITMQ_HANDSHAKE_TIMEOUT=60000` | `handshake_timeout = 60000` |
| `RABBITMQ_DEFAULT_QUEUE_TYPE=classic` | `default_queue_type = classic` |

## Usage

The configuration files are automatically mounted into the RabbitMQ containers via Docker Compose volume mounts:

```yaml
volumes:
  - ./config/rabbitmq/rabbitmq.conf:/etc/rabbitmq/rabbitmq.conf:ro
```

## Benefits

1. **No more deprecation warnings** - Uses current RabbitMQ configuration format
2. **Better maintainability** - Configuration is centralized in files
3. **Environment-specific tuning** - Different configs for different environments
4. **Future-proof** - Uses the recommended configuration approach

## Troubleshooting

If you encounter issues:

1. Check container logs: `docker logs <rabbitmq-container-id>`
2. Verify configuration: `docker exec <container-id> rabbitmq-diagnostics environment`
3. Use the troubleshooting script: `./scripts/troubleshoot-rabbitmq.sh`

## References

- [RabbitMQ Configuration Guide](https://www.rabbitmq.com/configure.html)
- [RabbitMQ Configuration File Documentation](https://www.rabbitmq.com/configure.html#configuration-files)
