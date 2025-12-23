"""
Workflow / Protocol definition (YAML-friendly).

This is a Tachyon-owned workflow format inspired by MADSci concepts, but designed
to stay simple and focused on device orchestration through the Node contract.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional

from .ids import new_ulid_str


@dataclass
class WorkflowStep:
    """
    A single protocol step.

    Step kinds:
    - Node step: set `node` + `action`
    - Wait step: set `wait_seconds`
    """

    id: str
    name: str = ""
    description: str = ""

    # DAG execution
    depends_on: List[str] = field(default_factory=list)

    # Node step
    node: Optional[str] = None
    action: Optional[str] = None
    args: Dict[str, Any] = field(default_factory=dict)

    # Wait step
    wait_seconds: Optional[float] = None

    # Execution controls
    timeout_seconds: float = 120.0
    poll_interval_seconds: float = 0.2

    def validate(self) -> None:
        is_wait = self.wait_seconds is not None
        is_node = self.node is not None or self.action is not None
        if is_wait and is_node:
            raise ValueError(f"Step '{self.id}' cannot be both wait and node step")
        if not is_wait and not (self.node and self.action):
            raise ValueError(f"Step '{self.id}' must set either wait_seconds or (node + action)")


@dataclass
class WorkflowDefinition:
    workflow_id: str = field(default_factory=new_ulid_str)
    name: str = ""
    description: str = ""

    # Parameter substitution scope
    parameters: Dict[str, Any] = field(default_factory=dict)

    # Node name -> base URL
    nodes: Dict[str, str] = field(default_factory=dict)

    steps: List[WorkflowStep] = field(default_factory=list)

    def validate(self) -> None:
        step_ids = set()
        for s in self.steps:
            if s.id in step_ids:
                raise ValueError(f"Duplicate step id: {s.id}")
            step_ids.add(s.id)
            s.validate()
            if s.node and s.node not in self.nodes:
                raise ValueError(f"Step '{s.id}' references unknown node '{s.node}'")
            for dep in s.depends_on:
                if dep not in step_ids and dep not in [x.id for x in self.steps]:
                    raise ValueError(f"Step '{s.id}' depends_on unknown step '{dep}'")





