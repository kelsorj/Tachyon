## Tachyon Protocols (Workflows)

This describes how a user defines “protocols” that Tachyon can schedule and run across multiple device Nodes.

Today we support a **simple YAML workflow format** with:
- **parameters** (templated with `${param}`)
- **steps** (node + action + args)
- **wait steps**
- **dependencies** between steps (DAG-style)

Execution happens via the Node contract in `NODE_CONTRACT.md`:
- `POST /actions/{action}/submit`
- `GET /actions/status/{execution_id}`

---

## Workflow YAML format (v0)

```yaml
workflow_id: ""           # optional; if empty, Tachyon will generate a ULID
name: "Demo Protocol"
description: "Example protocol using PF400 + wait"

parameters:
  speed_profile: 1
  rail_step_m: 0.01

nodes:
  pf400: "http://localhost:8091"   # base URL of the PF400 Node

steps:
  - id: "jog-rail-1"
    name: "Jog rail a bit"
    node: "pf400"
    action: "jog"
    args:
      joint: 6
      distance: "${rail_step_m}"
      speed_profile: "${speed_profile}"

  - id: "wait-1"
    name: "Wait for settling"
    wait_seconds: 10
    depends_on: ["jog-rail-1"]

  - id: "read-state"
    name: "Fetch joints"
    node: "pf400"
    action: "get_joints"
    depends_on: ["wait-1"]
```

### Notes
- **`depends_on`** is optional. If omitted, steps execute in order.
- A step is either:
  - a **Node step** (`node` + `action`), or
  - a **Wait step** (`wait_seconds`)
- Parameter substitution supports `${param_name}` inside strings and inside nested dict/list structures.

---

## Running a workflow

Use the runner:

```bash
python3 examples/run_workflow.py workflows/demo_pf400.workflow.yaml
```

---

## What’s next (near-term)

- Add “resource locks” (e.g., plate ownership, robot mastership leases)
- Add “location references” (handoff stations, device deck positions)
- Add retries, timeouts per step, and cancellation
- Add a Workcell/Scheduler FastAPI service that accepts workflows over HTTP





