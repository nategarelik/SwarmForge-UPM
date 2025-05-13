# SwarmForge Unity Tool

This repository contains the Unity Editor frontend and tools for the SwarmForge AI swarm orchestrator. SwarmForge is designed to automate various game development tasks by leveraging AI agents orchestrated by a Python backend.

This README provides a high-level overview and essential setup steps. For detailed information, please refer to the comprehensive documentation in the [`docs/`](docs/) directory.

## Documentation Structure

The project documentation is organized as follows:

*   [`README.md`](README.md): High-level overview, quick setup, and links to detailed documentation.
*   [`docs/setup_guide.md`](docs/setup_guide.md): Detailed prerequisites, installation instructions, and guidance on launching backend services.
*   [`docs/usage_guide.md`](docs/usage_guide.md): Comprehensive guide on using the SwarmForge Unity Editor window, including custom modes, task planning, and viewing results.
*   [`docs/architecture_overview.md`](docs/architecture_overview.md): High-level explanation of the SwarmForge system architecture, covering the Unity Editor Plugin, Python Backend, Orchestration, and MCP.
*   [`docs/websocket_plugin_architecture.md`](docs/websocket_plugin_architecture.md): Detailed design document for the WebSocket communication between the Unity Editor and the Python backend, including message formats and security considerations.
*   [`docs/component_breakdown.md`](docs/component_breakdown.md): Detailed descriptions of key scripts, modules, and their functionalities in both the Unity and Python parts of the project.
*   [`docs/api_reference.md`](docs/api_reference.md): Reference for the WebSocket API, detailing message types, data structures, and endpoints.
*   [`docs/troubleshooting.md`](docs/troubleshooting.md): Common issues and their solutions.

## Quick Setup

For detailed setup instructions, please see the [`docs/setup_guide.md`](docs/setup_guide.md).

1.  **Prerequisites:**
    *   Main SwarmForge Project (containing `orchestrator.py`, `python/mcp/Dockerfile`, etc.)
    *   Docker Desktop (Installed and running)
    *   Python 3.8+
    *   Required Python Dependencies (`websockets`, `fastapi`, `uvicorn`, `python-dotenv`, `pinecone-client`)
    *   Built `swarmforge-mcp` Docker Image

2.  **Installation:**
    *   Open your Unity project.
    *   Go to `Window > Package Manager`.
    *   Click `+` and select `Add package from git URL...`.
    *   Enter the package URL: `https://github.com/nategarelik/SwarmForge#1.0.0` (or the latest version tag).

3.  **Launch Backend Services:**
    *   In Unity, open `SwarmForge > Backend Manager`.
    *   Set and save the **absolute path** to your main SwarmForge Project.
    *   Click "Start MCP Docker Container (swarmforge-mcp)".
    *   Click "Start Python Orchestrator (orchestrator.py)".

## Basic Usage

For detailed usage instructions, please see the [`docs/usage_guide.md`](docs/usage_guide.md).

1.  Ensure backend services are running via the Backend Manager.
2.  Open the SwarmForge window: `Window > SwarmForge`.
3.  The window should connect to the orchestrator.
4.  Select a Custom Mode from the dropdown or enter a prompt in the text box.
5.  Click "Run Mode" or "Send" respectively.
6.  Observe task updates and log output in the window.

## Contributing

Information on contributing to the SwarmForge project can be found in the documentation.

## License

[Include License Information Here]