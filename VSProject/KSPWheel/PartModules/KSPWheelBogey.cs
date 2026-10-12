using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelBogey : KSPWheelSubmodule
    {
        #region REGION - Standard Part Config File Fields
        [KSPField]
        public string bogeyName = string.Empty;

        [KSPField]
        public Vector3 bogeyRotAxis = Vector3.left;

        /// <summary>
        /// Default config set bogey droop angle. Positive values only inversion of droop angle handled by invertRotation boolean 
        /// (works better with rotated/mirrored landing gear than negative values) checks are in place to remove sign if config set incorrectly
        /// </summary>
        [KSPField]
        public float restingRotation = 0f;

        /// <summary>
        /// Max allowed bogey droop angle set in config.
        /// </summary>
        [KSPField]
        public float maxBogeyDroopAngle = 45f;

        [KSPField]
        public float rotationOffset = 0f;

        /// <summary>
        /// Nominal bogey droop rotation speed in degrees per second.
        /// </summary>
        [KSPField]
        public float rotationSpeed = 5f;

        /// <summary>
        /// Maximum probe-driven bogey rotation in degrees per second.
        /// </summary>
        [KSPField]
        public float groundRotationSpeed = 120f;
        #endregion ENDREGION - Standard Part Config File Fields

        #region REGION - User GUI

        [KSPField]
        public bool userSetBogeyAngle = false; 

        /// <summary>
        /// Toggle user adjustable Bogey Settings.
        /// </summary>
        [KSPField(guiName = "Bogey Settings", guiActive = false, guiActiveEditor = true, isPersistant = true),
        UI_Toggle(affectSymCounterparts = UI_Scene.All, controlEnabled = true, disabledText = "Hidden", enabledText = "Shown", requireFullControl = false, suppressEditorShipModified = true, scene = UI_Scene.All)]
        public bool showBogeySettings = false; // Toggle bogey settings visibility in GUI for user adjustment of bogey resting angle and invert controls

        /// <summary>
        /// User adjustable bogey droop angle.
        /// </summary>
        [KSPField(guiName = "Bogey Droop Angle", guiActive = false, guiActiveEditor = false, isPersistant = true),
        UI_FloatRange(minValue = 0f, maxValue = 45f, stepIncrement = 1f, suppressEditorShipModified = true)]
        public float userBogeyDroopAngle = 0f; // User selected bogey droop angle

        /// <summary>
        /// User inversion of bogey droop angle.
        /// </summary>
        [KSPField(guiName = "Invert Bogey Rotation", guiActive = false, guiActiveEditor = true, isPersistant = true),
        UI_Toggle(enabledText = "Inverted", disabledText = "Normal", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
        public bool invertRotation = false; // Toggle for inverting restingRotation, used for mirror symmetry or rotated parts or as user toggle to invert angle

        // GUI Visibility controls

        /// <summary>
        /// State for GUI BogeySettings, set to false in config to disable user adjustments.
        /// </summary>
        [KSPField]
        public bool showGUIBogeySettings = true;
        
        /// <summary>
        /// State for GUI UserBogeyDroopAngle.
        /// </summary>
        [KSPField]
        public bool showGUIUserBogeyDroopAngle = true;
        
        /// <summary>
        /// State for GUI InvertRotation.
        /// </summary>
        [KSPField]
        public bool showGUIInvertRotation = true;

        /// <summary>
        /// State for GUI ResetBogeyAngle.
        /// </summary>
        [KSPField]
        public bool showResetBogey = true;

        /// <summary>
        /// Runtime bogey angle display for debugging.
        /// </summary>
        [KSPField(guiName = "Current Bogey Angle", guiActive = true, guiFormat = "F1")]
        public float angle;

        [KSPField(isPersistant = true)]
        public bool userValuesInitialised = false;

        #endregion ENDREGION - User GUI

        #region REGION - Private Working Variables

        private Quaternion defaultRotation;
        private Transform bogeyTransform;
        private float defaultDroopAngle;
        private bool defaultInverted;
        private int lastProbeFrame = -1;
        internal bool probeControlEnabled;

        internal Transform ProbeTransform { get { return bogeyTransform; } }
        internal Quaternion ProbeRestRotation { get { return defaultRotation; } }

        internal void MarkProbeContact()
        {
            lastProbeFrame = Time.frameCount;
        }

        internal void FollowGroundNormal()
        {
            Vector3 normal = bogeyTransform.InverseTransformDirection(wheel.contactNormal);
            float correction = getBogeyAngle(normal) + rotationOffset;
            if (correction >= 180f) { correction -= 360f; }
            if (correction < -180f) { correction += 360f; }
            if (groundRotationSpeed > 0f)
                correction = Mathf.MoveTowards(0f, correction, Time.deltaTime * groundRotationSpeed);
            bogeyTransform.Rotate(bogeyRotAxis, correction, Space.Self);
        }

        #endregion ENDREGION - Private Working Variables

        #region REGION - GUI Methods
        public void updateGUIVisibility()
        {
            bool settingsVisible = userSetBogeyAngle && showGUIBogeySettings && showBogeySettings;

            Fields[nameof(showBogeySettings)].guiActiveEditor = userSetBogeyAngle;
            Fields[nameof(invertRotation)].guiActiveEditor = settingsVisible && showGUIInvertRotation;
            Fields[nameof(userBogeyDroopAngle)].guiActiveEditor = settingsVisible && showGUIUserBogeyDroopAngle;
            Events[nameof(resetBogeyAngle)].guiActiveEditor = settingsVisible && showResetBogey;
        }

        public void onShowUIUpdated(BaseField field, object obj)
        {
            updateGUIVisibility();

            this.symmetryUpdate(m =>
            {
                if (m != this)
                {
                    m.updateGUIVisibility();
                }
            });
        }

        // Reset bogey settings to stored config defaults
        [KSPEvent(guiName = "Reset Bogey Angle", guiActive = false, guiActiveEditor = false)]
        public void resetBogeyAngle()
        {
            userBogeyDroopAngle = defaultDroopAngle;
            invertRotation = defaultInverted;

            this.updateUIFloatRangeControl(nameof(userBogeyDroopAngle), userBogeyDroopAngle, 0f, maxBogeyDroopAngle, 1f);

            updateGUIVisibility();

            // Update symmetry counterparts
            this.symmetryUpdate(m =>
            {
                m.userBogeyDroopAngle = m.defaultDroopAngle;
                m.invertRotation = m.defaultInverted;

                m.updateUIFloatRangeControl(nameof(m.userBogeyDroopAngle), m.userBogeyDroopAngle, 0f, m.maxBogeyDroopAngle, 1f);

                m.updateGUIVisibility();
            });
        }

        //Initialise user adjustable bogey angle from the part CFG default.
        //Only runs once for each part instance.
        public void initialiseUserValues()
        {
            if (!userValuesInitialised)
            {
                userBogeyDroopAngle = defaultDroopAngle;

                userValuesInitialised = true;
            }
        }
        #endregion ENDREGION - GUI Methods

        #region REGION - Standard KSP/Unity Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            defaultDroopAngle = Mathf.Clamp(Mathf.Abs(restingRotation), 0f, maxBogeyDroopAngle);
            defaultInverted = invertRotation;
            
            initialiseUserValues();

            this.updateUIFloatRangeControl(nameof(userBogeyDroopAngle), userBogeyDroopAngle, 0f, maxBogeyDroopAngle, 1f);
 
            Fields[nameof(showBogeySettings)].uiControlEditor.onFieldChanged = onShowUIUpdated; //Connect bogey settings toggle to GUI visibility handler
            Fields[nameof(userBogeyDroopAngle)].uiControlEditor.onFieldChanged = onBogeyDroopAngleChanged; //Connect user droop angle to symmetry update handler
            Fields[nameof(invertRotation)].uiControlEditor.onFieldChanged = onInvertRotationChanged; // Connect inversion toggle to symmetry update handler

            updateGUIVisibility();
        }

        internal override void postControllerSetup()
        {
            base.postControllerSetup();
            bogeyTransform = part.transform.FindChildren(bogeyName)[wheelData.indexInDuplicates];
            if (bogeyTransform == null)
            {
                MonoBehaviour.print("ERROR: Could not locate bogey for name: " + bogeyName);
            }
            defaultRotation = bogeyTransform.localRotation;
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
            if (controller.wheelState != KSPWheelState.DEPLOYED)
            {
                bogeyTransform.localRotation = rotationSpeed > 0
                    ? Quaternion.Lerp(bogeyTransform.localRotation, defaultRotation, Time.deltaTime)
                    : defaultRotation;

                return;
            }
            // The visual tyre probes own the grounded angle while they are in contact.
            if ((wheel.isGrounded && probeControlEnabled) ||
                (lastProbeFrame >= 0 && Time.frameCount - lastProbeFrame <= 1)) { return; }
            if (wheel.isGrounded)
            {
                FollowGroundNormal();
            }
            else
            {
                angle = getDroopAngle() + rotationOffset;

                if (angle >= 180) { angle -= 360f; }
                if (angle < -180) { angle += 360f; }

                Quaternion dest = defaultRotation * Quaternion.Euler(angle * bogeyRotAxis);

                bogeyTransform.localRotation = rotationSpeed > 0 ? Quaternion.RotateTowards(bogeyTransform.localRotation, dest, Time.deltaTime * rotationSpeed) : dest;
            }
        }
        #endregion ENDREGION - Standard KSP/Unity Overrides

        #region REGION - Custom Update Methods
        private float getDroopAngle()
        {
            float result = userSetBogeyAngle ? userBogeyDroopAngle : defaultDroopAngle;

            if (invertRotation)
            {
                result = -result;
            }
            KSPWheelAdjustableGear adjustableGear = part.GetComponent<KSPWheelAdjustableGear>();

            if (adjustableGear != null && adjustableGear.isFlipped) // KSPWheelAdjustableGear modifier, applies inverted result when part has mirrored clone
            {
                result = -result;
            }
            return result;
        }

        private void onBogeyDroopAngleChanged(BaseField field, object obj)
        {
            this.symmetryUpdate(m =>
            {
                m.userBogeyDroopAngle = userBogeyDroopAngle;
            });
        }

        private void onInvertRotationChanged(BaseField field, object obj)
        {
            this.symmetryUpdate(m =>
            {
                m.invertRotation = invertRotation;
            });
        }

        private float getBogeyAngle(Vector3 localHitNorm)
        {
            float axisA = 0;
            float axisB = 0;
            bool invert = false;
            if (bogeyRotAxis.x != 0)
            {
                axisA = localHitNorm.y;
                axisB = localHitNorm.z;
                invert = bogeyRotAxis.x < 0;
            }
            else if (bogeyRotAxis.y != 0)
            {
                axisA = localHitNorm.x;
                axisB = localHitNorm.z;
                invert = bogeyRotAxis.y < 0;
            }
            else if (bogeyRotAxis.z != 0)
            {
                axisA = localHitNorm.x;
                axisB = localHitNorm.y;
                invert = bogeyRotAxis.z < 0;
            }
            angle = Mathf.Atan2(axisB, axisA) * Mathf.Rad2Deg;
            if (invert) { angle = -angle; }
            return angle;
        }
        #endregion ENDREGION - Custom Update Methods
    }
}
