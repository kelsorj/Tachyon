/**
 * PM2 Ecosystem Configuration - Development Mode
 * Auto-restarts on file changes for hot reload
 */

module.exports = {
  apps: [
    {
      name: 'pf400-backend',
      script: './backend/run_sxl.sh',
      interpreter: 'bash',
      cwd: './',
      instances: 1,
      exec_mode: 'fork',
      watch: true, // Auto-restart on file changes
      watch_delay: 1000,
      watch_options: {
        followSymlinks: false,
        usePolling: false
      },
      ignore_watch: [
        'node_modules',
        'logs',
        '*.log',
        '.git',
        'venv',
        '__pycache__',
        '*.pyc',
        'htmlcov',
        'frontend'
      ],
      env: {
        NODE_ENV: 'development',
        ROBOT_MODEL: '400SXL',
        PF400_PORT: '8091',
        PF400_IP: '192.168.0.20',
        PF400_ROBOT_PORT: '10100',
        PYTHONUNBUFFERED: '1'
      },
      error_file: './logs/backend-error.log',
      out_file: './logs/backend-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss Z',
      merge_logs: true,
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '500M'
    },
    {
      name: 'pf400-frontend',
      script: 'npm',
      args: 'run dev',
      cwd: './frontend',
      instances: 1,
      exec_mode: 'fork',
      watch: false, // Disabled - Vite has its own HMR (Hot Module Replacement)
      // Don't use PM2 watch with Vite - it causes restart loops
      env: {
        NODE_ENV: 'development',
        PORT: '5173',
        VITE_API_URL: 'http://localhost:8091'
      },
      error_file: './logs/frontend-error.log',
      out_file: './logs/frontend-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss Z',
      merge_logs: true,
      autorestart: true,
      max_restarts: 10,
      min_uptime: '10s',
      max_memory_restart: '500M'
    }
  ]
};

