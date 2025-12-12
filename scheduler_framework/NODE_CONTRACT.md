## Tachyon Node HTTP Contract (v0)

This is **Tachyon-owned** (inspired by MADSci concepts, not copied). The goal is: **each device runs as its own FastAPI microservice** (“Node”), and the scheduler/workcell service orchestrates Nodes over HTTP.

### Design goals
- **Simple** to implement for devices (PF400, Planar, etc.)
- **Distributed-friendly**: supports async “submit + poll” so long actions don’t require long-lived HTTP connections
- **Idempotent-friendly**: every request carries a `request_id` (ULID). Nodes should dedupe if they receive retries.

---

## Endpoints

### `GET /health`
Returns basic liveness.

Response:
- `healthy`: boolean
- `detail`: optional string

### `GET /definition`
Returns metadata + list of supported actions.

Response (example fields):
- `node_id` (ULID)
- `name`
- `kind` (e.g. `robot.arm`, `robot.planar`)
- `version`
- `actions`: list of `{ name, description, args_schema }`

---

## Action execution (sync)

### `POST /actions/{action}`
Executes an action synchronously (good for short actions).

Request body:
- `request_id` (ULID) **required**
- `action` (string)
- `args` (object)
- `locations` (object) – already translated for this node (optional)

Response:
- `request_id`
- `execution_id` (optional ULID)
- `status`: `succeeded|failed`
- `success`: boolean
- `result`: object
- `error`: string|null

---

## Action execution (async / distributed-ready)

### `POST /actions/{action}/submit`
Submits an action for async execution (preferred for robot motion).

Request body: same shape as sync.

Response:
- `request_id`
- `execution_id` (ULID) **required**
- `status`: `queued|running|succeeded|failed|cancelled`
- `success`: boolean (optional; typically true unless immediate validation failure)
- `error`: string|null

### `GET /actions/status/{execution_id}`
Polls action status.

Response:
- `execution_id`
- `status`
- `success`
- `result` (present when succeeded)
- `error` (present when failed)

---

## Notes (important)

### Idempotency
Nodes should treat `request_id` as idempotency key. If the scheduler retries a submit due to timeout/network failure, the Node should:
- return the same `execution_id` for the same `request_id` (if possible), or
- safely no-op and return the already-running status.

### Correlation
Use `request_id` and `execution_id` in logs on both sides to make distributed debugging sane.

### Versioning
We’ll version this contract as `v0`, then introduce `/v1/...` if breaking changes become necessary.


