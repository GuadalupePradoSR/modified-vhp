# Virtual Human Project (VHP) - Context-Aware & Biomechanically Constrained Gaze Extension

*Original VHP Version: 0.2.1 (Beta) - 05/10/2025*  
*Original Contact: Geoffrey Gorisse, PhD, geoffrey.gorisse@gmail.com*  
*Modified Version Author: Guadalupe Prado Saldanha Ribeiro (Gira LAB / Universidade de Fortaleza - Unifor)*  
*Associated Publication:* **Biomechanically Constrained Gaze Control for NPCs in Unity: A VHP-Based Approach with Context-Aware Target Prioritization** (XXV Brazilian Symposium on Games and Digital Entertainment - SBGames 2026, Track: Computing)

---

## 📖 Project Overview

The Virtual Human Project (VHP) offers developers an optimized, scalable, and easy-to-integrate package to procedurally animate realistic virtual humans in Unity. 

This repository is a **modified fork** of the original VHP toolkit. It introduces a gaze-control extension designed to improve the plausibility of NPC gaze through biomechanical constraints, binocular coherence, and context-aware target prioritization.

---

## 🛠️ Custom Modifications in this Version

In digital games, NPC gaze and aiming behavior often remain mechanically rigid, visually inconsistent, or weakly connected to scene context. To address this, the original VHP gaze pipeline was extended with the following features:

### 1. Biomechanical Eye Clamping
* **What it does:** Restricts eye rotations according to configurable horizontal (yaw) and vertical (elevation/depression) comfort limits (typically $15^{\circ}$ to $25^{\circ}$) based on human ocular ranges. This prevents anatomically unsafe mesh deformations.
* **Head-Eye Coordination:** By clamping the eyes to a comfort range, the system naturally encourages the Head/Neck Inverse Kinematics (IK) system to participate when tracking extreme off-axis targets.
* **Modified File:** [`VHPGaze.cs`](Assets/Virtual%20Human%20Project/Scripts/VHPScripts/VHPGaze.cs)

### 2. Unified Focal-Center Correction
* **What it does:** Computes the gaze direction from a "unified focal center" (the midpoint between the two eyes) rather than calculating separate per-eye look directions. This single direction vector is clamped once and converted into a shared virtual target for both eyes, preventing binocular divergence (cross-eye artifacts / strabismus).
* **Focal Safety:** The reconstructed target is projected beyond a minimum focal distance to prevent excessive convergence when objects get too close to the NPC's face.
* **Modified File:** [`VHPGaze.cs`](Assets/Virtual%20Human%20Project/Scripts/VHPScripts/VHPGaze.cs)

### 3. Context-Aware Target Prioritization (Sentimental Attention)
* **What it does:** Introduces a lightweight semantic-relevance gaze manager that hooks into the VHP's probabilistic target scoring system.
* **Result:** If a detected target in the interest field belongs to a developer-defined list of high-priority objects, its score is boosted using a base offset and a multiplier: `(currentWeight + 50f) * sentimentalMultiplier`. This biases the NPC's attention toward authored elements without replacing the original probabilistic pipeline.
* **Modified Files:** 
  * [`VHPGazeTarget.cs`](Assets/Virtual%20Human%20Project/Scripts/VHPScripts/VHPGazeTarget.cs) (declares and triggers the `OnCalculateTargetWeight` event hook)
  * [`SentimentalGazeManager.cs`](Assets/Virtual%20Human%20Project/Scripts/VHPScripts/SentimentalGazeManager.cs) (subscribes to the event and applies the priority weight multiplier)

---

## 📂 Key Modified & Added Files (Evaluator Links)

To facilitate the evaluation process, here are the links to the key files containing our modifications and additions:

