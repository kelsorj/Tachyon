"""
REST Node client (standard library).

Expected Node endpoints (proposed contract):
- GET  /health -> {"healthy": true}
- GET  /definition -> NodeDefinition-like JSON
- POST /actions/{action} -> executes action, returns NodeActionResponse-like JSON

This is a Tachyon-owned contract; devices can implement it in FastAPI.
"""

from __future__ import annotations

import json
import urllib.request
import urllib.error
from dataclasses import asdict
from typing import Any, Dict, Optional

from .node_interface import (
    NodeClient,
    NodeDefinition,
    NodeAction,
    NodeActionRequest,
    NodeActionResponse,
)


def _join_url(base_url: str, path: str) -> str:
    base = base_url.rstrip("/")
    p = path if path.startswith("/") else f"/{path}"
    return base + p


class RestNodeClient(NodeClient):
    def __init__(self, base_url: str):
        self.base_url = base_url.rstrip("/")

    def _get_json(self, path: str, timeout_s: float) -> Dict[str, Any]:
        url = _join_url(self.base_url, path)
        req = urllib.request.Request(url, method="GET")
        try:
            with urllib.request.urlopen(req, timeout=timeout_s) as resp:
                body = resp.read().decode("utf-8")
                return json.loads(body) if body else {}
        except urllib.error.HTTPError as e:
            body = e.read().decode("utf-8") if hasattr(e, "read") else str(e)
            raise RuntimeError(f"GET {url} failed: {e.code} {body}") from e
        except Exception as e:
            raise RuntimeError(f"GET {url} failed: {e}") from e

    def _post_json(self, path: str, payload: Dict[str, Any], timeout_s: float) -> Dict[str, Any]:
        url = _join_url(self.base_url, path)
        data = json.dumps(payload).encode("utf-8")
        req = urllib.request.Request(
            url,
            data=data,
            method="POST",
            headers={"Content-Type": "application/json"},
        )
        try:
            with urllib.request.urlopen(req, timeout=timeout_s) as resp:
                body = resp.read().decode("utf-8")
                return json.loads(body) if body else {}
        except urllib.error.HTTPError as e:
            body = e.read().decode("utf-8") if hasattr(e, "read") else str(e)
            raise RuntimeError(f"POST {url} failed: {e.code} {body}") from e
        except Exception as e:
            raise RuntimeError(f"POST {url} failed: {e}") from e

    def health(self) -> bool:
        try:
            data = self._get_json("/health", timeout_s=3.0)
            return bool(data.get("healthy", True))
        except Exception:
            return False

    def get_definition(self) -> NodeDefinition:
        data = self._get_json("/definition", timeout_s=5.0)

        actions = []
        for a in data.get("actions", []) or []:
            if isinstance(a, dict) and "name" in a:
                actions.append(
                    NodeAction(
                        name=str(a.get("name", "")),
                        description=str(a.get("description", "")),
                        args_schema=a.get("args_schema") or {},
                    )
                )

        return NodeDefinition(
            node_id=str(data.get("node_id") or data.get("id") or ""),
            name=str(data.get("name") or ""),
            kind=str(data.get("kind") or ""),
            version=str(data.get("version") or ""),
            actions=actions,
        )

    def call_action(self, req: NodeActionRequest, timeout_s: float = 30.0) -> NodeActionResponse:
        payload = asdict(req)
        action = req.action
        # Primary: /actions/{action}
        data: Optional[Dict[str, Any]] = None
        try:
            data = self._post_json(f"/actions/{action}", payload=payload, timeout_s=timeout_s)
        except RuntimeError:
            # Fallback: /action (single endpoint)
            data = self._post_json("/action", payload=payload, timeout_s=timeout_s)

        return NodeActionResponse(
            request_id=str(data.get("request_id") or req.request_id),
            execution_id=str(data.get("execution_id") or data.get("job_id") or ""),
            status=str(data.get("status") or ("succeeded" if data.get("success", True) else "failed")),
            success=bool(data.get("success", True)),
            result=(data.get("result") or {}) if isinstance(data.get("result") or {}, dict) else {},
            error=data.get("error"),
        )

    def submit_action(self, req: NodeActionRequest, timeout_s: float = 10.0) -> NodeActionResponse:
        payload = asdict(req)
        action = req.action
        data = self._post_json(f"/actions/{action}/submit", payload=payload, timeout_s=timeout_s)
        return NodeActionResponse(
            request_id=str(data.get("request_id") or req.request_id),
            execution_id=str(data.get("execution_id") or data.get("job_id") or ""),
            status=str(data.get("status") or "queued"),
            success=bool(data.get("success", True)),
            result=(data.get("result") or {}) if isinstance(data.get("result") or {}, dict) else {},
            error=data.get("error"),
        )

    def get_action_status(self, execution_id: str, timeout_s: float = 10.0) -> NodeActionResponse:
        data = self._get_json(f"/actions/status/{execution_id}", timeout_s=timeout_s)
        return NodeActionResponse(
            request_id=str(data.get("request_id") or ""),
            execution_id=str(data.get("execution_id") or execution_id),
            status=str(data.get("status") or ""),
            success=bool(data.get("success", True)),
            result=(data.get("result") or {}) if isinstance(data.get("result") or {}, dict) else {},
            error=data.get("error"),
        )


