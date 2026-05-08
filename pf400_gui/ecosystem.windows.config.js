/**
 * PM2 Ecosystem Configuration - Windows (development)
 *
 * Equivalent of ecosystem.dev.config.js, adapted for Windows:
 *  - Calls the venv python directly (no bash run_sxl.sh)
 *  - Invokes vite via node (PM2 on Windows can't fork .cmd shims)
 *  - watch is disabled on the backend; restart manually with
 *    `pm2 restart pf400-backend` after backend code changes.
 */

const path = require('path');
const backendDir = path.join(__dirname, 'backend');
const venvPython = path.join(backendDir, 'venv', 'Scripts', 'python.exe');

module.exports = {
  apps: [
    {
      name: 'pf400-backend',
      script: 'start_server.py',
      args: '--real --port 8091',
      interpreter: venvPython,
      cwd: backendDir,
      instances: 1,
      exec_mode: 'fork',
      watch: false,
      env: {
        NODE_ENV: 'development',
        ROBOT_MODEL: '400SXL',
        DEVICE_NAME: 'PF400-021',
        PF400_PORT: '8091',
        PF400_IP: '192.168.0.20',
        PF400_ROBOT_PORT: '10100',
        PYTHONUNBUFFERED: '1',
        PYTHONIOENCODING: 'utf-8'
      },
      error_file: './logs/backend-error.log',
      out_file:   './logs/backend-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss Z',
      merge_logs: true,
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '500M'
    },
    {
      name: 'pf400-frontend',
      script: path.join(__dirname, 'frontend', 'node_modules', 'vite', 'bin', 'vite.js'),
      args: '--host',
      cwd: path.join(__dirname, 'frontend'),
      instances: 1,
      exec_mode: 'fork',
      watch: false,
      env: {
        NODE_ENV: 'development',
        PORT: '5173',
        VITE_API_URL: 'http://localhost:8091'
      },
      error_file: './logs/frontend-error.log',
      out_file:   './logs/frontend-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss Z',
      merge_logs: true,
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '500M'
    }
  ]
};
