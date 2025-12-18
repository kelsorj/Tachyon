## Tachyon Infra (Redis + MinIO)

This folder provides the supporting services Tachyon will use for:
- **Redis (6379)**: real-time state + task queuing
- **MinIO (9000/9001)**: S3-compatible object storage for data files

### Prereqs
- Docker Desktop (or compatible Docker Engine)

### Start

```bash
cd tachyon_infra
# optional:
cp env.example .env
docker compose --env-file .env -f compose.yaml up -d
```

### Stop

```bash
cd tachyon_infra
docker compose -f compose.yaml down
```

### Verify

```bash
# Redis
redis-cli -p 6379 ping

# MinIO health (S3 API)
curl -s http://localhost:9000/minio/health/live

# MinIO console
# http://localhost:9001  (login with MINIO_ROOT_USER / MINIO_ROOT_PASSWORD)
```

### Notes
- Volumes are persisted via Docker named volumes: `redis_data`, `minio_data`
- For anything beyond local dev, set strong MinIO credentials in `.env`




