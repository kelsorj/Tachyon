#!/bin/bash
# Helper script with common PM2 commands

case "$1" in
    start)
        echo "Starting PF400 services with PM2 (development mode)..."
        pm2 start ecosystem.dev.config.js
        pm2 save
        ;;
    stop)
        echo "Stopping PF400 services..."
        pm2 stop all
        ;;
    restart)
        echo "Restarting PF400 services..."
        pm2 restart all
        ;;
    status)
        pm2 status
        ;;
    logs)
        if [ -z "$2" ]; then
            pm2 logs
        else
            pm2 logs "$2"
        fi
        ;;
    monitor)
        pm2 monit
        ;;
    delete)
        echo "Deleting all PM2 processes..."
        pm2 delete all
        ;;
    save)
        pm2 save
        ;;
    startup)
        echo "Setting up PM2 to start on system boot..."
        pm2 startup
        ;;
    *)
        echo "PM2 Helper Commands"
        echo "Usage: ./pm2-commands.sh [command]"
        echo ""
        echo "Commands:"
        echo "  start      - Start all services (development mode)"
        echo "  stop       - Stop all services"
        echo "  restart    - Restart all services"
        echo "  status     - Show status of all services"
        echo "  logs       - Show logs (optionally: logs [app-name])"
        echo "  monitor    - Open PM2 monitoring dashboard"
        echo "  delete     - Delete all PM2 processes"
        echo "  save       - Save current PM2 process list"
        echo "  startup    - Configure PM2 to start on boot"
        echo ""
        echo "Examples:"
        echo "  ./pm2-commands.sh start"
        echo "  ./pm2-commands.sh logs pf400-backend"
        echo "  ./pm2-commands.sh status"
        ;;
esac



