"""
Node interface abstractions (MADSci-inspired, Tachyon-owned).

Goal:
- Each physical device (PF400, Planar, readers, etc.) can run as its own microservice.
- Tachyon scheduler talks to devices via a stable HTTP contract ("Node").

This module intentionally stays dependency-light (standard library only).
FastAPI services can implement this contract using their own Pydantic models.
"""

from __future__ import annotations

from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from enum import Enum
from typing import Any, Dict, List, Optional

from .ids import new_ulid_str


@dataclass(frozen=True)
class NodeAction:
    """An action a Node can execute (e.g. pick, place, move_to_location)."""

    name: str
    description: str = ""
    # Optional: schema/metadata for args; kept generic on purpose.
    args_schema: Dict[str, Any] = field(default_factory=dict)


@dataclass(frozen=True)
class NodeDefinition:
    """Definition/metadata for a Node."""

    node_id: str = field(default_factory=new_ulid_str)
    name: str = ""
    kind: str = ""  # e.g. "robot.arm", "robot.planar", "plate.reader"
    version: str = ""
    actions: List[NodeAction] = field(default_factory=list)


@dataclass
class NodeActionRequest:
    """Request payload for invoking an action on a Node."""

    request_id: str = field(default_factory=new_ulid_str)
    action: str = ""
    args: Dict[str, Any] = field(default_factory=dict)
    # Optional: locations passed in (already translated for this node)
    locations: Dict[str, Any] = field(default_factory=dict)


@dataclass
class NodeActionResponse:
    """Response payload from a Node action invocation."""

    request_id: str = ""
    execution_id: str = ""
    status: str = "succeeded"  # queued|running|succeeded|failed|cancelled
    success: bool = True  # convenience mirror of status
    result: Dict[str, Any] = field(default_factory=dict)
    error: Optional[str] = None


class ActionStatus(str, Enum):
    queued = "queued"
    running = "running"
    succeeded = "succeeded"
    failed = "failed"
    cancelled = "cancelled"


class NodeClient(ABC):
    """Client interface used by Tachyon to talk to a Node microservice."""

    @abstractmethod
    def get_definition(self) -> NodeDefinition:
        raise NotImplementedError

    @abstractmethod
    def health(self) -> bool:
        raise NotImplementedError

    @abstractmethod
    def call_action(self, req: NodeActionRequest, timeout_s: float = 30.0) -> NodeActionResponse:
        raise NotImplementedError

    # Optional async/distributed-friendly interface
    def submit_action(self, req: NodeActionRequest, timeout_s: float = 10.0) -> NodeActionResponse:
        """
        Submit an action for async execution and return an execution_id + initial status.
        Default implementation calls synchronous call_action().
        """
        return self.call_action(req=req, timeout_s=timeout_s)

    def get_action_status(self, execution_id: str, timeout_s: float = 10.0) -> NodeActionResponse:
        """
        Poll action status by execution_id.
        Default implementation is not supported.
        """
        raise NotImplementedError


