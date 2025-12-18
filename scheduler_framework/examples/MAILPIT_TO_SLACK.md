# Mailpit → Slack bridge (LAN device email alerts)

This lets a device on your LAN send SMTP alerts (eg. temperature changes) to a Mac running **Mailpit**, and then forwards each new email to a **Slack channel** as a normal message.

## 1) Run Mailpit so the device can reach it

- Pick your Mac's LAN IP (example):

```bash
ipconfig getifaddr en0
```

- Start Mailpit, binding SMTP + UI to the LAN interface (replace `192.168.1.50` with your Mac's IP):

```bash
mailpit --smtp 192.168.1.50:1025 --listen 192.168.1.50:8025
```

Notes:
- SMTP on `1025` is common (non-privileged). Your device should be configured to send to `192.168.1.50:1025`.
- The Mailpit UI + API will be at `http://192.168.1.50:8025` (API base is `.../api/v1`).
- Mailpit is intended for dev/test. Keep it on LAN/VPN (not internet-exposed).

## 2) Create a Slack Incoming Webhook

Create a Slack Incoming Webhook and copy the webhook URL:
- Slack docs: `https://api.slack.com/messaging/webhooks`

## 3) Run the bridge

From `scheduler_framework/`:

```bash
export SLACK_WEBHOOK_URL="https://hooks.slack.com/services/XXX/YYY/ZZZ"
python3 examples/mailpit_to_slack_bridge.py --mailpit-ui http://192.168.1.50:8025
```

## 4) (Optional) Run Mailpit under PM2

If you already use PM2, it can keep `mailpit` running and auto-restart it.

### Direct command

```bash
pm2 start mailpit --name mailpit -- --smtp 192.168.1.50:1025 --listen 192.168.1.50:8025
pm2 save
```

### Ecosystem file

Create an `ecosystem.config.js` somewhere you manage PM2 apps:

```javascript
module.exports = {
  apps: [
    {
      name: "mailpit",
      script: "mailpit",
      args: "--smtp 192.168.1.50:1025 --listen 192.168.1.50:8025",
      autorestart: true,
    },
  ],
};
```

Then:

```bash
pm2 start ecosystem.config.js
pm2 save
```

If PM2 can’t find `mailpit`, set `script` to the full path from `which mailpit`.

### Optional filters (recommended)

If your device sends multiple kinds of email, add filters so only the temperature alerts go to Slack:

```bash
python3 examples/mailpit_to_slack_bridge.py \
  --mailpit-ui http://192.168.1.50:8025 \
  --subject-contains "temperature"
```

Other filter flags:
- `--from-contains "..."`
- `--to-contains "..."`

### Dedupe / restart behavior

The bridge stores seen Mailpit message IDs in:
- `~/.tachyon/mailpit_slack_seen.json`

You can change it with `--state-file /path/to/file.json`.


