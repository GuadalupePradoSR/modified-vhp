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
* **Code Snippet (Editor Properties):**
  ```csharp
  [Header("Eye Clamping Limits (Ajustes para a malha)")]
  [SerializeField, Range(0, 60), Tooltip("Limite para esquerda/direita (Yaw)")] private float _maxYawLimit = 15.0f;
  [SerializeField, Range(0, 60), Tooltip("Limite para cima (Elevation)")] private float _maxElevationLimit = 10.0f;
  [SerializeField, Range(0, 60), Tooltip("Limite para baixo (Depression)")] private float _maxDepressionLimit = 15.0f;
  [SerializeField, Range(1, 50), Tooltip("Distancia minima do ponto focal falso")] private float _minFocalDistance = 10.0f;
  ```

### 2. Unified Focal-Center Correction
* **What it does:** Computes the gaze direction from a "unified focal center" (the midpoint between the two eyes) rather than calculating separate per-eye look directions. This single direction vector is clamped once and converted into a shared virtual target for both eyes, preventing binocular divergence (cross-eye artifacts / strabismus).
* **Focal Safety:** The reconstructed target is projected beyond a minimum focal distance to prevent excessive convergence when objects get too close to the NPC's face.
* **Modified File:** [`VHPGaze.cs`](Assets/Virtual%20Human%20Project/Scripts/VHPScripts/VHPGaze.cs)
* **Code Snippet (Unified Clamping & Rotational Target Application):**
  ```csharp
  // Clamps the gaze target position based on human eye rotational limits using a single midpoint vector
  private Vector3 ClampGazeDirection(Vector3 targetPos)
  {
      Vector3 centerEyePos = _eyesAveragePosition;
      Vector3 dirToTarget = (targetPos - centerEyePos).normalized;
      Vector3 forward = (_neutralTarget.transform.position - centerEyePos).normalized; 

      Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
      if (right == Vector3.zero) right = (_rightEyeBone.position - _leftEyeBone.position).normalized;
      Vector3 up = Vector3.Cross(forward, right).normalized;
      
      Quaternion headSpace = Quaternion.LookRotation(forward, up);
      Vector3 localDir = Quaternion.Inverse(headSpace) * dirToTarget;
      
      float yaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
      float pitch = Mathf.Asin(localDir.y) * Mathf.Rad2Deg;
      
      float originalYaw = yaw;
      float originalPitch = pitch;

      yaw = Mathf.Clamp(yaw, -_maxYawLimit, _maxYawLimit);
      pitch = Mathf.Clamp(pitch, -_maxDepressionLimit, _maxElevationLimit);
      
      if (originalYaw != yaw || originalPitch != pitch)
      {
          Debug.Log($"[Gaze Debug] Alvo fora do limite! Yaw: {originalYaw:F2}º -> {yaw:F2}º | Pitch: {originalPitch:F2}º -> {pitch:F2}º");
      }

      Vector3 clampedLocalDir = Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward;
      Vector3 clampedDir = headSpace * clampedLocalDir;
      
      float distance = Mathf.Max(Vector3.Distance(targetPos, centerEyePos), _minFocalDistance);
      
      return centerEyePos + clampedDir * distance;
  }

  // Inside CalculateGazeDirection(), we compute the ClampedTarget once and apply it to both eyes
  Vector3 clampedTarget = ClampGazeDirection(_targetPosition);
  _leftEyeBone.rotation = Quaternion.LookRotation(clampedTarget - _leftEyeBone.position) * _eyeForwardAxisRotationCorrection;
  _rightEyeBone.rotation = Quaternion.LookRotation(clampedTarget - _rightEyeBone.position) * _eyeForwardAxisRotationCorrection;
  ```

### 3. Context-Aware Target Prioritization (Sentimental Attention)
* **What it does:** Introduces a lightweight semantic-relevance gaze manager that hooks into the VHP's probabilistic target scoring system.
* **Result:** If a detected target in the interest field belongs to a developer-defined list of high-priority objects, its score is boosted using a base offset and a multiplier: `(currentWeight + 50f) * sentimentalMultiplier`. This biases the NPC's attention toward authored elements without replacing the original probabilistic pipeline.
* **Modified Files:** 
  * [`VHPGazeTarget.cs`](Assets/Virtual%20Human%20Project/Scripts/VHPScripts/VHPGazeTarget.cs) (declares and triggers the `OnCalculateTargetWeight` event hook)
  * [`SentimentalGazeManager.cs`](Assets/Virtual%20Human%20Project/Scripts/VHPScripts/SentimentalGazeManager.cs) (subscribes to the event and applies the priority weight multiplier)
* **Code Snippet (Weight Boost Event Hook & Listener):**
  *Inside `VHPGazeTarget.cs` (Hook declaration and trigger):*
  ```csharp
  // Event handler definition
  public delegate float TargetWeightHandler(Transform target, float currentWeight);
  public event TargetWeightHandler OnCalculateTargetWeight;

  // Invoked inside PonderateTargets()
  if (OnCalculateTargetWeight != null)
      targetPonderedValue = OnCalculateTargetWeight(_gazeTargets[i], targetPonderedValue);
  ```
  *Inside `SentimentalGazeManager.cs` (Priority multiplier application listener):*
  ```csharp
  private float AlterarPesoSentimental(Transform target, float currentWeight)
  {
      if (IsSentimentalObject(target.gameObject))
      {
          float novoPeso = (currentWeight + 50f) * sentimentalMultiplier;
          Debug.Log($"[Sentimental Gaze] Alvo Emocional '{target.name}' processado! Peso subiu de {currentWeight} para {novoPeso}");
          return novoPeso;
      }
      return currentWeight;
  }
  ```

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
