"""
Workflow I/O:
- Load WorkflowDefinition from YAML
- Parameter substitution (${param})
"""

from __future__ import annotations

import json
import re
from dataclasses import asdict
from typing import Any, Dict, List

from .ids import new_ulid_str
from .workflow_definition import WorkflowDefinition, WorkflowStep


_PARAM_RE = re.compile(r"\$\{([A-Za-z_][A-Za-z0-9_]*)\}")


def _substitute(value: Any, params: Dict[str, Any]) -> Any:
    if isinstance(value, str):
        # Replace ${param} occurrences
        def repl(m: re.Match) -> str:
            key = m.group(1)
            if key not in params:
                raise KeyError(f"Unknown parameter '{key}'")
            return str(params[key])

        return _PARAM_RE.sub(repl, value)
    if isinstance(value, list):
        return [_substitute(v, params) for v in value]
    if isinstance(value, dict):
        return {k: _substitute(v, params) for k, v in value.items()}
    return value


def load_workflow_yaml(path: str) -> WorkflowDefinition:
    try:
        import yaml  # type: ignore
    except ImportError as e:
        raise RuntimeError(
            "PyYAML is required to load .yaml workflows. "
            "Install with: pip install -r scheduler_framework/requirements.txt"
        ) from e

    with open(path, "r", encoding="utf-8") as f:
        data = yaml.safe_load(f) or {}

    wf_id = data.get("workflow_id") or ""
    if not wf_id:
        wf_id = new_ulid_str()

    params = data.get("parameters") or {}
    nodes = data.get("nodes") or {}

    steps: List[WorkflowStep] = []
    for s in data.get("steps") or []:
        sid = s.get("id")
        if not sid:
            raise ValueError("Each step must have an 'id'")
        step = WorkflowStep(
            id=str(sid),
            name=str(s.get("name") or ""),
            description=str(s.get("description") or ""),
            depends_on=list(s.get("depends_on") or []),
            node=s.get("node"),
            action=s.get("action"),
            args=s.get("args") or {},
            wait_seconds=s.get("wait_seconds"),
            timeout_seconds=float(s.get("timeout_seconds") or 120.0),
            poll_interval_seconds=float(s.get("poll_interval_seconds") or 0.2),
        )
        steps.append(step)

    # Substitute parameters across the whole definition (nodes too if desired)
    # We only substitute inside step args + wait_seconds (if string).
    for step in steps:
        if step.args:
            step.args = _substitute(step.args, params)
        if isinstance(step.wait_seconds, str):
            step.wait_seconds = float(_substitute(step.wait_seconds, params))

    wf = WorkflowDefinition(
        workflow_id=str(wf_id),
        name=str(data.get("name") or ""),
        description=str(data.get("description") or ""),
        parameters=params,
        nodes=nodes,
        steps=steps,
    )
    wf.validate()
    return wf


def load_workflow_file(path: str) -> WorkflowDefinition:
    """
    Load a workflow from YAML or JSON.

    - *.yaml / *.yml => requires PyYAML
    - *.json => uses standard library
    """
    lower = path.lower()
    if lower.endswith(".json"):
        with open(path, "r", encoding="utf-8") as f:
            data = json.load(f) or {}
        # Write JSON-to-definition through YAML loader logic by reusing the same code path:
        # We duplicate minimal parsing here to avoid requiring PyYAML.
        wf_id = data.get("workflow_id") or ""
        if not wf_id:
            wf_id = new_ulid_str()
        params = data.get("parameters") or {}
        nodes = data.get("nodes") or {}
        steps: List[WorkflowStep] = []
        for s in data.get("steps") or []:
            sid = s.get("id")
            if not sid:
                raise ValueError("Each step must have an 'id'")
            step = WorkflowStep(
                id=str(sid),
                name=str(s.get("name") or ""),
                description=str(s.get("description") or ""),
                depends_on=list(s.get("depends_on") or []),
                node=s.get("node"),
                action=s.get("action"),
                args=s.get("args") or {},
                wait_seconds=s.get("wait_seconds"),
                timeout_seconds=float(s.get("timeout_seconds") or 120.0),
                poll_interval_seconds=float(s.get("poll_interval_seconds") or 0.2),
            )
            steps.append(step)
        for step in steps:
            if step.args:
                step.args = _substitute(step.args, params)
            if isinstance(step.wait_seconds, str):
                step.wait_seconds = float(_substitute(step.wait_seconds, params))
        wf = WorkflowDefinition(
            workflow_id=str(wf_id),
            name=str(data.get("name") or ""),
            description=str(data.get("description") or ""),
            parameters=params,
            nodes=nodes,
            steps=steps,
        )
        wf.validate()
        return wf

    if lower.endswith(".yaml") or lower.endswith(".yml"):
        return load_workflow_yaml(path)

    raise ValueError("Unsupported workflow file type. Use .yaml/.yml or .json")


