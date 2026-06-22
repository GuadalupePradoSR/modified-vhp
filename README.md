# Virtual Human Project (VHP) - Context-Aware & Biomechanically Constrained Gaze Extension

*Original VHP Version: 0.2.1 (Beta) - 05/10/2025* *Original Contact: Geoffrey Gorisse, PhD, geoffrey.gorisse@gmail.com* *Modified Version Author: Guadalupe Prado Saldanha Ribeiro (Gira LAB / Universidade de Fortaleza - Unifor)* *Associated Publication: XXV Brazilian Symposium on Games and Digital Entertainment (SBGames 2026)*

The Virtual Human Project (VHP) offers developers an optimized, scalable, and easy-to-integrate package to procedurally animate realistic virtual humans in Unity. 

This repository is a **fork/modified version** of the original VHP toolkit. [cite_start]It introduces a gaze-control extension designed to improve the plausibility of NPC gaze through biomechanical constraints, binocular coherence, and context-aware target prioritization[cite: 9].

---

## 🛠️ Custom Modifications in this Version

[cite_start]In games, NPC gaze and aiming behavior often remain mechanically rigid or weakly connected to scene context[cite: 8]. To address this, the original VHP gaze pipeline was extended with the following features:

### 1. Biomechanical Eye Clamping
[cite_start]Independent per-eye limits can create cross-eye artifacts, and unrestrained rotations lead to anatomically unsafe mesh deformations[cite: 83]. 
* [cite_start]**What it does:** Restricts eye rotations according to configurable yaw and pitch limits (typically $15^{\circ}$ to $25^{\circ}$) based on human ocular ranges[cite: 10, 91, 93]. 
* [cite_start]**Head-Eye Coordination:** By clamping the eyes to a comfort range, the system encourages the Head/Neck Inverse Kinematics (IK) to participate naturally when tracking extreme targets[cite: 83, 93].
* [cite_start]**Modified File:** [`NomeDoScriptModificado.cs`](link-para-o-arquivo-modificado-aqui) [cite: 126]

### 2. Unified Focal-Center Correction
* [cite_start]**What it does:** Computes the gaze direction from a "unified focal center" (the midpoint between the two eyes) rather than calculating per-eye directions[cite: 110, 111]. [cite_start]This single direction vector is clamped and converted into a shared virtual target for both eyes, preventing asymmetric convergence[cite: 10, 111, 112].
* [cite_start]**Focal Safety:** The reconstructed target is projected beyond a minimum focal distance to prevent excessive convergence (strabismus) when objects get too close to the NPC's face[cite: 114].
* [cite_start]**Modified File:** [`NomeDoScriptModificado.cs`](link-para-o-arquivo-modificado-aqui) [cite: 126]

### 3. Context-Aware Target Prioritization
[cite_start]Game scenes often contain objects that are semantically relevant, even if they are not the closest to the NPC[cite: 117].
* [cite_start]**What it does:** Introduces a lightweight semantic-relevance gaze manager[cite: 118]. [cite_start]This component intercepts the VHP's probabilistic target scoring and checks if a detected object belongs to a developer-defined list of high-priority objects[cite: 119].
* [cite_start]**Result:** If the object is relevant, its gaze weight is artificially boosted via a base value and a multiplier, biasing the NPC's attention toward authored elements without replacing the original probabilistic pipeline[cite: 120, 121, 122].
* [cite_start]**New Manager File:** [`NomeDoScriptDoManager.cs`](link-para-o-arquivo-novo-aqui) [cite: 126]

---

## ⚙️ How to configure the new parameters

[cite_start]The new system exposes parameters directly in the Unity Inspector, making it adjustable for different character rigs without changing the source code[cite: 94, 95]:
* [cite_start]**Maximum Yaw / Elevation / Depression:** Define the anatomical comfort limits for the eyes[cite: 94].
* [cite_start]**Minimum Focal Distance:** Prevents objects from forcing the eyes to cross[cite: 94].
* [cite_start]**Semantic Relevance List:** Add specific GameObjects here to increase their priority in the NPC's attention system[cite: 150, 151].

---

## 🌟 Original Toolkit Features Overview

- **Blend Shapes Preset Editor**: Universal Unity editor extension for multi-mesh blend shapes.
- **Blend Shapes Mapper**: Scriptable object to save blend shapes presets.
- **Procedural Emotions**: Controls for emotions such as anger, disgust, fear, happiness, sadness, and surprise.
- **Gaze Modes**: Offers multiple gaze behaviors, including static, random, scripted and probabilistic.
- **Eye Micro-saccades & Blinking**: Realistic eye movements and blinking features.
- **Lip Synchronization**: Supports real-time lip-sync based on microphone input, as well as pre-recorded audio tracks.
- **Dynamic Blend Shape Transition**: Smooth transitions using event-based system for optimized performance.
- **Demo Scene**: High-quality characters and HD rendering presets for showcasing features.

**Watch the original VHP videos**:\
[Virtual Human Project | Procedural Emotions, Gaze and Lip Sync](https://youtu.be/mstLuzTw790?si=5IOlBIR9mrzxmKuZ)\
[Virtual Human Project | Lip Sync Update](https://youtu.be/48T4lnm_Kqs?si=9hNPWfa4Gsfyyrou)

---

## 📥 Installation & Setup

1. **Clone the repository** or **download the latest release** from GitHub.
2. Open the project in **Unity** (recommended Unity version: 2020.3 or higher).
3. Select your character's VHP Gaze component in the Inspector to configure the new Biomechanical Clamping and Semantic Target lists.
4. **Follow the setup guide** in the documentation to get started with the demo scene and begin using the VHP features.

---

## 📄 License

This project is licensed under the **GNU GPLv3**. For commercial projects requiring a proprietary license or specific usage conditions, **dual licensing** is available.  
For inquiries regarding a commercial license for the base project, please contact **Geoffrey Gorisse, PhD** at geoffrey.gorisse@gmail.com.
