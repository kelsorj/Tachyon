# PM2 Setup and Usage Guide

This guide explains how to run the PF400 GUI (frontend + backend) using PM2 process manager.

## Quick Start

### 1. Run Setup Script

```bash
cd pf400_gui
./setup_pm2.sh
```

This will:
- Install Node.js (if needed)
- Install PM2 globally
- Install frontend dependencies
- Fix port configuration
- Set up logs directory

### 2. Start Services

```bash
# Start in development mode (with auto-restart on file changes)
pm2 start ecosystem.dev.config.js

# Or use the helper script
./pm2-commands.sh start
```

### 3. Check Status

```bash
pm2 status
# or
./pm2-commands.sh status
```

## PM2 Commands

### Using the Helper Script

```bash
./pm2-commands.sh [command]
```

Available commands:
- `start` - Start all services
- `stop` - Stop all services
- `restart` - Restart all services
- `status` - Show status
- `logs` - Show logs (optionally: `logs pf400-backend` or `logs pf400-frontend`)
- `monitor` - Open PM2 monitoring dashboard
- `delete` - Delete all processes
- `save` - Save current process list
- `startup` - Configure PM2 to start on system boot

### Direct PM2 Commands

```bash
# Start services
pm2 start ecosystem.dev.config.js      # Development mode (auto-restart)
pm2 start ecosystem.config.js          # Production mode (no auto-restart)

# View status
pm2 status
pm2 list

# View logs
pm2 logs                    # All logs
pm2 logs pf400-backend      # Backend only
pm2 logs pf400-frontend     # Frontend only

# Control processes
pm2 stop all
pm2 stop pf400-backend
pm2 restart all
pm2 restart pf400-frontend

# Monitoring
pm2 monit                   # Real-time monitoring dashboard

# Save and startup
pm2 save                    # Save current process list
pm2 startup                 # Generate startup script
pm2 unstartup               # Remove startup script
```

## Configuration Files

### `ecosystem.dev.config.js`
Development mode configuration:
- Auto-restart on file changes (`watch: true`)
- Development environment variables
- Detailed logging

### `ecosystem.config.js`
Production mode configuration:
- No auto-restart (`watch: false`)
- Production environment variables
- Optimized logging

## Services

### pf400-backend
- **Script**: `./backend/run_sxl.sh`
- **Port**: 3061 (configurable via `PF400_PORT`)
- **Model**: 400SXL (configurable via `ROBOT_MODEL`)
- **Logs**: `./logs/backend-out.log`, `./logs/backend-error.log`

### pf400-frontend
- **Script**: `npm run dev` (Vite dev server)
- **Port**: 5173 (Vite default)
- **API URL**: `http://localhost:3061`
- **Logs**: `./logs/frontend-out.log`, `./logs/frontend-error.log`

## Environment Variables

### Backend
- `ROBOT_MODEL`: Robot model (400SX or 400SXL, default: 400SXL)
- `PF400_PORT`: Backend port (default: 3061)
- `PYTHONUNBUFFERED`: Set to 1 for real-time logs

### Frontend
- `PORT`: Frontend dev server port (default: 5173)
- `VITE_API_URL`: Backend API URL (default: http://localhost:3061)

## Logs

All logs are stored in the `./logs/` directory:
- `backend-out.log` - Backend stdout
- `backend-error.log` - Backend stderr
- `frontend-out.log` - Frontend stdout
- `frontend-error.log` - Frontend stderr

View logs in real-time:
```bash
pm2 logs                    # All logs
pm2 logs pf400-backend      # Backend only
tail -f logs/backend-out.log  # Direct file access
```

## Troubleshooting

### Services won't start
1. Check if ports are in use:
   ```bash
   lsof -i :3061  # Backend
   lsof -i :5173  # Frontend
   ```

2. Check PM2 logs:
   ```bash
   pm2 logs
   ```

3. Check individual service:
   ```bash
   pm2 describe pf400-backend
   pm2 describe pf400-frontend
   ```

### Backend issues
- Ensure virtual environment is set up: `cd backend && source venv/bin/activate`
- Check Python dependencies: `pip install -r requirements.txt`
- Verify robot connection settings

### Frontend issues
- Ensure dependencies are installed: `cd frontend && npm install`
- Check if Vite is running: `lsof -i :5173`
- Verify API URL matches backend port

### PM2 not found
```bash
npm install -g pm2
```

### Port conflicts
Update ports in:
- `ecosystem.dev.config.js` - PM2 config
- `frontend/src/App.jsx` - Frontend API URL
- `backend/main.py` - Backend port (or use `PF400_PORT` env var)

## Auto-start on Boot

To start services automatically when system boots:

```bash
# Generate startup script
pm2 startup

# Follow the instructions it prints, then:
pm2 save
```

## Development Workflow

1. **Start services**:
   ```bash
   pm2 start ecosystem.dev.config.js
   ```

2. **Make changes** - PM2 will auto-restart on file changes (if watch is enabled)

3. **View logs**:
   ```bash
   pm2 logs
   ```

4. **Stop services**:
   ```bash
   pm2 stop all
   ```

## Production Deployment

For production, use `ecosystem.config.js`:
```bash
pm2 start ecosystem.config.js
pm2 save
pm2 startup
```

## Additional Resources

- [PM2 Documentation](https://pm2.keymetrics.io/docs/usage/quick-start/)
- [PM2 Ecosystem File](https://pm2.keymetrics.io/docs/usage/application-declaration/)



