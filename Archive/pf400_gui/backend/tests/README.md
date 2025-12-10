# Backend Tests

This directory contains unit tests for the PF400 backend API.

## Running Tests

### Install Dependencies

```bash
pip install -r requirements.txt
```

### Run All Tests

```bash
pytest
```

### Run Specific Test File

```bash
pytest tests/test_main.py
pytest tests/test_ros_client.py
pytest tests/test_sim_driver.py
```

### Run with Coverage

```bash
pytest --cov=. --cov-report=html
```

Coverage report will be generated in `htmlcov/index.html`.

### Run Verbose

```bash
pytest -v
```

## Test Structure

- `test_main.py`: Tests for FastAPI endpoints and SimClient
- `test_ros_client.py`: Tests for ROS client implementation
- `test_sim_driver.py`: Tests for MuJoCo simulator driver
- `conftest.py`: Shared fixtures and test configuration

## Writing New Tests

When adding new tests:

1. Follow the naming convention: `test_*.py` for files, `test_*` for functions
2. Use pytest fixtures from `conftest.py` when possible
3. Mock external dependencies (ROS, MuJoCo) to keep tests fast and isolated
4. Add docstrings to test classes and methods
5. Mark slow tests with `@pytest.mark.slow`

## Continuous Integration

Tests should pass before merging PRs. The CI pipeline runs:

```bash
pytest --cov=. --cov-report=term-missing
```

