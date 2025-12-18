"""
Workflow runner that executes steps by calling Node services.
"""

from __future__ import annotations

import time
from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional, Set

from .ids import new_ulid_str
from .node_client import RestNodeClient
from .node_interface import NodeActionRequest, NodeActionResponse
from .workflow_definition import WorkflowDefinition, WorkflowStep


@dataclass
class StepResult:
    step_id: str
    status: str
    success: bool
    execution_id: str = ""
    result: Dict[str, Any] = field(default_factory=dict)
    error: Optional[str] = None
    started_at: float = 0.0
    finished_at: float = 0.0


class WorkflowRunner:
    """
    Simple DAG executor:
    - steps run when all `depends_on` are complete
    - node steps use async submit/poll by default
    - wait steps sleep locally
    """

    def __init__(self, nodes: Dict[str, str]):
        self.nodes = nodes
        self.clients: Dict[str, RestNodeClient] = {k: RestNodeClient(v) for k, v in nodes.items()}

    def run(self, wf: WorkflowDefinition) -> Dict[str, StepResult]:
        results: Dict[str, StepResult] = {}
        pending: Dict[str, WorkflowStep] = {s.id: s for s in wf.steps}
        completed: Set[str] = set()

        def deps_done(step: WorkflowStep) -> bool:
            return all(d in completed for d in (step.depends_on or []))

        while pending:
            progressed = False
            for step_id, step in list(pending.items()):
                if not deps_done(step):
                    continue

                progressed = True
                pending.pop(step_id)
                started = time.time()

                if step.wait_seconds is not None:
                    time.sleep(float(step.wait_seconds))
                    results[step_id] = StepResult(
                        step_id=step_id,
                        status="succeeded",
                        success=True,
                        started_at=started,
                        finished_at=time.time(),
                    )
                    completed.add(step_id)
                    continue

                # Node action step
                if not step.node or not step.action:
                    results[step_id] = StepResult(
                        step_id=step_id,
                        status="failed",
                        success=False,
                        error="Invalid step: missing node/action",
                        started_at=started,
                        finished_at=time.time(),
                    )
                    completed.add(step_id)
                    continue

                client = self.clients[step.node]
                req = NodeActionRequest(
                    request_id=new_ulid_str(),
                    action=step.action,
                    args=step.args or {},
                    locations={},
                )

                # Submit (async) and poll
                submit_resp: NodeActionResponse = client.submit_action(req, timeout_s=10.0)
                exec_id = submit_resp.execution_id
                if not exec_id:
                    # Fallback to sync call
                    sync_resp = client.call_action(req, timeout_s=step.timeout_seconds)
                    results[step_id] = StepResult(
                        step_id=step_id,
                        status=sync_resp.status,
                        success=sync_resp.success,
                        execution_id=sync_resp.execution_id or "",
                        result=sync_resp.result or {},
                        error=sync_resp.error,
                        started_at=started,
                        finished_at=time.time(),
                    )
                    completed.add(step_id)
                    continue

                deadline = time.time() + float(step.timeout_seconds)
                last: Optional[NodeActionResponse] = None
                while time.time() < deadline:
                    last = client.get_action_status(exec_id, timeout_s=10.0)
                    if last.status in ("succeeded", "failed", "cancelled"):
                        break
                    time.sleep(float(step.poll_interval_seconds))

                if last is None:
                    results[step_id] = StepResult(
                        step_id=step_id,
                        status="failed",
                        success=False,
                        execution_id=exec_id,
                        error="No status response",
                        started_at=started,
                        finished_at=time.time(),
                    )
                elif last.status not in ("succeeded", "failed", "cancelled"):
                    results[step_id] = StepResult(
                        step_id=step_id,
                        status="failed",
                        success=False,
                        execution_id=exec_id,
                        error=f"Timed out waiting for completion (last status={last.status})",
                        started_at=started,
                        finished_at=time.time(),
                    )
                else:
                    results[step_id] = StepResult(
                        step_id=step_id,
                        status=last.status,
                        success=last.success,
                        execution_id=exec_id,
                        result=last.result or {},
                        error=last.error,
                        started_at=started,
                        finished_at=time.time(),
                    )

                completed.add(step_id)

            if not progressed:
                # Cyclic dependency or missing deps
                remaining = ", ".join(sorted(pending.keys()))
                raise RuntimeError(f"Workflow deadlock. Remaining steps: {remaining}")

        return results




