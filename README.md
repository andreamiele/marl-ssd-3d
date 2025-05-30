# marl-ssd-3d

This project explores cooperative and competitive behaviors between predator agents (wolves) chasing a prey agent (goat) within a realistic Unity environment featuring obstacles and raycast-based vision.

## Project Overview

The WolfpackUnity ML-Agents project examines how agents learn and adapt behaviors of cooperation or defection through multi-agent reinforcement learning techniques. The goal is to analyze agent interactions in dynamic scenarios, providing insights into cooperative strategies and emergent behaviors.

## Project Structure

- **Assets/**
  - **Scenes/**: Contains two primary Unity scenes:
    - `train`: For training agents.
    - `inference`: For visualizing trained agents' behaviors.
  - **Scripts/**: Includes core C# scripts controlling agents and environment interactions:
    - `PreyAgent.cs`
    - `PredatorAgent.cs`
    - `EnvironmentController.cs`
    - `CameraView.cs`
    - `Radius.cs`
  - **ML-Agents/**: Contains trained neural network weights for agents.
  - Unity skins and prefabs for visual representation.

- **Packages/**: Unity-managed packages required for the environment.
- **ProjectSettings/**: Unity editor configurations and settings.
- **config/**: YAML configuration files for training agent behaviors using ML-Agents.
- `.collabignore`: Configuration for Unity Collaborate (ignores unnecessary files).
- `update_config.py`: Python script to streamline YAML configuration updates from the command line for efficient experimentation.

## Installation & Setup

### Prerequisites
- Unity Editor version `6000.1.1f1`
- Python installed with dependencies listed in `requirements.txt`

### Quick Start
1. Clone this repository.
2. Install Python dependencies:
   ```bash
   pip install -r requirements.txt
   ```
3. Open the Unity project located in the `WolfpackUnity` folder using Unity Editor.
4. Train agents using ML-Agents:
    ```bash
    mlagents-learn config/your_config.yaml --env your_train_env_build --run-id run_identifier --no-graphics
    ```
5. Once trained, run inference scene in Unity to observe agent behaviors.

### Usage
- Modify training parameters quickly by adjusting your YAML file or using the provided `update_config.py`:
    ```bash
    python update_config.py config/base_config.yaml config/new_config.yaml --param param_1_name=param_1_value --param param_2_name=param_2_value
    ```
- Experiment with different scenarios and observe cooperation or defection patterns between predator agents.
