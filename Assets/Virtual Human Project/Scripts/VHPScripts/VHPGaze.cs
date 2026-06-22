/********************************************************************
Filename    :   VHPGaze.cs
Created     :   July 16th, 2020
Copyright   :   Geoffrey Gorisse.

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see<https://www.gnu.org/licenses/>.
********************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(VHPManager), typeof(Animator))]
public class VHPGaze : MonoBehaviour
{
    public enum GazeBehavior
    {
        PROBABILISTIC,
        RANDOM,
        STATIC,
        SCRIPTED,
        NONE
    }

    public enum GazeActiveBlendShapes
    {
        BLINK,
        GAZEUP,
        GAZEDOWN,
        NONE
    }

    public enum Axis
    {
        X,
        Y,
        Z
    }

    public GazeBehavior GazeBehaviorMode { get { return _gazeBehavior; } }
    public Vector3 EyesAveragePosition { get { return _eyesAveragePosition; } }
    public Vector3 NeutralTargetPosition { get { return _neutralTarget.transform.position; } }

    // Intensity values for gaze and blinking, used to adjust the associated blend shapes.
    [HideInInspector, Range(0, 100)] public float BlinkIntensity = 0f;
    [HideInInspector, Range(0, 100)] public float GazeUpIntensity = 0f;
    [HideInInspector, Range(0, 100)] public float GazeDownIntensity = 0f;

    // Delegate and event allowing the VHP manager to subscribe a function that updates the character's blend shapes with new gaze values.
    public delegate void OnGazeChangeDelegate(float[] currentGazeBlendShapeValues);
    public event OnGazeChangeDelegate OnGazeChange;

    [Header("Debug")]

    [SerializeField, Tooltip("Enable to display the gaze direction in the scene view.")] 
    private bool _drawGazeDirection = false;

    [SerializeField, Tooltip("Lock the character's head and neck movement to test eye limits.")] 
    private bool _lockHeadMovement = false;

    [Header("Character properties")]

    [SerializeField] private Transform _headBone;
    [SerializeField] private Transform _leftEyeBone;
    [SerializeField] private Transform _rightEyeBone;
    [SerializeField] private Axis _eyesForwardAxis;
    [SerializeField] private bool _invertEyesAxis = false;

    [Header("Gaze settings")]

    [SerializeField, Tooltip("Select the eye behavior model.")] private GazeBehavior _gazeBehavior;
    [SerializeField, HideInInspector] private GameObject _interestFieldPrefab;
    [SerializeField, HideInInspector] private bool _agentMode = false;
    [SerializeField, Tooltip("Enable random micro saccades to the gaze direction.")] public bool _enableMicroSaccades = true;
    [SerializeField] private bool _enableBlinking = true;

    [Header("Eye Clamping Limits (Ajustes para a malha)")]
    [SerializeField, Range(0, 60), Tooltip("Limite para esquerda/direita (Yaw)")] private float _maxYawLimit = 15.0f;
    [SerializeField, Range(0, 60), Tooltip("Limite para cima (Elevation)")] private float _maxElevationLimit = 10.0f;
    [SerializeField, Range(0, 60), Tooltip("Limite para baixo (Depression)")] private float _maxDepressionLimit = 15.0f;
    [SerializeField, Range(1, 50), Tooltip("Distancia minima do ponto focal falso (Aumente se o personagem for gigante e ficar vesgo)")] private float _minFocalDistance = 10.0f;

    private VHPManager _VHPmanager;

    private List<float> _blinkBlendShapeValues = new List<float>();
    private List<float> _gazeUpBlendShapeValues = new List<float>();
    private List<float> _gazeDownBlendShapeValues = new List<float>();

    [Range(0, 100)] private float _currentBlinkIntensity;
    [Range(0, 100)] private float _currentGazeUpIntensity;
    [Range(0, 100)] private float _currentGazeDownIntensity;

    private bool _enableEyesProceduralAnimation = false;
    private Vector3 _eyesAveragePosition;
    private GameObject _gazeSubObjectsParent;
    private GameObject _neutralTarget;
    private GameObject _target;
    private Vector3 _targetPosition;
    private GameObject _interestFieldInstance;
    private bool _interestFieldLoaded = false;
    private bool _characterModeSet = false;
    private Animator _animator;
    private GazeActiveBlendShapes _gazeActiveBlendShapes = GazeActiveBlendShapes.NONE;
    private Quaternion _eyeForwardAxisRotationCorrection;
    private float _currentMicroSaccadesWaitingTime = 0f;
    private bool _islookingAtRandomPoint = false;
    private float _currentBlinkingWaitingTime = 0f;
    private bool _isBlinking = false;

    private void Awake()
    {
        _VHPmanager = gameObject.GetComponent<VHPManager>();

        if (_headBone && _leftEyeBone && _rightEyeBone)
            _enableEyesProceduralAnimation = true;

        else
            Debug.LogWarning("Missing bone transform(s). Please assign the character's bones in the public fields to enable the procedural gaze system!");

        LoadBlendShapeValues();
        InstantiateGazeComponents();
    }

    private void OnEnable()
    {
        if (_enableEyesProceduralAnimation)
        {
            SyncGazeIntensityValues();
            InstantiateGazeTarget();

            int eyesForwardAxisDirection = 1;

            if (_invertEyesAxis)
                eyesForwardAxisDirection = -1;

            switch (_eyesForwardAxis)
            {
                case Axis.X:
                    _eyeForwardAxisRotationCorrection = Quaternion.Euler(new Vector3(0, 90 * eyesForwardAxisDirection, 0));
                    break;
                case Axis.Y:
                    _eyeForwardAxisRotationCorrection = Quaternion.Euler(new Vector3(90 * eyesForwardAxisDirection, 0, 0));
                    break;
                case Axis.Z:
                    if (_invertEyesAxis)
                        _eyeForwardAxisRotationCorrection = Quaternion.Euler(new Vector3(0, 0, eyesForwardAxisDirection));
                    else
                        _eyeForwardAxisRotationCorrection = Quaternion.Euler(Vector3.zero);
                    break;
                default:
                    break;
            }
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        _currentBlinkIntensity = 0f;
        _currentGazeUpIntensity = 0f;
        _currentGazeDownIntensity = 0f;

        SyncGazeIntensityValues();

        Destroy(_target);

        if (_interestFieldInstance)
        {
            Destroy(_interestFieldInstance);
            _interestFieldLoaded = false;
        }
    }

    // Late update loop used to override bone animations and procedurally control eye orientation.
    private void LateUpdate()
    {
        _eyesAveragePosition = Vector3.Lerp(_leftEyeBone.position, _rightEyeBone.position, 0.5f);

        _gazeSubObjectsParent.transform.position = _eyesAveragePosition;
        _gazeSubObjectsParent.transform.rotation = _headBone.rotation;

        if (_gazeBehavior == GazeBehavior.PROBABILISTIC)
        {
            if(_interestFieldLoaded && _agentMode)
                _interestFieldInstance.transform.rotation = transform.rotation;

            else if(!_interestFieldLoaded)                
                SetProbabilisticGazeInterestField();
        }

        if (_interestFieldLoaded && (_gazeBehavior != GazeBehavior.PROBABILISTIC || _agentMode != _characterModeSet))
        {
            Destroy(_interestFieldInstance);
            _interestFieldLoaded = false;
        }

        CalculateGazeDirection();
        CalculateGazeDirectionIntensity();

        if (_enableBlinking)
            SetBlinkingIntensity();

        SetCurrentGazeIntensityValues();
    }

    #region Blend shape values initilization

    // Loads the maximum gaze blend shape values from the blend shapes mapper.
    private void LoadBlendShapeValues()
    {
        if (_VHPmanager.blendShapesMapperPreset)
        {
            BlendShapesMapper blendShapesMapper = _VHPmanager.blendShapesMapperPreset;

            CopyBlendShapesMapperValues(blendShapesMapper.GetBlenShapeValues(BlendShapesMapper.FacialExpression.BLINK), _blinkBlendShapeValues);
            CopyBlendShapesMapperValues(blendShapesMapper.GetBlenShapeValues(BlendShapesMapper.FacialExpression.GAZEUP), _gazeUpBlendShapeValues);
            CopyBlendShapesMapperValues(blendShapesMapper.GetBlenShapeValues(BlendShapesMapper.FacialExpression.GAZEDOWN), _gazeDownBlendShapeValues);
        }

        else
        {
            Debug.LogWarning("No blend shapes preset! Procedural gaze will not be initialized.");
            return;
        }
    }

    // Copies the values from the blend shapes mapper.
    private void CopyBlendShapesMapperValues(List<float> blendShapesMapperValues, List<float> gazeBlendShapeValues)
    {
        for (int i = 0; i < blendShapesMapperValues.Count; i++)
        {
            if (blendShapesMapperValues[i] >= 0 && blendShapesMapperValues[i] <= 100)
                gazeBlendShapeValues.Add(blendShapesMapperValues[i]);

            else
                gazeBlendShapeValues.Add(0);
        }
    }

    #endregion

    #region Gaze components initialization

    // Instiantiates the GameObjects required to compute the gaze direction and associated blend shape values.
    private void InstantiateGazeComponents()
    {
        _eyesAveragePosition = Vector3.Lerp(_leftEyeBone.position, _rightEyeBone.position, 0.5f);

        _gazeSubObjectsParent = new GameObject("Gaze_Sub_Objects");
        _gazeSubObjectsParent.transform.position = _eyesAveragePosition;
        _gazeSubObjectsParent.transform.rotation = _headBone.rotation;
        _gazeSubObjectsParent.transform.parent = transform;

        // Instantiates a target corresponding to the character's neutral gaze direction based on the eyes' forward axis.
        // This target is used to compute the relative angle between the current gaze direction and the neutral one.
        _neutralTarget = new GameObject("Neutral_Target");
        //m_neutralTarget.hideFlags = HideFlags.HideInHierarchy;
        _neutralTarget.transform.parent = _gazeSubObjectsParent.transform;
        _neutralTarget.transform.position = _gazeSubObjectsParent.transform.position;
        _neutralTarget.transform.rotation = _leftEyeBone.transform.rotation;

        int eyesForwardAxisSense = 1;

        if (_invertEyesAxis)
            eyesForwardAxisSense = -1;

        switch (_eyesForwardAxis)
        {
            case Axis.X:
                _neutralTarget.transform.Translate(eyesForwardAxisSense, 0, 0);
                break;
            case Axis.Y:
                _neutralTarget.transform.Translate(0, eyesForwardAxisSense, 0);
                break;
            case Axis.Z:
                _neutralTarget.transform.Translate(0, 0, eyesForwardAxisSense);
                break;
            default:
                break;
        }
    }

    // Instantiates the eyes' target along with its script to set and control the gaze model.
    private void InstantiateGazeTarget()
    {
        _target = new GameObject("Eyes_Target");
        _target.AddComponent<VHPGazeTarget>();
        _target.GetComponent<VHPGazeTarget>().VHPGaze = this;
        _target.transform.parent = _gazeSubObjectsParent.transform;
        _target.transform.position = _neutralTarget.transform.position;

        _targetPosition = _target.transform.position;
    }

    // Instantiates the interest field prefab required to detect potential targets in the scene for the probabilistic gaze model.
    private void SetProbabilisticGazeInterestField()
    {
        if (_interestFieldPrefab)
        {
            _interestFieldInstance = Instantiate(_interestFieldPrefab);
            _interestFieldInstance.name = "Interest_Field";
            _interestFieldInstance.transform.parent = _gazeSubObjectsParent.transform;
            _interestFieldInstance.transform.position = _gazeSubObjectsParent.transform.position;

            if (!_agentMode)
                _interestFieldInstance.transform.rotation = transform.parent.rotation;

            else
                _animator = transform.GetComponent<Animator>();

            // Interest field configuration based on the selected mode (avatar/agent).
            _interestFieldInstance.transform.GetComponent<MeshCollider>().enabled = !_agentMode;
            _interestFieldInstance.transform.GetComponent<SphereCollider>().enabled = _agentMode;

            _target.GetComponent<VHPGazeTarget>().VHPGazeInterestField = _interestFieldInstance.GetComponent<VHPGazeInterestField>();

            _characterModeSet = _agentMode;
            _interestFieldLoaded = true;
        }

        else
            Debug.LogError("No interest field prefab assigned in the public field! The interest field prefab must be located in the following folder: Assets/Virtual Human Project/Prefabs/");
    }

    #endregion

    #region Gaze direction and micro-saccades

    // Sets the direction of the gaze and optional micro-saccades.
    private void CalculateGazeDirection()
    {
        if (_enableMicroSaccades)
        {
            if (_currentMicroSaccadesWaitingTime >= Random.Range(1f, 3f) && !_islookingAtRandomPoint)
            {
                _targetPosition = CalculateGazeMicroSaccades(_target.transform.position);
                StartCoroutine(SetGazeMicroSaccadesDuration());
            }

            else if (!_islookingAtRandomPoint)
                _targetPosition = _target.transform.position;

            _currentMicroSaccadesWaitingTime += Time.deltaTime;
        }

        else
            _targetPosition = _target.transform.position;

        // Apply clamping ONCE from the center point so eyes don't split targets and go cross-eyed
        Vector3 clampedTarget = ClampGazeDirection(_targetPosition);

        // Makes the eyes look at the clamped target
        _leftEyeBone.rotation = Quaternion.LookRotation(clampedTarget - _leftEyeBone.position) * _eyeForwardAxisRotationCorrection;
        _rightEyeBone.rotation = Quaternion.LookRotation(clampedTarget - _rightEyeBone.position) * _eyeForwardAxisRotationCorrection;

        if (_drawGazeDirection)
        {
            Debug.DrawLine(_leftEyeBone.transform.position, clampedTarget, Color.cyan);
            Debug.DrawLine(_rightEyeBone.transform.position, clampedTarget, Color.cyan);
        }
    }

    // Clamps the gaze target position based on human eye rotational limits
    private Vector3 ClampGazeDirection(Vector3 targetPos)
    {
        Vector3 centerEyePos = _eyesAveragePosition;
        Vector3 dirToTarget = (targetPos - centerEyePos).normalized;
        Vector3 forward = (_neutralTarget.transform.position - centerEyePos).normalized; 

        // Ensure we calculate the exact right and up vectors
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        if (right == Vector3.zero) right = (_rightEyeBone.position - _leftEyeBone.position).normalized;
        Vector3 up = Vector3.Cross(forward, right).normalized;
        
        Quaternion headSpace = Quaternion.LookRotation(forward, up);
        Vector3 localDir = Quaternion.Inverse(headSpace) * dirToTarget;
        
        // Convert to angles
        float yaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        float pitch = Mathf.Asin(localDir.y) * Mathf.Rad2Deg;
        
        float originalYaw = yaw;
        float originalPitch = pitch;

        // Clamp values
        yaw = Mathf.Clamp(yaw, -_maxYawLimit, _maxYawLimit);
        // Correcting the pitch inversion logic
        pitch = Mathf.Clamp(pitch, -_maxDepressionLimit, _maxElevationLimit);
        
        if (originalYaw != yaw || originalPitch != pitch)
        {
            Debug.Log($"[Gaze Debug] Alvo fora do limite! Yaw: {originalYaw:F2}º -> {yaw:F2}º | Pitch: {originalPitch:F2}º -> {pitch:F2}º");
        }

        // Reconstruct direction with pitch inverted correctly for Unity's Euler system
        Vector3 clampedLocalDir = Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward;
        Vector3 clampedDir = headSpace * clampedLocalDir;
        
        // Force a minimum distance so the eyes never go cross-eyed (vesgos) when the target is too close
        float distance = Mathf.Max(Vector3.Distance(targetPos, centerEyePos), _minFocalDistance);
        
        return centerEyePos + clampedDir * distance;
    }

    // Calculates the position of a random target near the current target to add micro-saccades.
    private Vector3 CalculateGazeMicroSaccades(Vector3 currentEyesTargetPosition)
    {
        // Depending on the target distance, a maximum value for random positioning is calculated.
        float targetDistance = Vector3.Distance(_eyesAveragePosition, currentEyesTargetPosition);
        float maxRandomDeviation = 0.1f * targetDistance;
        float deviation = Random.Range(-maxRandomDeviation, maxRandomDeviation);

        Vector3 randomTargetPosition = new Vector3(currentEyesTargetPosition.x + deviation, currentEyesTargetPosition.y + deviation, currentEyesTargetPosition.z + deviation);

        return randomTargetPosition;
    }

    // Sets the duration of the micro-saccades.
    private IEnumerator SetGazeMicroSaccadesDuration()
    {
        _islookingAtRandomPoint = true;

        yield return new WaitForSeconds(Random.Range(0.2f, 0.3f));

        _islookingAtRandomPoint = false;
        _currentMicroSaccadesWaitingTime = 0;
    }

    #endregion

    #region Blend shape intensity values

    // Calculates the intensity of the gaze direction.
    private void CalculateGazeDirectionIntensity()
    {
        // Calculates the current gaze direction vector and the initial forward direction vector.
        Vector3 gazeDirection = _target.transform.position - _eyesAveragePosition;
        Vector3 gazeInitialDirection = _neutralTarget.transform.position - _eyesAveragePosition;

        // Calculates the normal of the plane based on the vector between the character's eyes.
        Vector3 planeNormal = _leftEyeBone.transform.position - _rightEyeBone.transform.position;

        // Projects the gaze direction and the initial forward direction on the plane.
        Vector3 projectedGazeDirection = Vector3.ProjectOnPlane(gazeDirection, planeNormal);
        Vector3 projectedInitialGazeDirection = Vector3.ProjectOnPlane(gazeInitialDirection, planeNormal);

        // Calcultaes the angle between the two directions.
        float eyesAngle = Vector3.SignedAngle(projectedGazeDirection, projectedInitialGazeDirection, planeNormal);

        float blendShapeIntensityMultiplier = 15f;

        // Sets the blend shape intensity values based on the gaze direction.
        if (eyesAngle >= 0 && eyesAngle < 180)
            GazeDownIntensity = Mathf.Clamp(eyesAngle * blendShapeIntensityMultiplier, 0f, 100f);

        else if (eyesAngle < 0 && eyesAngle >= -180)
            GazeUpIntensity = Mathf.Clamp((eyesAngle * -1) * blendShapeIntensityMultiplier, 0f, 100f);
    }

    // Sets the blinking intensity.
    private void SetBlinkingIntensity()
    {
        if (_currentBlinkingWaitingTime >= Random.Range(3f, 8f) && !_isBlinking)
        {
            _isBlinking = true;
            BlinkIntensity = 100f;

            StartCoroutine(SetBlinkingDuration(0.1f));

            _currentBlinkingWaitingTime = 0;
        }

        _currentBlinkingWaitingTime += Time.deltaTime;
    }

    // Controls the blinking duration.
    private IEnumerator SetBlinkingDuration(float duration)
    {
        yield return new WaitForSeconds(duration);

        BlinkIntensity = 0f;
        _isBlinking = false;
    }

    // Sets the current gaze intensity values.
    private void SetCurrentGazeIntensityValues()
    {
        float[] currentGazeIntensityValues = { _currentBlinkIntensity, _currentGazeUpIntensity, _currentGazeDownIntensity };
        float[] requestedGazeIntensityValues = { BlinkIntensity, GazeUpIntensity, GazeDownIntensity };

        for (int i = 0; i < currentGazeIntensityValues.Length; i++)
        {
            // Detects a significant difference in the requested gaze intensities to update the blend shape values.
            if (requestedGazeIntensityValues[i] > currentGazeIntensityValues[i] + 10 || requestedGazeIntensityValues[i] < currentGazeIntensityValues[i] - 10)
            {
                currentGazeIntensityValues[i] = requestedGazeIntensityValues[i];
                _gazeActiveBlendShapes = (GazeActiveBlendShapes)i;

                // All intensity values are set to 0, except for the blinking and current gaze intensities, as they are mutually exclusive.
                for (int j = 0; j < currentGazeIntensityValues.Length; j++)
                    if (j != 0 && j != (int)_gazeActiveBlendShapes)
                        currentGazeIntensityValues[j] = 0;

                _currentBlinkIntensity = currentGazeIntensityValues[0];
                _currentGazeUpIntensity = currentGazeIntensityValues[1];
                _currentGazeDownIntensity = currentGazeIntensityValues[2];

                SyncGazeIntensityValues();
                UpdateGazeBlendShapeValues();

                break;
            }
        }
    }

    // Synchronizes the current and requested gaze intensity values.
    private void SyncGazeIntensityValues()
    {
        BlinkIntensity = _currentBlinkIntensity;
        GazeUpIntensity = _currentGazeUpIntensity;
        GazeDownIntensity = _currentGazeDownIntensity;
    }

    // Updates the current gaze blend shape values and triggers the event to allow the VHP manager to update the character's blend shapes with the new gaze values.
    private void UpdateGazeBlendShapeValues()
    {
        float[] currentGazeBlendShapeValues = new float[_VHPmanager.TotalCharacterBlendShapes];

        switch (_gazeActiveBlendShapes)
        {
            case GazeActiveBlendShapes.GAZEUP:
                for (int i = 0; i < _gazeUpBlendShapeValues.Count; i++)
                    currentGazeBlendShapeValues[i] = Mathf.Clamp(_currentGazeUpIntensity * _gazeUpBlendShapeValues[i] / 100, 0f, 100f);
                break;
            case GazeActiveBlendShapes.GAZEDOWN:
                for (int i = 0; i < _gazeDownBlendShapeValues.Count; i++)
                    currentGazeBlendShapeValues[i] = Mathf.Clamp(_currentGazeDownIntensity * _gazeDownBlendShapeValues[i] / 100, 0f, 100f);
                break;
        }

        // Blinking blend shape values are added to the current gaze values list to combine the gaze direction blend shapes with the blinking blend shapes.
        for (int i = 0; i < currentGazeBlendShapeValues.Length; i++)
            currentGazeBlendShapeValues[i] = Mathf.Clamp(currentGazeBlendShapeValues[i] + (_currentBlinkIntensity * _blinkBlendShapeValues[i] / 100f), 0f, 100f);

        // If any function is subscribed to the gaze change event, the associated delegate is invoked with the current gaze blend shape values as parameter.
        OnGazeChange?.Invoke(currentGazeBlendShapeValues);
    }

    #endregion

    #region Agent mode IK settings

    private void OnAnimatorIK()
    {
        if (_animator && _gazeBehavior == GazeBehavior.PROBABILISTIC && _agentMode && !_lockHeadMovement)
        {
            // Sets the weights (head and body) to rotate the character based on the target's location.
            // Note that the eyes' weight is not overridden as it is controlled by other functions to allow for micro saccades without rotating the character's body.
            // Additionally, the eyes are controlled outside the animator functions to support non-rigged characters (e.g., partial avatars in VR).
            _animator.SetLookAtWeight(1f, 0.05f, 0.5f);
            _animator.SetLookAtPosition(_target.transform.position);
        }
    }

    #endregion
}