module.exports = {
  apps: [
    {
      name: 'planar-motor-backend',
      script: 'backend/main.py',
      interpreter: '/Users/kelsorj/Tachyon/planar_motor/venv313/bin/python',
      cwd: '/Users/kelsorj/Tachyon/planar_motor',
      env: {
        PMC_IP: '192.168.10.100',
        PORT: 3062,
        MONO_GAC_PREFIX: '/opt/homebrew'
      },
      args: '--port 3062 --pmc-ip 192.168.10.100',
      autorestart: true,
      watch: false,
      max_memory_restart: '500M',
      error_file: './logs/planar-motor-backend-error.log',
      out_file: './logs/planar-motor-backend-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss Z'
    }
  ]
}

