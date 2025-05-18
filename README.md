
# Mixed Reality Industrial Experience (MRIE) 🚀

An innovative Mixed Reality (MR) application designed to revolutionize industrial training, monitoring, and operations by bridging the gap between physical and virtual worlds.

![Screenshot 2025-05-18 142055](https://github.com/user-attachments/assets/f4397c3f-5d18-4a26-b2a8-5695a7eb2039)

---

## 🌟 Features

### Interactive Control Panel

* Monitor real-time machine data such as connection status, emergency/reset button states, RFID readings, and safety door statuses.
* Access and interact with data from various Festo factory stations including the robot arm, camera station, and more.

![Screenshot 2025-05-18 142134](https://github.com/user-attachments/assets/a6ff7b2c-4a32-453b-99c1-ca059bbf48dc)


![Screenshot 2025-05-18 142239](https://github.com/user-attachments/assets/7a0d8bcd-0025-488a-80c5-2c9beec07fe7)


![Screenshot 2025-05-18 142327](https://github.com/user-attachments/assets/1bee34e7-483c-42b1-8300-b6e4efec2123)


  

### Digital Twin Mode

* Fully virtual industrial environment with realistic visuals and immersive soundscapes.
* Interactive panels and emergency/reset buttons that simulate real industrial functionality.
* Floating UI control panel for enhanced accessibility and user interaction.

![Screenshot 2025-03-31 165659](https://github.com/user-attachments/assets/782e3f91-59aa-4b5d-ba8c-3a12cbad6919)


![Screenshot 2025-05-18 142820](https://github.com/user-attachments/assets/0065e067-e857-4453-a3c6-12029004554c)


### Live Camera Streaming

* Stream real-time footage from Robotino robot cameras, quality control cameras, and inspection cameras.
* Powered by MJPG streaming for seamless, low-latency video feed integration.

  

---

## 🔧 Technologies Used

* **Unity** – Application development and scene management.
* **XR Interaction Toolkit** & **OVR SDK** – For mixed reality and Oculus integration.
* **OPC Nodes** – Real-time data integration from Festo factory machines.
* **C# Scripting** – Custom functionality including camera streaming, UI interactions, and system controls.

---

## 💡 Challenges & Solutions

* **Oculus Integration Package**
  Developed custom grab-and-move systems for UI panels and addressed XR rig limitations to improve user interaction.

* **Panel Alignment Issues**
  Debugged disappearing UI panels with iterative testing and persistent design adjustments to ensure stable panel positioning.

* **Live Camera Streaming**
  Successfully implemented MJPG streaming to provide smooth, real-time camera feeds inside the MR environment.

* **Android Build Stability**
  Resolved manifest corruption and system crashes to deliver a stable Android build ready for deployment.

---

## 🚀 Getting Started

### Prerequisites

* Unity (version XX or later)
* Oculus software and hardware setup
* Access to Festo factory OPC server for real-time data (optional for testing)

### Installation

1. Clone the repository:

   ```bash
    git clone https://github.com/AakashMenon007/Industry4.0_FestoMachine.git
   ```
2. Open the project in Unity.
3. Configure OPC node connections and camera stream URLs in the settings panel.
4. Build and run on your target device (Oculus Quest recommended).

---

## 📂 Project Structure

```
/Assets
  /Scripts         # C# scripts for UI, data integration, and camera streaming
  /Scenes          # Unity scenes for Digital Twin and Control Panel modes
  /Prefabs         # Reusable UI panels and interactive elements
  /Streaming       # MJPG streaming handlers and camera feed implementations
  /XR             # XR Interaction Toolkit & Oculus SDK assets and setups
/Docs              # Documentation and technical notes
```

---

## 🤝 Contribution

Contributions are welcome! Please submit pull requests or open issues for any bugs or feature requests.

---

## 📄 License

[MIT License](LICENSE)

---
