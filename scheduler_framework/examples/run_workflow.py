#!/usr/bin/env python3
"""
Run a Tachyon workflow (protocol) YAML against Node services.

Example:
  python3 examples/run_workflow.py workflows/demo_pf400.workflow.yaml
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

# Allow running from source checkout without installing as a package
REPO_ROOT = Path(__file__).resolve().parents[1]
if str(REPO_ROOT) not in sys.path:
    sys.path.insert(0, str(REPO_ROOT))

from scheduler import load_workflow_file, WorkflowRunner  # noqa: E402


def main() -> int:
    parser = argparse.ArgumentParser(description="Run a Tachyon workflow (protocol) YAML/JSON against Node services.")
    parser.add_argument("workflow", help="Path to workflow file (.yaml/.yml or .json)")
    parser.add_argument(
        "--show-results",
        action="store_true",
        help="Include full step results (e.g. joints/cartesian) in output.",
    )
    parser.add_argument(
        "--pretty",
        action="store_true",
        help="Pretty-print JSON output.",
    )
    parser.add_argument(
        "--only-step",
        action="append",
        default=[],
        help="Only include these step IDs in the printed output (can be repeated).",
    )
    args = parser.parse_args()

    wf_path = Path(args.workflow).expanduser()
    if not wf_path.exists():
        print(f"Workflow not found: {wf_path}")
        return 2

    wf = load_workflow_file(str(wf_path))
    runner = WorkflowRunner(wf.nodes)
    results = runner.run(wf)

    only_steps = set(args.only_step or [])

    def want_step(step_id: str) -> bool:
        return True if not only_steps else step_id in only_steps

    # Print a summary (optionally include full results)
    summary = {
        "workflow_id": wf.workflow_id,
        "name": wf.name,
        "steps": {
            sid: {
                "status": r.status,
                "success": r.success,
                "execution_id": r.execution_id,
                "error": r.error,
                **(
                    {"result": (r.result or {})}
                    if args.show_results
                    else {"result_keys": sorted(list((r.result or {}).keys()))}
                ),
                "elapsed_s": round((r.finished_at - r.started_at), 3) if r.finished_at and r.started_at else None,
            }
            for sid, r in results.items()
            if want_step(sid)
        },
    }
    if args.pretty:
        print(json.dumps(summary, indent=2))
    else:
        print(json.dumps(summary, separators=(",", ":"), ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())


