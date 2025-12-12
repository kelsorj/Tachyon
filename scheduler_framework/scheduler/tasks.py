"""
Task model definitions.

Separated into its own module to avoid circular imports between:
- `active_plate.py` (plate state & locations)
- `worklist.py` (work definition & transfers)
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Dict

from .ids import new_ulid_str


@dataclass
class PlateTask:
    """Represents a single task to be performed on a plate."""

    device_type: str  # e.g., "Bumblebee", "Reader", "PF400", "Planar"
    command: str  # e.g., "pick", "place", "read", "move_to_location"
    parameters: Dict[str, Any] = field(default_factory=dict)
    completed: bool = False
    task_id: str = field(default_factory=new_ulid_str)

    def __repr__(self) -> str:
        return f"PlateTask({self.device_type}.{self.command})"


@dataclass
class WaitTask:
    """Represents a wait/delay task."""

    duration_seconds: float
    description: str = ""
    completed: bool = False
    task_id: str = field(default_factory=new_ulid_str)

    def __repr__(self) -> str:
        return f"WaitTask({self.duration_seconds}s)"