1. 📄 **[`VHPGaze.cs`](Assets/Virtual%20Human%20Project/Scripts/VHPScripts/VHPGaze.cs)**: Implements eye bone rotation controls, the unified focal-center strategy, the `ClampGazeDirection` method, and the biomechanical limits inspector properties.
2. 📄 **[`VHPGazeTarget.cs`](Assets/Virtual%20Human%20Project/Scripts/VHPScripts/VHPGazeTarget.cs)**: Calculates distance, sound, and movement ponderations for targets, and triggers the `OnCalculateTargetWeight` delegate event for weight overrides.
3. 📄 **[`SentimentalGazeManager.cs`](Assets/Virtual%20Human%20Project/Scripts/VHPScripts/SentimentalGazeManager.cs)**: *[New File]* Implements the Sentimental Attention logic, mapping prioritized objects and hooking into the target ponderation loop.
4. 📄 **[`VHPGazeInterestField.cs`](Assets/Virtual%20Human%20Project/Scripts/VHPScripts/VHPGazeInterestField.cs)**: Manages collision triggers and maintains the list of candidate targets currently within the NPC's field of interest.
5. 📄 **[`VHPEmotions.cs`](Assets/Virtual%20Human%20Project/Scripts/VHPScripts/VHPEmotions.cs)**: Handles procedural blend shape control for facial expressions of basic emotions (anger, disgust, fear, happiness, sadness, surprise).

---

## ⚙️ How to Configure the New Parameters

The extension exposes parameters directly in the Unity Inspector, making it easy to calibrate different character rigs without editing the source code:

### Core Gaze & Clamping (`VHPGaze.cs` Inspector Properties)
* **Maximum Yaw Limit:** Comfort range limit for horizontal rotation (left/right).
* **Maximum Elevation Limit:** Comfort range limit for looking upwards.
* **Maximum Depression Limit:** Comfort range limit for looking downwards.
* **Minimum Focal Distance:** Safe threshold distance to prevent strabismus (cross-eyed look) when tracking close targets.
* **Lock Head Movement:** Debug option to freeze head tracking, allowing isolate testing of the biomechanical eye limits.

### Sentimental Attention (`SentimentalGazeManager.cs` Inspector Properties)
* **Sentimental Objects:** A list of `GameObjects` in the scene designated as emotionally significant.
* **Sentimental Multiplier:** The multiplier factor used to boost the scoring of these objects when they enter the NPC's field of view.

---

## 🌟 Original Toolkit Features Overview

- **Blend Shapes Preset Editor**: Universal Unity editor extension for multi-mesh blend shapes.
- **Blend Shapes Mapper**: Scriptable object to save blend shapes presets.
- **Procedural Emotions**: Controls for emotions such as anger, disgust, fear, happiness, sadness, and surprise.
- **Gaze Modes**: Offers multiple gaze behaviors, including static, random, scripted, and probabilistic.
- **Eye Micro-saccades & Blinking**: Realistic eye movements and blinking features.
- **Lip Synchronization**: Supports real-time lip-sync based on microphone input, as well as pre-recorded audio tracks.
- **Dynamic Blend Shape Transition**: Smooth transitions using event-based system for optimized performance.
- **Demo Scene**: High-quality characters and HD rendering presets for showcasing features.

**Watch the original VHP videos:**  
🎥 [Virtual Human Project | Procedural Emotions, Gaze and Lip Sync](https://youtu.be/mstLuzTw790?si=5IOlBIR9mrzxmKuZ)  
🎥 [Virtual Human Project | Lip Sync Update](https://youtu.be/48T4lnm_Kqs?si=9hNPWfa4Gsfyyrou)

---

## 📥 Installation & Setup

1. **Clone the repository** or **download the latest release** from GitHub.
2. Open the project in **Unity** (recommended Unity version: 2020.3 or higher).
3. Select your character's `VHPGaze` component in the Inspector to configure the new biomechanical clamping limits.
4. Add the `SentimentalGazeManager` component to your character and configure the list of emotional targets.
5. Follow the setup guide in the documentation to get started with the demo scene and begin using the VHP features.

---

## 📄 License

This project is licensed under the **GNU GPLv3**. For commercial projects requiring a proprietary license or specific usage conditions, **dual licensing** is available.  
For inquiries regarding a commercial license for the base project, please contact **Geoffrey Gorisse, PhD** at geoffrey.gorisse@gmail.com.
