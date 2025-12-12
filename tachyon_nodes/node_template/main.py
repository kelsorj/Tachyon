"""
Tachyon Node Template (FastAPI).

Implements the contract in:
  `scheduler_framework/NODE_CONTRACT.md`

This is a reference microservice we can copy/adapt for:
- PF400 Node
- Planar Node
- Readers, washers, etc.

It uses an in-memory job store for async actions. For real distributed execution,
we'll swap this with Redis / a DB-backed queue.
"""

from __future__ import annotations

import threading
import time
from typing import Any, Dict, Optional

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field


# --- ULID: keep template self-contained (we can centralize later) ---
def _new_ulid_str() -> str:
    # Import from scheduler_framework if available; otherwise fallback.
    try:
        from scheduler.ids import new_ulid_str  # type: ignore

        return new_ulid_str()
    except Exception:
        import os

        alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"

        def enc(v: int, n: int) -> str:
            out = ["0"] * n
            for i in range(n - 1, -1, -1):
                out[i] = alphabet[v & 31]
                v >>= 5
            return "".join(out)

        ts_ms = int(time.time() * 1000) & ((1 << 48) - 1)
        rnd = int.from_bytes(os.urandom(10), "big")
        return enc(ts_ms, 10) + enc(rnd, 16)


class NodeActionModel(BaseModel):
    name: str
    description: str = ""
    args_schema: Dict[str, Any] = Field(default_factory=dict)


class NodeDefinitionModel(BaseModel):
    node_id: str
    name: str
    kind: str
    version: str = "0.0.1"
    actions: list[NodeActionModel] = Field(default_factory=list)


class ActionRequestModel(BaseModel):
    request_id: str = Field(default_factory=_new_ulid_str)
    action: str
    args: Dict[str, Any] = Field(default_factory=dict)
    locations: Dict[str, Any] = Field(default_factory=dict)


class ActionResponseModel(BaseModel):
    request_id: str
    execution_id: str = ""
    status: str = "succeeded"  # queued|running|succeeded|failed|cancelled
    success: bool = True
    result: Dict[str, Any] = Field(default_factory=dict)
    error: Optional[str] = None


class _Job:
    def __init__(self, request_id: str, action: str, args: Dict[str, Any], locations: Dict[str, Any]):
        self.request_id = request_id
        self.execution_id = _new_ulid_str()
        self.action = action
        self.args = args
        self.locations = locations
        self.status = "queued"
        self.success = True
        self.result: Dict[str, Any] = {}
        self.error: Optional[str] = None
        self.created_at = time.time()
        self.updated_at = time.time()


_jobs_lock = threading.Lock()
_jobs_by_execution_id: Dict[str, _Job] = {}
_execution_id_by_request_id: Dict[str, str] = {}


app = FastAPI(title="Tachyon Node Template", version="0.0.1")


@app.get("/health")
def health() -> Dict[str, Any]:
    return {"healthy": True}


@app.get("/definition")
def definition() -> NodeDefinitionModel:
    return NodeDefinitionModel(
        node_id="NODE_TEMPLATE",
        name="node-template",
        kind="template.node",
        version="0.0.1",
        actions=[
            NodeActionModel(
                name="sleep",
                description="Sleep for N seconds (demo async action).",
                args_schema={"seconds": {"type": "number", "minimum": 0}},
            ),
            NodeActionModel(
                name="echo",
                description="Echo input payload (demo sync action).",
                args_schema={"message": {"type": "string"}},
            ),
        ],
    )


def _execute_job(job: _Job) -> None:
    with _jobs_lock:
        job.status = "running"
        job.updated_at = time.time()

    try:
        if job.action == "sleep":
            seconds = float(job.args.get("seconds", 0))
            time.sleep(max(0.0, seconds))
            job.result = {"slept": seconds}
        else:
            raise ValueError(f"Unknown action: {job.action}")

        job.status = "succeeded"
        job.success = True
        job.error = None
    except Exception as e:
        job.status = "failed"
        job.success = False
        job.error = str(e)
    finally:
        with _jobs_lock:
            job.updated_at = time.time()


@app.post("/actions/{action}", response_model=ActionResponseModel)
def action_sync(action: str, req: ActionRequestModel) -> ActionResponseModel:
    # Override action from path to avoid ambiguity
    req_action = action

    if req_action == "echo":
        return ActionResponseModel(
            request_id=req.request_id,
            execution_id="",
            status="succeeded",
            success=True,
            result={"message": req.args.get("message"), "locations": req.locations},
            error=None,
        )

    raise HTTPException(status_code=400, detail="Use /submit for long-running actions or unknown sync action.")


@app.post("/actions/{action}/submit", response_model=ActionResponseModel)
def action_submit(action: str, req: ActionRequestModel) -> ActionResponseModel:
    # Idempotency on request_id: return same execution_id if already submitted.
    with _jobs_lock:
        existing = _execution_id_by_request_id.get(req.request_id)
        if existing:
            job = _jobs_by_execution_id[existing]
            return ActionResponseModel(
                request_id=job.request_id,
                execution_id=job.execution_id,
                status=job.status,
                success=job.success,
                result=job.result,
                error=job.error,
            )

        job = _Job(request_id=req.request_id, action=action, args=req.args, locations=req.locations)
        _jobs_by_execution_id[job.execution_id] = job
        _execution_id_by_request_id[req.request_id] = job.execution_id

    t = threading.Thread(target=_execute_job, args=(job,), daemon=True, name=f"job-{job.execution_id}")
    t.start()

    return ActionResponseModel(
        request_id=job.request_id,
        execution_id=job.execution_id,
        status=job.status,
        success=True,
        result={},
        error=None,
    )


@app.get("/actions/status/{execution_id}", response_model=ActionResponseModel)
def action_status(execution_id: str) -> ActionResponseModel:
    with _jobs_lock:
        job = _jobs_by_execution_id.get(execution_id)

    if not job:
        raise HTTPException(status_code=404, detail="Unknown execution_id")

    return ActionResponseModel(
        request_id=job.request_id,
        execution_id=job.execution_id,
        status=job.status,
        success=job.success if job.status in ("succeeded", "failed", "cancelled") else True,
        result=job.result if job.status == "succeeded" else {},
        error=job.error if job.status == "failed" else None,
    )


