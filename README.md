# TangibleTable

**A Unity/C# TUIO-based interactive surface application for tangible interfaces.**

TangibleTable is a framework for creating tangible user interfaces using the [TUIO protocol](http://www.tuio.org/). It enables the tracking and interaction with physical objects (pucks/markers) and touch inputs on interactive surfaces.

## Features

- **TUIO Protocol Support**: Full implementation of TUIO v1.1 for tracking cursors (touches) and objects (physical markers)
- **Cursor Tracking**: Track and visualize touch inputs with customizable stabilization
- **Puck Management**: Handle physical markers with object tracking and rotation
- **Touch UI Interaction**: Seamlessly interact with Unity UI elements using TUIO cursors
- **Extensible Framework**: Built with extensibility in mind for custom tangible interactions

## Usage

Place the TangibleTable prefabs in your scene and configure the TUIO connection settings. The system will automatically detect and track TUIO cursors and objects from the specified input source.

**Cursor Interaction**: TUIO cursors are automatically mapped to UI pointer events, allowing interaction with standard Unity UI elements.

**Puck Interaction**: Physical markers (pucks) are tracked with position and rotation, allowing for tangible interaction on the surface.

## Implementation

TangibleTable consists of several key components:

- **TuioCursorManager**: Handles touch input on the surface
- **TuioPuckManager**: Manages physical objects placed on the surface
- **TuioSettings**: Configures connection and stabilization settings
- **TuioVisualizer**: Visualizes TUIO elements with customizable appearance

## Requirements

- Unity 2021.3 or newer
- .NET Framework 4.6+

## Getting Started

1. Clone this repository
2. Open the project in Unity
3. Open the TUIO scene in Assets/Scenes
4. Configure the TUIO settings for your specific hardware
5. Run the project

## Dependencies

- TuioNet/Tuio11: TUIO protocol implementation
- TuioUnity: Unity integration for TUIO

## License

MIT License