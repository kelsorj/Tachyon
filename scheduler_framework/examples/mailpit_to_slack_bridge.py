#!/usr/bin/env python3
"""
Mailpit -> Slack bridge (LAN friendly)

Polls Mailpit's REST API for new messages and forwards them to Slack via an
Incoming Webhook URL.

Typical use case: a device on the same LAN emits SMTP alerts (eg. temperature
change) to Mailpit running on a Mac, and this bridge posts the alert into Slack.

Mailpit API:
  - GET  {base}/api/v1/messages
  - GET  {base}/api/v1/message/{id}
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen


def _http_json(url: str, *, method: str = "GET", payload: Optional[dict] = None, timeout_s: float = 5.0) -> Any:
    data: Optional[bytes] = None
    headers = {"Accept": "application/json"}
    if payload is not None:
        data = json.dumps(payload).encode("utf-8")
        headers["Content-Type"] = "application/json; charset=utf-8"
    req = Request(url, data=data, headers=headers, method=method)
    with urlopen(req, timeout=timeout_s) as resp:
        raw = resp.read().decode("utf-8", errors="replace")
        return json.loads(raw) if raw else None


def _best_effort_str(x: Any) -> str:
    if x is None:
        return ""
    if isinstance(x, str):
        return x
    return str(x)


def _extract_addr(field: Any) -> str:
    """
    Mailpit often returns:
      {"Name": "...", "Address": "a@b.com"}
    but we tolerate variations.
    """
    if field is None:
        return ""
    if isinstance(field, str):
        return field
    if isinstance(field, dict):
        name = _best_effort_str(field.get("Name") or field.get("name")).strip()
        addr = _best_effort_str(field.get("Address") or field.get("address")).strip()
        if name and addr:
            return f"{name} <{addr}>"
        return addr or name
    return _best_effort_str(field)


def _extract_to_list(field: Any) -> List[str]:
    if field is None:
        return []
    if isinstance(field, list):
        return [_extract_addr(x) for x in field if _extract_addr(x)]
    return [_extract_addr(field)] if _extract_addr(field) else []


@dataclass(frozen=True)
class Filters:
    subject_contains: Optional[str] = None
    from_contains: Optional[str] = None
    to_contains: Optional[str] = None

    def matches(self, *, subject: str, from_addr: str, to_addrs: Iterable[str]) -> bool:
        if self.subject_contains and self.subject_contains.lower() not in subject.lower():
            return False
        if self.from_contains and self.from_contains.lower() not in from_addr.lower():
            return False
        if self.to_contains:
            joined_to = " ".join(to_addrs).lower()
            if self.to_contains.lower() not in joined_to:
                return False
        return True


class SeenState:
    def __init__(self, path: Path, *, max_ids: int = 2000) -> None:
        self.path = path
        self.max_ids = max_ids
        self._ids: List[str] = []
        self._set: set[str] = set()

    def load(self) -> None:
        try:
            if self.path.exists():
                data = json.loads(self.path.read_text(encoding="utf-8"))
                if isinstance(data, list):
                    self._ids = [str(x) for x in data]
                    self._set = set(self._ids)
        except Exception:
            # If state is corrupted, start fresh rather than crash/spam.
            self._ids = []
            self._set = set()

    def save(self) -> None:
        self.path.parent.mkdir(parents=True, exist_ok=True)
        self.path.write_text(json.dumps(self._ids[-self.max_ids :], indent=2), encoding="utf-8")

    def __contains__(self, msg_id: str) -> bool:
        return msg_id in self._set

    def add(self, msg_id: str) -> None:
        self._ids.append(msg_id)
        self._set.add(msg_id)
        if len(self._ids) > self.max_ids:
            self._ids = self._ids[-self.max_ids :]
            self._set = set(self._ids)


def _mailpit_list_messages(api_base: str) -> List[Dict[str, Any]]:
    data = _http_json(f"{api_base}/messages")
    if isinstance(data, dict) and isinstance(data.get("messages"), list):
        return data["messages"]
    return []


def _mailpit_get_message(api_base: str, msg_id: str) -> Dict[str, Any]:
    data = _http_json(f"{api_base}/message/{msg_id}")
    return data if isinstance(data, dict) else {}


def _slack_post(webhook_url: str, text: str) -> None:
    # Slack Incoming Webhooks expect {"text": "..."}.
    _http_json(webhook_url, method="POST", payload={"text": text}, timeout_s=10.0)


def _format_slack_message(*, subject: str, from_addr: str, to_addrs: List[str], body: str, msg_id: str) -> str:
    to_line = ", ".join([a for a in to_addrs if a]) if to_addrs else ""
    parts = [
        "*Mail alert received*",
        f"*From:* {from_addr or '(unknown)'}",
        f"*To:* {to_line}" if to_line else None,
        f"*Subject:* {subject or '(no subject)'}",
        "",
        body.strip() or "(empty body)",
        "",
        f"_Mailpit ID: {msg_id}_",
    ]
    return "\n".join([p for p in parts if p is not None])


def main(argv: Optional[List[str]] = None) -> int:
    p = argparse.ArgumentParser(description="Poll Mailpit and forward messages to Slack.")
    p.add_argument("--mailpit-ui", default=os.getenv("MAILPIT_UI_BASE", "http://127.0.0.1:8025"),
                   help="Mailpit UI base URL (default: http://127.0.0.1:8025).")
    p.add_argument("--slack-webhook-url", default=os.getenv("SLACK_WEBHOOK_URL"),
                   help="Slack Incoming Webhook URL (or set SLACK_WEBHOOK_URL).")
    p.add_argument("--poll-seconds", type=float, default=float(os.getenv("POLL_SECONDS", "2")),
                   help="Polling interval (seconds).")
    p.add_argument("--max-body-chars", type=int, default=int(os.getenv("MAX_BODY_CHARS", "3000")),
                   help="Max characters of body/snippet to post to Slack.")
    p.add_argument("--state-file", default=os.getenv("STATE_FILE", str(Path.home() / ".tachyon" / "mailpit_slack_seen.json")),
                   help="Path to a JSON file used to dedupe messages across restarts.")
    p.add_argument("--subject-contains", default=os.getenv("SUBJECT_CONTAINS"),
                   help="Only forward messages whose subject contains this substring (case-insensitive).")
    p.add_argument("--from-contains", default=os.getenv("FROM_CONTAINS"),
                   help="Only forward messages whose From contains this substring (case-insensitive).")
    p.add_argument("--to-contains", default=os.getenv("TO_CONTAINS"),
                   help="Only forward messages whose To contains this substring (case-insensitive).")
    args = p.parse_args(argv)

    if not args.slack_webhook_url:
        print("ERROR: Provide --slack-webhook-url or set SLACK_WEBHOOK_URL.", file=sys.stderr)
        return 2

    ui_base = args.mailpit_ui.rstrip("/")
    api_base = f"{ui_base}/api/v1"

    filters = Filters(
        subject_contains=args.subject_contains,
        from_contains=args.from_contains,
        to_contains=args.to_contains,
    )

    state = SeenState(Path(args.state_file))
    state.load()

    print(f"Mailpit API: {api_base}")
    print("Forwarding to Slack via Incoming Webhook.")
    if any([args.subject_contains, args.from_contains, args.to_contains]):
        print(f"Filters: subject~={args.subject_contains!r}, from~={args.from_contains!r}, to~={args.to_contains!r}")
    print(f"State file: {state.path}")

    while True:
        try:
            msgs = _mailpit_list_messages(api_base)
            # Process oldest-first if possible so Slack reads naturally.
            msgs = list(reversed(msgs))

            for m in msgs:
                msg_id = _best_effort_str(m.get("ID") or m.get("id")).strip()
                if not msg_id or msg_id in state:
                    continue

                subject = _best_effort_str(m.get("Subject") or m.get("subject")).strip()
                from_addr = _extract_addr(m.get("From") or m.get("from"))
                to_addrs = _extract_to_list(m.get("To") or m.get("to"))

                if not filters.matches(subject=subject, from_addr=from_addr, to_addrs=to_addrs):
                    state.add(msg_id)
                    state.save()
                    continue

                full = _mailpit_get_message(api_base, msg_id)
                body = _best_effort_str(full.get("Text") or full.get("text") or m.get("Snippet") or m.get("snippet"))
                body = body[: args.max_body_chars]

                slack_text = _format_slack_message(
                    subject=subject,
                    from_addr=from_addr,
                    to_addrs=to_addrs,
                    body=body,
                    msg_id=msg_id,
                )
                _slack_post(args.slack_webhook_url, slack_text)

                state.add(msg_id)
                state.save()

        except (HTTPError, URLError, TimeoutError) as e:
            print(f"[warn] network error: {e}", file=sys.stderr)
        except Exception as e:
            print(f"[warn] unexpected error: {e}", file=sys.stderr)

        time.sleep(args.poll_seconds)


if __name__ == "__main__":
    raise SystemExit(main())


