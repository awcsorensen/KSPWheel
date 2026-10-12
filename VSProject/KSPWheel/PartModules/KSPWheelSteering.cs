using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelSteering : KSPWheelSubmodule
    {

        /// <summary>
        /// Name of the transform that will be rotated for visual steering effect
        /// </summary>
        [KSPField]
        public string steeringName = string.Empty;
        
        /// <summary>
        /// Maximum deflection angle of the steering transform, measured from its default state (rotation = 0,0,0)
        /// </summary>
        [KSPField(guiName = "Max Steering Angle", guiActive = true, guiActiveEditor = true, isPersistant = true, guiUnits = " deg"),
         UI_FloatEdit(minValue = 0f, maxValue = 180f, incrementLarge = 10f, incrementSmall = 1f, incrementSlide = 1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.Editor)]
        public float maxSteeringAngle = 0f;

        /// <summary>
        /// If true the steering will be locked to zero and will not respond to steering input.
        /// </summary>
        [KSPField(guiName = "Steering Lock", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(enabledText = "Locked", disabledText = "Free", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.Editor)]
        public bool steeringLocked;

        /// <summary>
        /// If true, steering will be inverted for this wheel.  Toggleable in editor and flight.  Persistant.
        /// </summary>
        [KSPField(guiName = "Invert Steering", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(enabledText = "Inverted", disabledText = "Normal", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.Editor)]
        public bool invertSteering = false;

        /// <summary>
        /// Low speed steering limit 0-100% of maxSteeringAngle
        /// </summary>
        [KSPField(guiName = "Steering Limit Low", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0, maxValue = 1, stepIncrement = 0.01f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.Editor)]
        public float steeringLimit = 1f;

        /// <summary>
        /// High speed steering limit 0-100% of maxSteeringAngle
        /// </summary>
        [KSPField(guiName = "Steering Limit High", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0, maxValue = 1, stepIncrement = 0.01f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.Editor)]
        public float steeringLimitHigh = 1f;

        /// <summary>
        /// Changes the rate of steering from 1 = instant angle transform, ~0.05 = steering turns at realistic rate 
        /// </summary>
        [KSPField(guiName = "Steering Response", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0, maxValue = 1, stepIncrement = 0.01f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.Editor)]
        public float steeringResponse = 1f;

        /// <summary>
        /// Pseudo Ackermann steering geometry. Mirrored symmetry partners use the opposite bias.
        /// </summary>
        [KSPField(guiName = "Steering Bias", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = -1, maxValue = 1, stepIncrement = 0.025f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.Editor)]
        public float steeringBias = 0f;

        /// <summary>
        /// If true, will update the wheels internal steering values.  If false, will only update the visible steering mesh (if specified).
        /// </summary>
        [KSPField]
        public bool updateWheelSteering = true;

        /// <summary>
        /// The ALG disables steering for a user-set wheel angle without changing this module's configured maximum.
        /// </summary>
        internal bool steeringDisabledByAdjustableGear;
        
        /// <summary>
        /// The local axis of the steering transform to rotate around.  Defaults to 0, 1, 0 -- rotate around y+ axis, with z+ facing forward.
        /// </summary>
        [KSPField]
        public Vector3 steeringAxis = Vector3.up;

        [KSPField]
        public bool useSteeringCurve = true;

        [KSPField]
        public FloatCurve steeringCurve = new FloatCurve();

        [KSPField]
        public float steeringCurveMaxSpeed;

        /// <summary>
        /// Reduce the strength of the config set or default steering curve which acts as an assist at high speeds, 100% = full steering curve assist, 0% = no steering curve assist
        /// </summary>
        [KSPField(guiName = "Steering Curve Strength", guiActive = true, guiActiveEditor = true, isPersistant = true, guiFormat = "P0"),
        UI_FloatRange(minValue = 0f, maxValue = 1f, stepIncrement = 0.05f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.Editor)]
        public float steeringCurveStrength = 1f;

        [KSPField]
        public bool showGUISteerLock = true;
        [KSPField]
        public bool showGUISteerInvert = true;
        [KSPField]
        public bool showGUISteerBias = true;
        [KSPField]
        public bool showGUIMaxSteeringAngle = true;
        [KSPField]
        public bool showGUISteerResponse = true;
        [KSPField]
        public bool showGUISteerLimit = true;
        [KSPField]
        public bool showGUISteerCurve = true;

        private Transform steeringTransform;
        private Quaternion defaultRotation;
        private float rotInput;

        internal void onSteeringLocked(BaseField field, System.Object obj)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
            {
                m.steeringLocked = steeringLocked;
            });
        }

        internal void onSteeringInverted(BaseField field, System.Object obj)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
            {
                m.invertSteering = invertSteering;
            });
        }

        internal void onSteeringLimitUpdated(BaseField field, System.Object obj)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
            {
                m.steeringLimit = steeringLimit;
                m.steeringLimitHigh = steeringLimitHigh;
                m.steeringResponse = steeringResponse;
            });
        }

        internal void onSteeringBiasUpdated(BaseField field, System.Object obj)
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                bool mirroredPair = part.symmetryCounterparts != null && part.symmetryCounterparts.Count == 1;
                this.symmetryUpdate(m =>
                {
                    m.steeringBias = mirroredPair && m != this ? -steeringBias : steeringBias;
                    m.updateUIFloatRangeControl(nameof(steeringBias), m.steeringBias, -1f, 1f, 0.025f);
                });
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                // Process each pair once. Start with this module so its chosen sign wins.
                List<KSPWheelSteering> group = new List<KSPWheelSteering> { this };
                this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
                {
                    if (m != this && !group.Contains(m)) { group.Add(m); }
                });
                HashSet<KSPWheelSteering> updated = new HashSet<KSPWheelSteering>();
                foreach (KSPWheelSteering m in group)
                {
                    if (!updated.Add(m)) { continue; }
                    m.steeringBias = steeringBias;
                    m.updateUIFloatRangeControl(nameof(steeringBias), m.steeringBias, -1f, 1f, 0.025f);
                    KSPWheelSteering mirror = m.getMirrorCounterpart();
                    if (mirror != null && updated.Add(mirror))
                    {
                        mirror.steeringBias = -steeringBias;
                        mirror.updateUIFloatRangeControl(nameof(steeringBias), mirror.steeringBias, -1f, 1f, 0.025f);
                    }
                }
            }
        }

        private KSPWheelSteering getMirrorCounterpart()
        {
            if (part.symmetryCounterparts == null || part.symmetryCounterparts.Count != 1) { return null; }
            int moduleIndex = part.Modules.IndexOf(this);
            Part mirrorPart = part.symmetryCounterparts[0];
            return moduleIndex >= 0 && moduleIndex < mirrorPart.Modules.Count
                ? mirrorPart.Modules[moduleIndex] as KSPWheelSteering : null;
        }

        internal void onMaxSteeringAngleUpdated(BaseField field, System.Object obj)
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                this.symmetryUpdate(m =>
                {
                    m.maxSteeringAngle = maxSteeringAngle;
                    m.updateUIFloatEditControl(nameof(maxSteeringAngle), maxSteeringAngle);
                });
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
                {
                    m.symmetryUpdate(s =>
                    {
                        s.maxSteeringAngle = maxSteeringAngle;
                        s.updateUIFloatEditControl(nameof(maxSteeringAngle), maxSteeringAngle);
                    });
                });
            }
        }

        internal void onSteeringCurveStrengthUpdated(BaseField field, System.Object obj)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
            {
                m.steeringCurveStrength = steeringCurveStrength;
                //m.updateUIFloatEditControl(nameof(m.steeringCurveStrength), m.steeringCurveStrength);
            });
        }

        [KSPAction("Lock Steering")]
        public void steeringLockAction(KSPActionParam param)
        {
            steeringLocked = !steeringLocked;
        }

        [KSPAction("Invert Steering")]
        public void steeringInvertAction(KSPActionParam param)
        {
            invertSteering = !invertSteering;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Fields[nameof(maxSteeringAngle)].uiControlEditor.onFieldChanged = Fields[nameof(maxSteeringAngle)].uiControlFlight.onFieldChanged = onMaxSteeringAngleUpdated;
            Fields[nameof(steeringBias)].uiControlEditor.onFieldChanged = Fields[nameof(steeringBias)].uiControlFlight.onFieldChanged = onSteeringBiasUpdated;
            Fields[nameof(steeringLocked)].uiControlFlight.onFieldChanged = onSteeringLocked;
            Fields[nameof(invertSteering)].uiControlFlight.onFieldChanged = onSteeringInverted;
            Fields[nameof(steeringLimit)].uiControlFlight.onFieldChanged = onSteeringLimitUpdated;
            Fields[nameof(steeringLimitHigh)].uiControlFlight.onFieldChanged = onSteeringLimitUpdated;
            Fields[nameof(steeringResponse)].uiControlFlight.onFieldChanged = onSteeringLimitUpdated;
            Fields[nameof(steeringCurveStrength)].uiControlFlight.onFieldChanged = onSteeringCurveStrengthUpdated;
            if (HighLogic.LoadedSceneIsEditor && part.isClone)
            {
                KSPWheelSteering mirror = getMirrorCounterpart();
                if (mirror != null) { steeringBias = -mirror.steeringBias; }
            }
        }

        internal override string getModuleInfo()
        {
            return "Max Steering Deflection: " + maxSteeringAngle + " degrees";
        }

        internal override void onUIControlsUpdated(bool show)
        {
            base.onUIControlsUpdated(show);
            Fields[nameof(steeringLocked)].guiActive = Fields[nameof(steeringLocked)].guiActiveEditor = show && showGUISteerLock;
            Fields[nameof(invertSteering)].guiActive = Fields[nameof(invertSteering)].guiActiveEditor = show && showGUISteerInvert;
            Fields[nameof(steeringLimit)].guiActive = Fields[nameof(steeringLimit)].guiActiveEditor = show && showGUISteerLimit;
            Fields[nameof(steeringLimitHigh)].guiActive = Fields[nameof(steeringLimitHigh)].guiActiveEditor = show && showGUISteerLimit;
            Fields[nameof(steeringResponse)].guiActive = Fields[nameof(steeringResponse)].guiActiveEditor = show && showGUISteerResponse;
            Fields[nameof(steeringBias)].guiActive = Fields[nameof(steeringBias)].guiActiveEditor = show && showGUISteerBias;
            Fields[nameof(maxSteeringAngle)].guiActive = Fields[nameof(maxSteeringAngle)].guiActiveEditor = show && showGUIMaxSteeringAngle;
            Fields[nameof(steeringCurveStrength)].guiActive = Fields[nameof(steeringCurveStrength)].guiActiveEditor = show && showGUISteerCurve;
        }

        internal override void postControllerSetup()
        {
            base.postControllerSetup();
            if (!string.IsNullOrEmpty(steeringName))
            {
                steeringTransform = part.transform.FindChildren(steeringName)[wheelData.indexInDuplicates];
                defaultRotation = steeringTransform.localRotation;
            }
            if (steeringCurve == null || steeringCurve.Curve.length == 0)
            {
                steeringCurve = new FloatCurve();
                steeringCurve.Add(0, 1f, -0.9f, -0.9f);
                steeringCurve.Add(1, 0.1f, -0.9f, -0.9f);
            }
            if (steeringCurveMaxSpeed <= 0)
            {
                KSPWheelDamage dmg = controller.subModules.Find(m => m.wheelIndex == this.wheelIndex && m is KSPWheelDamage) as KSPWheelDamage;
                if (dmg != null && dmg.maxSpeed > 0)
                {
                    steeringCurveMaxSpeed = dmg.maxSpeed;
                }
                else
                {
                    steeringCurveMaxSpeed = controller.GetDefaultMaxSpeed(400);
                }
            }
        }

        internal override void preWheelPhysicsUpdate()
        {
            base.preWheelPhysicsUpdate();
            float rI = -(part.vessel.ctrlState.wheelSteer + part.vessel.ctrlState.wheelSteerTrim);
            if (steeringLocked) { rI = 0; }
            if (invertSteering) { rI = -rI; }
            if (rI < 0)
            {
                rI = rI * (1 - steeringBias);
            }
            if (rI > 0)
            {
                rI = rI * (1 + steeringBias);
            }
            rI = Mathf.Clamp(rI, -1, 1);
            rotInput = Mathf.MoveTowards(rotInput, rI, steeringResponse);
            float perc = Mathf.Clamp01(Mathf.Abs(wheel.wheelLocalVelocity.z) / controller.GetScaledMaxSpeed(steeringCurveMaxSpeed));
            float limit = ((1 - perc) * steeringLimit) + (perc * steeringLimitHigh);
            if (useSteeringCurve)
            {
                float curveLimit = steeringCurve.Evaluate(perc);
                curveLimit = Mathf.Lerp(1f, curveLimit, steeringCurveStrength);
                limit *= curveLimit;
            }
            if (updateWheelSteering)
            {
                wheel.steeringAngle = steeringDisabledByAdjustableGear ? 0f : maxSteeringAngle * rotInput * limit;
            }
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight || steeringTransform == null || wheel==null) { return; }
            steeringTransform.localRotation = defaultRotation;
            if (controller.wheelState == KSPWheelState.DEPLOYED)
            {
                steeringTransform.Rotate(wheel.steeringAngle * steeringAxis, Space.Self);
            }
        }

    }
}
