# Quick Start - PM2 Development Mode

## One-Time Setup (Already Done!)

✅ Node.js installed  
✅ PM2 installed  
✅ Frontend dependencies installed  
✅ Port configuration fixed  
✅ Scripts made executable  

## Start Services

### Option 1: Quick Start Script (Easiest)
```bash
cd pf400_gui
./start-dev.sh
```

### Option 2: PM2 Direct
```bash
cd pf400_gui
pm2 start ecosystem.dev.config.js
```

### Option 3: Helper Script
```bash
cd pf400_gui
./pm2-commands.sh start
```

## Check Status

```bash
pm2 status
```

You should see:
- `pf400-backend` - Running on port 3061
- `pf400-frontend` - Running on port 5173

## View Logs

```bash
# All logs
pm2 logs

# Backend only
pm2 logs pf400-backend

# Frontend only
pm2 logs pf400-frontend
```

## Access Services

- **Frontend**: http://localhost:5173
- **Backend API**: http://localhost:3061
- **Diagnostics**: http://localhost:3061/diagnostics

## Stop Services

```bash
pm2 stop all
```

## Restart Services

```bash
pm2 restart all
```

## Development Features

- ✅ Auto-restart on file changes (watch mode enabled)
- ✅ Separate logs for frontend and backend
- ✅ Process monitoring
- ✅ Easy start/stop/restart

## Common Commands

```bash
# Start
pm2 start ecosystem.dev.config.js

# Stop
pm2 stop all

# Restart
pm2 restart all

# Status
pm2 status

# Logs
pm2 logs

# Monitor (real-time dashboard)
pm2 monit

# Delete all processes
pm2 delete all
```

## Troubleshooting

### Services won't start
```bash
# Check logs
pm2 logs

# Check if ports are in use
lsof -i :3061  # Backend
lsof -i :5173  # Frontend
```

### Backend issues
```bash
# Check backend logs
pm2 logs pf400-backend

# Verify venv is set up
cd backend && source venv/bin/activate
```

### Frontend issues
```bash
# Check frontend logs
pm2 logs pf400-frontend

# Reinstall dependencies if needed
cd frontend && npm install
```

For more details, see `PM2_README.md`


