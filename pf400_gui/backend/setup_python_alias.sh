#!/bin/bash
# Script to add python alias to zsh config

ZSHRC="$HOME/.zshrc"

# Check if alias already exists
if grep -q "alias python=" "$ZSHRC" 2>/dev/null; then
    echo "Python alias already exists in $ZSHRC"
    grep "alias python=" "$ZSHRC"
else
    # Add alias to zshrc
    echo "" >> "$ZSHRC"
    echo "# Python alias - map python3 to python" >> "$ZSHRC"
    echo "alias python=python3" >> "$ZSHRC"
    echo "✓ Added 'alias python=python3' to $ZSHRC"
    echo "Run 'source ~/.zshrc' or restart your terminal to use it"
fi


