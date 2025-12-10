#!/usr/bin/env python3
"""
Test runner script for scheduler framework tests.

Runs tests in batches to avoid thread-related hanging issues.
"""

import subprocess
import sys
import time
from pathlib import Path

# Test batches - organized by module
TEST_BATCHES = [
    {
        "name": "Worklist Tests",
        "files": ["tests/test_worklist.py"],
        "description": "Tests for Worklist, PlateTask, Transfer classes"
    },
    {
        "name": "Active Plate Tests",
        "files": ["tests/test_active_plate.py"],
        "description": "Tests for ActivePlate, PlateLocation, PlatePlace"
    },
    {
        "name": "Active Plate Factory Tests",
        "files": ["tests/test_active_plate_factory.py"],
        "description": "Tests for ActivePlateFactory implementations"
    },
    {
        "name": "Device Manager Tests",
        "files": ["tests/test_device_manager.py"],
        "description": "Tests for DeviceManager and device interfaces"
    },
    {
        "name": "Path Planner Tests",
        "files": ["tests/test_path_planner.py"],
        "description": "Tests for PathPlanner and graph algorithms"
    },
    {
        "name": "Robot Scheduler Tests",
        "files": ["tests/test_robot_scheduler.py"],
        "description": "Tests for RobotScheduler"
    },
    {
        "name": "Plate Scheduler Tests",
        "files": ["tests/test_plate_scheduler.py"],
        "description": "Tests for PlateScheduler (main orchestrator)"
    },
]

def run_batch(batch, verbose=False):
    """Run a batch of tests and return the result."""
    print(f"\n{'='*70}")
    print(f"Running: {batch['name']}")
    print(f"Description: {batch['description']}")
    print(f"{'='*70}\n")
    
    cmd = ["python3", "-m", "pytest"] + batch["files"]
    
    if verbose:
        cmd.append("-v")
    else:
        cmd.append("-q")
    
    cmd.extend(["--tb=short"])
    
    start_time = time.time()
    result = subprocess.run(cmd, capture_output=True, text=True)
    elapsed = time.time() - start_time
    
    # Print output
    if result.stdout:
        print(result.stdout)
    if result.stderr:
        print(result.stderr, file=sys.stderr)
    
    return {
        "name": batch["name"],
        "success": result.returncode == 0,
        "returncode": result.returncode,
        "elapsed": elapsed,
        "output": result.stdout,
        "error": result.stderr
    }

def main():
    """Main test runner."""
    import argparse
    
    parser = argparse.ArgumentParser(
        description="Run scheduler framework tests in batches",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python3 run_tests.py              # Run all tests quietly
  python3 run_tests.py -v           # Run all tests with verbose output
  python3 run_tests.py --batch 0   # Run only the first batch
  python3 run_tests.py --list      # List all test batches
        """
    )
    parser.add_argument(
        "-v", "--verbose",
        action="store_true",
        help="Run tests with verbose output"
    )
    parser.add_argument(
        "--batch",
        type=int,
        metavar="N",
        help="Run only batch number N (0-indexed)"
    )
    parser.add_argument(
        "--list",
        action="store_true",
        help="List all test batches and exit"
    )
    parser.add_argument(
        "--stop-on-failure",
        action="store_true",
        help="Stop running tests if a batch fails"
    )
    
    args = parser.parse_args()
    
    # Change to script directory
    script_dir = Path(__file__).parent
    os.chdir(script_dir)
    
    if args.list:
        print("Available test batches:\n")
        for i, batch in enumerate(TEST_BATCHES):
            print(f"  [{i}] {batch['name']}")
            print(f"      {batch['description']}")
            print(f"      Files: {', '.join(batch['files'])}")
            print()
        return 0
    
    # Determine which batches to run
    if args.batch is not None:
        if args.batch < 0 or args.batch >= len(TEST_BATCHES):
            print(f"Error: Batch number must be between 0 and {len(TEST_BATCHES) - 1}")
            return 1
        batches_to_run = [TEST_BATCHES[args.batch]]
    else:
        batches_to_run = TEST_BATCHES
    
    # Run tests
    print(f"\n{'='*70}")
    print(f"Scheduler Framework Test Runner")
    print(f"Running {len(batches_to_run)} batch(es)")
    print(f"{'='*70}\n")
    
    results = []
    total_start = time.time()
    
    for i, batch in enumerate(batches_to_run):
        result = run_batch(batch, verbose=args.verbose)
        results.append(result)
        
        if not result["success"]:
            print(f"\n❌ Batch '{result['name']}' FAILED (exit code: {result['returncode']})")
            if args.stop_on_failure:
                print("\nStopping due to --stop-on-failure flag")
                break
        else:
            print(f"\n✅ Batch '{result['name']}' PASSED ({result['elapsed']:.2f}s)")
    
    total_elapsed = time.time() - total_start
    
    # Print summary
    print(f"\n{'='*70}")
    print("TEST SUMMARY")
    print(f"{'='*70}\n")
    
    passed = sum(1 for r in results if r["success"])
    failed = len(results) - passed
    
    for result in results:
        status = "✅ PASSED" if result["success"] else "❌ FAILED"
        print(f"  {status:12} {result['name']:40} ({result['elapsed']:.2f}s)")
    
    print(f"\n{'='*70}")
    print(f"Total: {len(results)} batch(es) | Passed: {passed} | Failed: {failed}")
    print(f"Total time: {total_elapsed:.2f}s")
    print(f"{'='*70}\n")
    
    return 0 if failed == 0 else 1

if __name__ == "__main__":
    import os
    sys.exit(main())


