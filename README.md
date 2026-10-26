# GateVR - Logic Circuits VR

Virtual Reality educational application for learning logic circuits, developed for engineering students (Thesis Project - UPC). The project focuses on an interactive, guided experience covering theory, practice, simulation, and feedback without relying on generative AI or multiplayer components.

## Development Environment
* Engine: Unity 6 (6000.6.6f1)
* Render Pipeline: Universal Render Pipeline (URP)
* Target Platform: Android / Meta Quest 3
* Core SDK: Meta XR All-in-One SDK (v65+)
* XR Standard: OpenXR

## Clean Architecture Structure
The source code is isolated within the `Assets/_GateVR/` directory to prevent collisions with third-party packages. The project implements a strict Layered Architecture:

1. Presentation: UI, 3D models, scenes, and scripts coupled to Unity components (MonoBehaviours, XR Interactables). Namespace: `GateVR.Presentation`
2. Application: System use cases (Tutorial, Practice, Simulation, Validation) and flow orchestration. Namespace: `GateVR.Application`
3. Domain: Pure deterministic business logic (Truth tables, logic gate behavior) using native C# POCOs without `UnityEngine` dependencies. Namespace: `GateVR.Domain`
4. Data: Local persistence management using JSON format to save user progress. Namespace: `GateVR.Data`

## Project Setup & Configuration
1. Clone the repository and open the project using Unity Hub.
2. Navigate to `File > Build Profiles` and ensure the **Meta Quest** profile is set to Active.
3. The project is configured with Git LFS for large assets and a standard Unity `.gitignore`.

## Meta XR Simulator Installation (Testing without Headset)
To develop and test the VR mechanics on a PC without a physical Meta Quest headset, the Meta XR Simulator is required.

1. Download the installer directly from the official Meta portal:
   https://developers.meta.com/vr/downloads/package/meta-xr-simulator-windows/
2. Execute the `.msi` file and follow the Windows installation wizard.
3. Open the "Meta XR Simulator" desktop application.
4. Toggle the switch to enable "XR Simulator runtime".
5. Press "Play" in the Unity Editor. Use the WASD keys and the mouse to navigate the scene and emulate controller inputs.