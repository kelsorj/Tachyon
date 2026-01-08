Architecture Considerations
1. Device Collections
I agree - a separate DeviceCollection entity makes sense. This enables:
Reusing the same "lab setup" across multiple workflows
Versioning collections independently from workflows
Easy swapping of test vs. production device sets
2. State Machine
A hierarchical state machine would work well:
Workflow States: IDLE → RUNNING → PAUSED → COMPLETED                              ↘ ERROR → RECOVERING                              Step States: PENDING → EXECUTING → COMPLETED                  ↘ SKIPPED    ↘ FAILED
Simulation mode would execute the same state transitions but skip actual device calls.
3. Visual Editor
React Flow is the standard choice for this - it's mature, handles drag-and-drop nodes, connections, and has good performance. It would let users:
Drag step nodes onto a canvas
Connect outputs to inputs
Visualize branches/conditionals
4. Data Model (YAML vs alternatives)
YAML is good for human readability and version control. However, for graph structures with branching, I'd suggest:
Hybrid approach:
Store internally as JSON (easy to manipulate in JS/Python)
Support YAML import/export for human editing and Git versioning
The graph structure maps naturally to both formats
A workflow could look like:
id: wf-transfer-001name: "Plate Transfer Protocol"device_collection_id: dc-lab-bench-1nodes:  - id: start    type: trigger    next: [pick-source]      - id: pick-source    type: device_action    device: PF400-021    action: pick_place    params:      pick: source_nest      place: transfer_position    next: [check-temp]    on_error: error-handler      - id: check-temp    type: conditional    condition: "devices['incubator'].temperature >= 37"    on_true: [incubate]    on_false: [wait-temp]      - id: wait-temp    type: code_module    module: wait_for_condition    params:      device: incubator      field: temperature      operator: ">="      value: 37    next: [incubate]      - id: incubate    type: device_action    ...
5. Code Modules
These could be:
Built-in modules (wait, loop, conditional, log, etc.)
Custom Python scripts stored in the DB or filesystem
Each module has defined inputs/outputs for type-safe connections
6. Backend Structure
MongoDB collections:
Collection	Purpose
device_collections	Groups of devices with aliases
workflows	Workflow definitions (the graph)
workflow_runs	Execution instances with state
code_modules	Reusable script definitions
Questions Before We Build
Execution engine location: Should workflows run on the backend (Python) or could some run client-side? (I'd recommend backend for reliability)
Real-time updates: Do you want live updates to the UI as steps execute? (WebSockets would enable this)
Concurrency: Can multiple workflows run simultaneously on the same device collection, or is it one-at-a-time with queueing?
Code modules: Python scripts, or should we support JavaScript too?
Let me know your thoughts on these, and we can start scaffolding the architecture!
Execution engine location: Should workflows run on the backend (Python) or could some run client-side? I think it should run on the backend so that it always is persisting regardless of what the user does on the frontend. With the architecture it's possible there will be several people that open up a frontend interface to see how the workflow is running.

Real-time updates: Do you want live updates to the UI as steps execute? Live updates would be ideal

Concurrency: Can multiple workflows run simultaneously on the same device collection, or is it one-at-a-time with queueing? Eventually we will want to work towards allowing multiple workflows to share the same devices but there are many complications involving timing/blocking of the devices to make sure the workflows execute successfully and without causing significant changes in timing.

Code modules: Python scripts, or should we support JavaScript too? I think supporting C#, Python, and JavaScript is a good place to start.

Excellent clarifications! Here's the refined architecture:
Final Architecture
┌─────────────────────────────────────────────────────────────────┐
│                        FRONTEND (React)                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │ Workflow    │  │ Collection  │  │ Run         │              │
│  │ Editor      │  │ Manager     │  │ Monitor     │              │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘              │
│         │                │                │                     │
│         └────────────────┼────────────────┘                     │
│                          │ REST API + WebSocket                 │
└──────────────────────────┼──────────────────────────────────────┘
                           │
┌──────────────────────────┼──────────────────────────────────────┐
│                     BACKEND (Python/FastAPI)                    │
│  ┌─────────────┐  ┌──────┴──────┐  ┌─────────────┐              │
│  │ Workflow    │  │ WebSocket   │  │ Code Module │              │
│  │ Engine      │  │ Hub         │  │ Executor    │              │
│  └──────┬──────┘  └─────────────┘  └──────┬──────┘              │
│         │                                 │                     │
│  ┌──────┴──────┐                  ┌───────┴───────┐             │
│  │ State       │                  │ Subprocess    │             │
│  │ Machine     │                  │ Python/JS/C#  │             │
│  └──────┬──────┘                  └───────────────┘             │
│         │                                                       │
│  ┌──────┴──────────────────────────────────────────┐            │
│  │              Device Clients (REST)              │            │
│  │   PF400  │  Planar Motor  │  Incubator  │ ...   │            │
│  └─────────────────────────────────────────────────┘            │
└─────────────────────────────────────────────────────────────────┘
                           │
┌──────────────────────────┼───────────────────────────────────────┐
│                     MongoDB                                      │
│  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐     │
│  │ device_    │ │ workflows  │ │ workflow_  │ │ code_      │     │
│  │ collections│ │            │ │ runs       │ │ modules    │     │
│  └────────────┘ └────────────┘ └────────────┘ └────────────┘     │
└──────────────────────────────────────────────────────────────────┘
Implementation Plan
Phase	Component	Description
1	Data Models	MongoDB schemas for collections, workflows, runs, modules
2	REST API	CRUD endpoints for all entities
3	Workflow Engine	State machine, step execution, device integration
4	WebSocket Hub	Real-time state broadcasts to all connected clients
5	Visual Editor	React Flow-based workflow builder
6	Code Modules	Python, JavaScript, C# subprocess execution
7	Concurrency	Resource locking, queue management (future)
