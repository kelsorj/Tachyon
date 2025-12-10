#!/bin/bash
# Fix setup: add python alias and ensure venv is ready

echo "Fixing Python setup..."

# 1. Add python alias to zshrc
ZSHRC="$HOME/.zshrc"
if [ ! -f "$ZSHRC" ]; then
    touch "$ZSHRC"
fi

if ! grep -q "alias python=" "$ZSHRC" 2>/dev/null; then
    echo "" >> "$ZSHRC"
    echo "# Python alias - map python3 to python" >> "$ZSHRC"
    echo "alias python=python3" >> "$ZSHRC"
    echo "✓ Added 'alias python=python3' to $ZSHRC"
else
    echo "✓ Python alias already exists in $ZSHRC"
fi

# 2. Fix venv permissions if needed
if [ -d "venv" ]; then
    echo "✓ Virtual environment found"
    if [ -f "venv/bin/python3" ]; then
        chmod +x venv/bin/python3 2>/dev/null
        echo "✓ Fixed venv python3 permissions"
    fi
else
    echo "⚠ Virtual environment not found - creating one..."
    python3 -m venv venv
    source venv/bin/activate
    pip install --upgrade pip
    pip install -r requirements.txt
    echo "✓ Created and configured virtual environment"
fi

# 3. Verify fastapi is installed
if [ -d "venv" ]; then
    if venv/bin/python3 -c "import fastapi" 2>/dev/null; then
        echo "✓ FastAPI is installed in venv"
    else
        echo "⚠ FastAPI not found - installing..."
        source venv/bin/activate
        pip install -r requirements.txt
        echo "✓ Installed dependencies"
    fi
fi

echo ""
echo "Setup complete! Next steps:"
echo "  1. Run: source ~/.zshrc  (or restart terminal)"
echo "  2. Run: ./run_sxl.sh     (to start the server)"
echo ""


