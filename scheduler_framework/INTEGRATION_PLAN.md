# Tachyon Integration Plan: MADSci-Inspired Architecture

This document outlines how we'll integrate key concepts from MADSci (POC) into Tachyon's scheduler framework, adapting the architecture to fit our needs without copying code.

## Overview

MADSci provides a comprehensive microservices architecture for laboratory automation. We'll adapt its best concepts to enhance Tachyon's scheduler framework while maintaining our existing C#-inspired architecture.

## Key Concepts to Integrate

### 1. Node-Based Instrument Integration
**MADSci Concept**: Instruments implement a REST-based Node interface
**Tachyon Adaptation**: 
- Create a `NodeInterface` abstraction for instruments/robots
- Support REST-based communication (like PF400 backend)
- Allow instruments to register actions they can perform
- Enable dynamic discovery of instrument capabilities

**Implementation Plan**:
- Add `NodeInterface` to `device_manager.py`
- Create `RestNodeClient` for communicating with instrument backends
- Extend existing `RobotInterface` to support Node-style actions
- Add action discovery and validation

### 2. Workflow Management
**MADSci Concept**: YAML-based workflow definitions with steps, parameters, conditions
**Tachyon Adaptation**:
- Extend `Worklist` to support YAML workflow definitions
- Add step-based workflow execution (similar to MADSci's StepDefinition)
- Support workflow parameters and templating
- Add conditional step execution

**Implementation Plan**:
- Create `WorkflowDefinition` class (YAML-serializable)
- Add `WorkflowStep` class with conditions, parameters, locations
- Extend `PlateScheduler` to execute workflows step-by-step
- Add workflow parameter substitution (e.g., `${param_name}`)

### 3. Location Management
**MADSci Concept**: Multi-representation locations (different coordinate systems per robot)
**Tachyon Adaptation**:
- Extend `PlateLocation` to support multiple coordinate representations
- Add location references per robot/device
- Support location-to-resource attachments
- Enable location translation between coordinate systems

**Implementation Plan**:
- Add `LocationReference` class mapping device names to coordinates
- Extend `PlateLocation` with `references: Dict[str, Any]`
- Update path planner to use appropriate reference for each robot
- Add location manager service (optional, for complex labs)

### 4. Resource Management
**MADSci Concept**: Track labware, consumables, equipment, samples
**Tachyon Adaptation**:
- Extend `Plate` to be more resource-aware
- Add resource tracking (plates, tips, reagents)
- Support resource reservations and availability
- Track resource lifecycle (created, in-use, consumed, disposed)

**Implementation Plan**:
- Create `ResourceManager` class
- Add `Resource` base class with types (Plate, Tip, Reagent, etc.)
- Integrate with `DeviceManager` for resource availability
- Add resource reservation to scheduling logic

### 5. Event Management
**MADSci Concept**: Distributed event logging and querying
**Tachyon Adaptation**:
- Add structured event logging
- Support event querying and filtering
- Enable event-driven workflows
- Add event correlation for debugging

**Implementation Plan**:
- Create `EventManager` class
- Add event types (WorkflowStarted, StepCompleted, ErrorOccurred, etc.)
- Integrate logging with event system
- Add event query API (optional service)

### 6. Data Management
**MADSci Concept**: Store workflow results, files, datapoints
**Tachyon Adaptation**:
- Store workflow execution results
- Track step outputs and intermediate data
- Support file-based results
- Enable data retrieval for analysis

**Implementation Plan**:
- Create `DataManager` class
- Add `DataPoint` class for storing results
- Integrate with workflow execution to capture results
- Add data query API

### 7. ULID for IDs
**MADSci Concept**: Use ULID instead of UUID for better performance and sorting
**Tachyon Adaptation**:
- Replace UUID generation with ULID
- Use ULID for all resource IDs (plates, workflows, tasks)
- Benefit from lexicographical sorting

**Implementation Plan**:
- Implement a dependency-free ULID generator in `scheduler/ids.py`
- Use `new_ulid_str()` across core entities
- Update all ID generation to use ULID
- Update database schemas if needed

### 8. Manager Services Architecture
**MADSci Concept**: Microservices with FastAPI, each manager is independent
**Tachyon Adaptation**:
- Prefer microservices early:
  - Each device runs as its own FastAPI “Node” service (PF400 service, Planar service, etc.)
  - A Workcell/Scheduler service orchestrates Nodes via HTTP
  - Supporting manager services (Location, Resource, Event, Data) can start minimal and grow as needed

**Implementation Plan**:
- Define a Tachyon-owned Node HTTP contract (health/definition/actions)
- Implement a scheduler/workcell service (FastAPI) that:
  - accepts workflows
  - resolves locations/resources
  - dispatches step actions to Nodes
- Add health + definition endpoints to each service
- Add service discovery via config first; registry later (optional)

### 9. Distributed Execution (why/why not)

**What it means in Tachyon**: the scheduler can run steps on multiple machines/services concurrently (and keep going if one process restarts), rather than being a single in-process orchestration loop.

**Pros**:
- **Fault isolation**: a PF400 Node crash doesn’t take down the scheduler or other devices.
- **Horizontal scaling**: multiple workcells/robots can be scheduled in parallel, and heavy compute (planning/vision/analysis) can run elsewhere.
- **Clear ownership boundaries**: each device team/service owns its own API and behavior.
- **Operational visibility**: service-level logs/health checks make “what’s broken” obvious.
- **Upgradability**: you can deploy/roll back one device service without redeploying everything.

**Cons / costs**:
- **Complexity tax**: networking, retries, timeouts, idempotency, versioning, and partial failures become your daily reality.
- **State consistency**: you must decide which service is the source of truth for plate location/resource state, and handle race conditions.
- **Debugging is harder**: a single “move plate” becomes many events across services; you need correlation IDs (ULIDs help).
- **Latency & flakiness**: network delays and transient failures can slow or derail workflows unless designed for it.
- **Testing burden**: integration tests become “bring up a lab” tests (compose/k8s), not just unit tests.

**Recommended approach** (best of both):
- **Microservices for devices** (Nodes) + **one Workcell/Scheduler service** as the orchestrator.
- Add **distributed execution** by pushing work onto a queue (later) and making actions **idempotent** from day 1.
- Keep **state authoritative** in one place initially (Workcell service + DB), and let Nodes be “actuators” that report state.

### 9. Scheduler Architecture
**MADSci Concept**: Pluggable schedulers (FIFO, priority-based, etc.)
**Tachyon Adaptation**:
- Make scheduler pluggable
- Support different scheduling strategies
- Add priority-based scheduling
- Enable custom scheduler implementations

**Implementation Plan**:
- Create `AbstractScheduler` interface
- Refactor `PlateScheduler` to use scheduler interface
- Implement FIFO scheduler (current behavior)
- Add priority scheduler
- Enable scheduler selection via configuration

### 10. Client Libraries
**MADSci Concept**: Client classes for programmatic service interaction
**Tachyon Adaptation**:
- Create `TachyonClient` for programmatic workflow submission
- Support both direct Python API and REST client
- Enable remote workflow submission
- Add result retrieval helpers

**Implementation Plan**:
- Create `TachyonClient` class
- Add workflow submission methods
- Add status querying methods
- Add result retrieval methods
- Support both sync and async operations

## Implementation Phases

### Phase 1: Foundation (Current)
- ✅ Basic scheduler framework
- ✅ Multi-robot handoff support
- ✅ Wait task support
- 🔄 ULID integration
- 🔄 Node interface abstraction

### Phase 2: Workflow Enhancement
- Workflow YAML definitions
- Step-based execution
- Parameter templating
- Conditional steps

### Phase 3: Location & Resource Management
- Multi-representation locations
- Resource tracking
- Resource reservations
- Location manager

### Phase 4: Services & APIs
- Event management
- Data management
- REST API endpoints
- Client libraries

### Phase 5: Advanced Features
- Pluggable schedulers
- Service discovery
- Distributed execution
- Advanced monitoring

## Architecture Decisions

### What We're Keeping from Tachyon
- C#-inspired scheduler architecture (PlateScheduler, RobotScheduler)
- ActivePlate concept
- Path planning with Dijkstra's algorithm
- Worklist-based workflow definition
- Thread-based execution model

### What We're Adopting from MADSci
- Node-based instrument interface
- YAML workflow definitions
- Multi-representation locations
- ULID for IDs
- Structured event logging
- Resource management
- Client libraries

### What We're Adapting
- Workflow execution: Keep worklist-based but add step-based execution
- Service architecture: Monolithic first, microservices later if needed
- Scheduler: Make pluggable but keep current FIFO as default
- Location management: Add multi-representation but keep existing structure

## File Structure

```
scheduler_framework/
├── scheduler/
│   ├── __init__.py
│   ├── plate_scheduler.py          # Enhanced with workflow support
│   ├── robot_scheduler.py          # Enhanced with node interface
│   ├── path_planner.py             # Enhanced with multi-representation locations
│   ├── active_plate.py             # Enhanced with resource tracking
│   ├── worklist.py                 # Enhanced with YAML workflow definitions
│   ├── device_manager.py           # Enhanced with node interface
│   ├── handoff_location.py         # ✅ Already added
│   ├── node_interface.py           # NEW: Node-based instrument interface
│   ├── workflow_definition.py      # NEW: YAML workflow definitions
│   ├── location_manager.py         # NEW: Multi-representation locations
│   ├── resource_manager.py         # NEW: Resource tracking
│   ├── event_manager.py            # NEW: Event logging
│   ├── data_manager.py             # NEW: Result storage
│   └── utils.py                    # NEW: ULID, helpers
├── clients/
│   └── tachyon_client.py           # NEW: Client library
├── examples/
│   ├── multi_robot_handoff.py      # ✅ Already added
│   ├── workflow_example.py          # NEW: YAML workflow example
│   └── node_integration.py         # NEW: Node interface example
└── docs/
    ├── INTEGRATION_PLAN.md         # This file
    ├── NODE_INTERFACE.md            # NEW: Node interface guide
    ├── WORKFLOW_DEFINITIONS.md     # NEW: Workflow YAML guide
    └── LOCATION_MANAGEMENT.md       # NEW: Location guide
```

## Next Steps

1. **Review and approve this plan**
2. **Start with Phase 1 completion**:
   - Add ULID support
   - Create NodeInterface abstraction
3. **Implement Phase 2**:
   - YAML workflow definitions
   - Step-based execution
4. **Iterate based on feedback**

## Questions to Consider

1. Do we want to keep the monolithic scheduler or move to microservices immediately?
2. Should we use FastAPI for REST endpoints or keep it Python-only initially?
3. How important is backward compatibility with existing worklist format?
4. Do we need distributed execution or is single-process sufficient?
5. What's the priority order for these features?

