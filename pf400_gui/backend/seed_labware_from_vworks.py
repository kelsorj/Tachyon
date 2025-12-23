"""
Seed Labware Types from the VWorks labware library in `models/labware/`.

This script intentionally seeds JUST ONE entry (random or specified) so we can
iterate on the schema/parameter mapping. Once it looks right, we can scale to all.
"""

from __future__ import annotations

import argparse
import os
import random
from pathlib import Path
from typing import Any, Dict, Optional, Tuple

import db as mongodb


REPO_ROOT = Path(__file__).resolve().parents[2]
LABWARE_ROOT = REPO_ROOT / "models" / "labware"


def _infer_rows_cols(wells: int) -> Tuple[Optional[int], Optional[int]]:
    mapping = {
        6: (2, 3),
        24: (4, 6),
        48: (6, 8),
        96: (8, 12),
        384: (16, 24),
        1536: (32, 48),
    }
    return mapping.get(int(wells), (None, None))


def _parse_vworks_dat(path: Path) -> Dict[str, str]:
    """
    Parse VWorks .dat lines:
      "KEY"="VALUE"

    Some values (notably DESCRIPTION) can span multiple lines until a closing quote.
    """
    txt = path.read_text(errors="replace").splitlines()
    out: Dict[str, str] = {}

    current_key: Optional[str] = None
    current_val: str = ""

    for raw_line in txt:
        line = raw_line.rstrip("\n")

        if current_key is not None:
            # Continue multi-line value until a trailing quote terminates it.
            if line.endswith('"'):
                current_val += "\n" + line[:-1]
                out[current_key] = current_val
                current_key = None
                current_val = ""
            else:
                current_val += "\n" + line
            continue

        if not line.startswith('"') or '"="' not in line:
            continue

        try:
            key = line.split('"', 2)[1]
        except Exception:
            continue

        prefix = f"\"{key}\"=\""
        if not line.startswith(prefix):
            continue

        remainder = line[len(prefix):]
        if remainder.endswith('"'):
            out[key] = remainder[:-1]
        else:
            # Multi-line begins
            current_key = key
            current_val = remainder

    # If file ended mid-value, still store what we got.
    if current_key is not None:
        out[current_key] = current_val

    return out


def _to_float(d: Dict[str, str], key: str) -> Optional[float]:
    v = d.get(key)
    if v is None:
        return None
    try:
        return float(v)
    except Exception:
        return None


def _to_int(d: Dict[str, str], key: str) -> Optional[int]:
    v = d.get(key)
    if v is None:
        return None
    try:
        return int(float(v))
    except Exception:
        return None


def _to_bool01(d: Dict[str, str], key: str) -> Optional[bool]:
    v = d.get(key)
    if v is None:
        return None
    try:
        return bool(int(float(v)))
    except Exception:
        return None


def _map_base_class(v: Optional[int]) -> str:
    # VWorks `BASE_CLASS` appears numeric; we map the common values.
    mapping = {
        1: "microplate",
        2: "filter_plate",
        3: "reservoir",
        4: "tip_wash_station",
        5: "pin_tool",
        6: "tip_box",
        7: "lid",
        8: "tip_trash_bin",
        9: "assaymap_cartridge_rack",
    }
    return mapping.get(v or 1, "microplate")


def _map_handling_speed(v: Optional[int]) -> Optional[str]:
    # Many VWorks files use ROBOT_HANDLING_SPEED="2" where the legacy GUI shows "Fast".
    # We'll treat: 0=slow, 1=medium, 2=fast (best-effort).
    if v is None:
        return None
    return {0: "slow", 1: "medium", 2: "fast"}.get(v, None)


