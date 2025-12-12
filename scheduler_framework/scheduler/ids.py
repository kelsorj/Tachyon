"""
ULID utilities for Tachyon.

We use ULIDs for:
- globally unique identifiers
- lexicographical sorting (time-ordered)
- easier distributed generation (no coordination required)

This implementation is dependency-free (standard library only).
"""

from __future__ import annotations

import os
import time
from typing import Final

_CROCKFORD_BASE32_ALPHABET: Final[str] = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"
_CROCKFORD_DECODE: Final[dict[str, int]] = {c: i for i, c in enumerate(_CROCKFORD_BASE32_ALPHABET)}


def _encode_crockford_base32(value: int, length: int) -> str:
    """Encode integer to fixed-length Crockford Base32 string."""
    chars = ["0"] * length
    for i in range(length - 1, -1, -1):
        chars[i] = _CROCKFORD_BASE32_ALPHABET[value & 0x1F]
        value >>= 5
    return "".join(chars)


def new_ulid_str() -> str:
    """
    Generate a ULID string (26 chars, Crockford Base32).

    Layout:
    - 48 bits: timestamp (ms since epoch)
    - 80 bits: randomness
    """
    ts_ms = int(time.time() * 1000) & ((1 << 48) - 1)
    rand = int.from_bytes(os.urandom(10), "big")  # 80 bits
    return _encode_crockford_base32(ts_ms, 10) + _encode_crockford_base32(rand, 16)


def is_valid_ulid(value: str) -> bool:
    """Basic ULID format validation (length + alphabet)."""
    if not isinstance(value, str) or len(value) != 26:
        return False
    try:
        for ch in value.upper():
            if ch not in _CROCKFORD_DECODE:
                return False
        return True
    except Exception:
        return False


