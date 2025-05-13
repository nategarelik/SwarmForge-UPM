# SwarmForge Unity Package (UPM)

This package contains the SwarmForge Unity tool, an AI swarm orchestrator for automating game development tasks. It provides an Editor Window to interact with the SwarmForge system and a Backend Manager to help launch the necessary backend services from within Unity.

## Prerequisites

Before using this package, ensure you have the following:

1.  **Main SwarmForge Project:** You must have the main SwarmForge project (containing `orchestrator.py`, `python/mcp/Dockerfile`, etc.) cloned or downloaded to your local machine. This UPM package only contains the Unity Editor frontend and tools; it does **not** include the Python backend.
2.  **Docker Desktop:** Installed and running.
3.  **Python:** Python 3.8+ installed and accessible in your system's PATH.
4.  **Python Dependencies:** From your main SwarmForge project directory, install the required Python packages:
    ```bash
    pip install websockets fastapi uvicorn python-dotenv pinecone-client
    ```
5.  **Build `swarmforge-mcp` Docker Image (One-Time Setup):**
    Navigate to the `python/mcp` directory within your **main SwarmForge project** and build the Docker image. This only needs to be done once (or when the Dockerfile changes).
    ```bash
    cd path/to/your/SwarmForge_Main_Project/python/mcp
    docker build -t swarmforge-mcp .
    ```

## Installation

1.  Open your Unity project.
2.  Go to `Window > Package Manager`.
3.  Click the `+` icon in the top-left corner.
4.  Select `Add package from git URL...`.
5.  Enter the following URL: `https://github.com/nategarelik/SwarmForge#1.0.0` (or the latest version tag).
6.  Click `Add`.

## Launching the Backend Services (via Backend Manager)

The SwarmForge tool requires its backend services (MCP Docker Container and Python Orchestrator) to be running. You can manage these from within Unity using the Backend Manager.

1.  **Open Backend Manager:** In the Unity Editor, go to `SwarmForge > Backend Manager`.
2.  **Configure Project Path:**
    *   In the "Path to your main SwarmForge Project" field, enter the **absolute path** to the root directory of your cloned/downloaded main SwarmForge project (the one containing `orchestrator.py`).
    *   Click "Save Project Path". This path is saved locally for your Unity project.
3.  **Start MCP Docker Container:**
    *   Click the "Start MCP Docker Container (swarmforge-mcp)" button.
    *   This will attempt to run the `swarmforge-mcp` Docker container in detached mode, exposing port 8000.
    *   Check the Unity Console for output and any errors.
4.  **Start Python Orchestrator:**
    *   Click the "Start Python Orchestrator (orchestrator.py)" button.
    *   This will attempt to run the `orchestrator.py` script using your system's Python.
    *   The orchestrator will listen on `ws://localhost:8765`.
    *   Check the Unity Console for output and any errors.

**Note:** The Backend Manager attempts to start these processes. For detailed logs or if you encounter issues, you might still need to run these commands manually in your terminal to diagnose.

## Using the SwarmForge Tool

Once the backend services are running:

1.  Open the SwarmForge main window via `Window > SwarmForge` (or as previously named if different).
2.  The window should now be able to connect to the Python orchestrator (`ws://localhost:8765`).
3.  **Custom Modes:**
    *   Defined in `custom_modes.json` (located in your main SwarmForge project). The dropdown populates at startup; click **Run Mode** to execute commands.
4.  **Task Planning:**
    *   Enter a prompt in the top text box and click **Send**. Generated tasks or script JSON appear in the task list and log.

## Troubleshooting

*   **"SwarmForge Project Path is not set":** Ensure you've set and saved the correct path in the Backend Manager.
*   **"orchestrator.py not found":** Double-check the project path in the Backend Manager.
*   **Docker errors:** Ensure Docker Desktop is running. If the `swarmforge-mcp` container fails to start, try running it manually from your terminal to see more detailed errors: `docker run -d -p 8000:8000 swarmforge-mcp`.
*   **Python errors:** Ensure Python is in your PATH and all dependencies are installed in the environment Python is using. Try running `python orchestrator.py` manually from your main SwarmForge project directory.

This updated README should provide a much clearer path for users to get SwarmForge up and running!