def pick_one_dat(subdir: Optional[str] = None, seed: Optional[int] = None) -> Path:
    if not LABWARE_ROOT.exists():
        raise SystemExit(f"Labware root not found: {LABWARE_ROOT}")

    if subdir:
        p = LABWARE_ROOT / subdir
        if not p.exists() or not p.is_dir():
            raise SystemExit(f"Subdir not found: {p}")
        dats = list(p.glob("*.dat"))
        if not dats:
            raise SystemExit(f"No .dat files found in: {p}")
        return dats[0]

    all_dats = [p for p in LABWARE_ROOT.rglob("*.dat") if p.is_file()]
    if not all_dats:
        raise SystemExit(f"No .dat files found under: {LABWARE_ROOT}")

    rng = random.Random(seed)
    return rng.choice(all_dats)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--subdir", default="", help="Optional: pick a specific subdir under models/labware/")
    ap.add_argument("--seed", type=int, default=None, help="Random seed (for repeatable selection)")
    args = ap.parse_args()

    dat_path = pick_one_dat(subdir=args.subdir.strip() or None, seed=args.seed)
    meta = _parse_vworks_dat(dat_path)

    wells = _to_int(meta, "NUMBER_OF_WELLS") or _to_int(meta, "NUMBER_OF_WELLS ")  # defensive
    if not wells:
        # fallback: parse leading number from filename/dir
        try:
            wells = int(dat_path.stem.split(" ", 1)[0])
        except Exception:
            wells = 96

    rows, cols = _infer_rows_cols(wells)

    name = (meta.get("NAME") or dat_path.stem).strip()
    mfg_part = (meta.get("MANUFACTURER_PART_NUMBER") or "").strip()

    # Very rough vendor guess: 2nd token in folder name (e.g. "96 Costar 3596 ...")
    vendor_guess = ""
    try:
        vendor_guess = dat_path.parent.name.split(" ", 2)[1].strip()
    except Exception:
        vendor_guess = ""

    thickness_mm = _to_float(meta, "THICKNESS")
    stacking_thickness_mm = _to_float(meta, "STACKING_THICKNESS")
    shim_thickness_mm = _to_float(meta, "SHIM_THICKNESS")
    robot_gripper_offset_mm = _to_float(meta, "ROBOT_GRIPPER_OFFSET")
    can_be_sealed = _to_bool01(meta, "CAN_BE_SEALED")
    sealed_thickness_mm = _to_float(meta, "SEALED_THICKNESS")
    sealed_stacking_thickness_mm = _to_float(meta, "SEALED_STACKING_THICKNESS")
    can_have_lid = _to_bool01(meta, "CAN_HAVE_LID")
    lidded_thickness_mm = _to_float(meta, "LIDDED_THICKNESS")
    lidded_stacking_thickness_mm = _to_float(meta, "LIDDED_STACKING_THICKNESS")
    lid_resting_height_mm = _to_float(meta, "LID_RESTING_HEIGHT")
    lid_departure_height_mm = _to_float(meta, "LID_DEPARTURE_HEIGHT")
    lower_plate_at_labeler = _to_bool01(meta, "LOWER_PLATE_AT_VCODE")
    can_mount = _to_bool01(meta, "CAN_MOUNT")
    can_be_mounted = _to_bool01(meta, "CAN_BE_MOUNTED")
    handling_speed = _map_handling_speed(_to_int(meta, "ROBOT_HANDLING_SPEED"))
    requires_insert = (meta.get("REQUIRES INSERT") or meta.get("REQUIRES_INSERT") or "").strip() or None
    filter_tip_pin_tool_length_mm = _to_float(meta, "FILTER_TIP_PIN_TOOL_LENGTH")
    filter_channel_resting_depth_mm = _to_float(meta, "FILTER_CHANNEL_RESTING_DEPTH")

    well_diam_mm = _to_float(meta, "WELL_DIAMETER")
    well_depth_mm = _to_float(meta, "WELL_DEPTH")
    pitch_x_mm = _to_float(meta, "X_WELL_TO_WELL")
    pitch_y_mm = _to_float(meta, "Y_WELL_TO_WELL")
    off_x_mm = _to_float(meta, "X_TEACHPOINT_TO_WELL")
    off_y_mm = _to_float(meta, "Y_TEACHPOINT_TO_WELL")
    well_geom = _to_int(meta, "WELL_GEOMETRY")
    well_bottom = _to_int(meta, "WELL_BOTTOM_SHAPE")

    description = (meta.get("DESCRIPTION") or "").strip()
    image_filename = (meta.get("IMAGE_FILENAME") or "").strip()
    base_class = _map_base_class(_to_int(meta, "BASE_CLASS"))

    doc: Dict[str, Any] = {
        "kind": "sbs_plate",
        "name": name,
        "vendor": vendor_guess,
        "catalog_number": mfg_part,
        "description": description,
        "base_class": base_class,
        "labware_class_ids": [],
        "wells": wells,
        "well_type": "",  # we'll derive later once we map WELL_GEOMETRY/BOTTOM_SHAPE
        "plate_dimensions_mm": {
            # SBS footprint defaults; VWorks .dat doesn't usually include L/W
            "length_mm": 127.76,
            "width_mm": 85.48,
            "height_mm": thickness_mm or 0.0,
        },
        "plate_properties": {
            "robot_gripper_offset_mm": robot_gripper_offset_mm,
            "empty_check_offset_mm": None,  # not mapped yet (needs confirmation)
            "thickness_mm": thickness_mm,
            "stacking_thickness_mm": stacking_thickness_mm,
            "shim_thickness_mm": shim_thickness_mm,
            "can_be_sealed": can_be_sealed,
            "sealed_thickness_mm": sealed_thickness_mm,
            "sealed_stacking_thickness_mm": sealed_stacking_thickness_mm,
            "can_have_lid": can_have_lid,
            "lidded_thickness_mm": lidded_thickness_mm,
            "lidded_stacking_thickness_mm": lidded_stacking_thickness_mm,
            "lid_resting_height_mm": lid_resting_height_mm,
            "lid_departure_height_mm": lid_departure_height_mm,
            "lower_plate_at_labeler": lower_plate_at_labeler,
            "can_mount": can_mount,
            "can_be_mounted": can_be_mounted,
            "max_robot_handling_speed": handling_speed,
            "filter_tip_pin_tool_length_mm": filter_tip_pin_tool_length_mm,
            "filter_channel_resting_depth_mm": filter_channel_resting_depth_mm,
            "requires_insert": requires_insert,
        },
        "well_dimensions_mm": {
            "diameter_mm": well_diam_mm,
            "depth_mm": well_depth_mm,
            "spacing_x_mm": pitch_x_mm,
            "spacing_y_mm": pitch_y_mm,
            "offset_x_mm": off_x_mm,
            "offset_y_mm": off_y_mm,
            "rows": rows,
            "cols": cols,
            "well_geometry": well_geom,
            "well_bottom_shape": well_bottom,
        },
        "model_3d": None,
        "notes": "Seeded from VWorks .dat; verify dimensions + map geometry/base class enums.",
        "source": {
            "system": "vworks",
            "dat_path": str(dat_path.relative_to(REPO_ROOT)),
            "folder": dat_path.parent.name,
            "image_filename": image_filename,
        },
        # Keep raw VWorks keys for traceability while we refine mappings
        "vworks_raw": meta,
    }

    created = mongodb.create_labware_type(doc)
    if not created:
        raise SystemExit("Failed to insert labware type (Mongo unavailable?)")

    print("Inserted labware type:")
    print(f"  id:     {created.get('labware_type_id')}")
    print(f"  name:   {created.get('name')}")
    print(f"  wells:  {created.get('wells')}")
    print(f"  vendor: {created.get('vendor')}")
    print(f"  cat#:   {created.get('catalog_number')}")
    print(f"  source: {created.get('source', {}).get('dat_path')}")


if __name__ == "__main__":
    main()


