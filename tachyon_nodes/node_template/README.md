## Tachyon Node Template (FastAPI)

This is a minimal **device microservice template** implementing the Tachyon Node contract in `scheduler_framework/NODE_CONTRACT.md`.

### Endpoints
- `GET /health`
- `GET /definition`
- `POST /actions/{action}` (sync, short actions)
- `POST /actions/{action}/submit` (async)
- `GET /actions/status/{execution_id}` (poll)

### Running
Create a venv and install:

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
uvicorn main:app --host 0.0.0.0 --port 8090
```

Then:
- `curl -s http://localhost:8090/health`
- `curl -s http://localhost:8090/definition`

### Notes
- This template is intentionally “dumb” and safe.
- Real nodes should implement idempotency using `request_id`.
- The async executor here is an in-memory job store; for true distributed execution we’ll move this to Redis/DB.


