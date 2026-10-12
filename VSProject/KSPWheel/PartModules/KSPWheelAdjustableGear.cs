using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelAdjustableGear : KSPWheelSubmodule
    {

        #region REGION - Standard Part Config File Fields

        [KSPField]
        public string suspensionContainer1Name = string.Empty;

        [KSPField]
        public string suspensionContainer2Name = string.Empty;

        [KSPField]
        public string suspensionTargetName = string.Empty;

        [KSPField]
        public string suspensionRotatorName = string.Empty;

        [KSPField]
        public string wheelContainerName = string.Empty;

        [KSPField]
        public string wheelMeshName = string.Empty;

        [KSPField]
        public string rearDoorFlipName = string.Empty;

        [KSPField]
        public string rearDoorName = string.Empty;

        [KSPField]
        public string rightDoorName = string.Empty;

        [KSPField]
        public string leftDoorName = string.Empty;

        [KSPField]
        public string deployEffect = "DeployEffect";

        [KSPField]
        public string deployedEffect = "DeployedEffect";

        [KSPField]
        public string retractEffect = "RetractEffect";

        [KSPField]
        public string retractedEffect = "RetractedEffect";

        /// <summary>
        /// Toggles state to allow the user to 'flip' about mirror line. This actually rotates the whole "Wheel Container" target object 180deg about vertical axis. 
        /// ALGs have constraints to link the visual meshes to this targets children. On non-sideMode ALG this will invert the angle of the main strut as well as the wheel angle.
        /// Typically disabled on sideMode due to only impacting the wheel angle, no real advantage to the user that cannot be solved via manual angle adjustment.
        /// </summary>
        [KSPField]
        public bool allowFlip = false;

        /// <summary>
        /// Toggles state to apply changes suit side folding landing gear, strut folds expected direction, wheel inversions removed, used to distinguish between standard and side folding variants.
        /// </summary>
        [KSPField]
        public bool sideMode = false;

        /// <summary>
        /// Changes the original behaviour of ALG to have "Strut Extension" act upon an extension sleeve object rather than simply extending the suspension strut out of the main strut.
        /// Requires models to have an additional parent part to the "Suspension" mesh object and an empty parent target axis to "WheelContainer" to handle the transform.
        /// All quaternion orientations remain the same as unmodified ALG for the children of these new objects. 
        /// Example of this model hierarchy for modders can be found by importing the .mu model to 3D modelling software.
        /// </summary>
        [KSPField]
        public bool altExtendMode = false;

        [KSPField]
        public string extendTargetName = string.Empty;

        /// <summary>
        /// Toggles state to apply changes to suit trailing arm style suspension i.e. SU-27/30/34 etc front landing gear. 
        /// Uses the position data for "linkageTargetName", "trailingArmName", "wheelMeshName" to calculate dynamic visual suspension ratio based on the "suspensionTravel" value defined within KSPWheelBase module.
        /// Simple rotation transform proportional to physical wheel travel to give an approximation to IRL movement, extreme values will break this... modders please use best judgement for realistic movement.
        /// </summary>
        [KSPField]
        public bool trailingArmMode = false;

        /// <summary>
        /// Front linkage attachment marker on the trailing arm (point c). Must be a child of "trailing arm" object. This supplies position information only, the linkage movement itself uses part CFG constraints.
        /// </summary>
        [KSPField]
        public string linkageTargetName = string.Empty;

        /// <summary>
        /// Arm origin is its centre pivot. This object is used to rotation to the "trailing arm" object and is a the (point b) reference used to calculate visual suspension travel for "trailingArmMode" enabled parts.
        /// The arm must be a child of the visual suspension target so its pivot inherits the reduced strut travel.
        /// </summary>
        [KSPField]
        public string trailingArmName = string.Empty;

        /// <summary>
        /// User-selectable strut-extension.  Makes the entire gear 'longer'.
        /// </summary>
        [KSPField]
        public float maxExtension = 0.5f;

        /// <summary>
        /// User-selectable strut angle min/max (negative value used for min)
        /// </summary>
        [KSPField]
        public float minStrutAngle = -30f;

        /// <summary>
        /// User-selectable strut angle min/max (negative value used for min)
        /// </summary>
        [KSPField]
        public float maxStrutAngle = 30f;

        /// <summary>
        /// User-selectable minimum wheel angle.
        /// </summary>
        [KSPField]
        public float minWheelAngle = -60f;

        /// <summary>
        /// User-selectable maximum wheel angle.
        /// </summary>
        [KSPField]
        public float maxWheelAngle = 60f;

        /// <summary>
        /// Config set door opening angle to prevent wheel clipping with smooth animation. Originally 90deg. 
        /// </summary>
        [KSPField]
        public float doorAngle = 110f;

        /// <summary>
        /// Default retracted angle of wheel bogey.
        /// </summary>
        [KSPField]
        public float wheelBogeyRetractedAngle = 0f;

        /// <summary>
        /// Default retracted secondary angle of main strut. Used to rotate wheel about suspension axis.
        /// </summary>
        [KSPField]
        public float secStrutRetractedAngle = 90f;

        /// <summary>
        /// Current animation state/time.  Stored independently of animation state (which is stored in the base module)
        /// </summary>
        [KSPField(isPersistant = true)]
        public float animationTime = 1f;

        /// <summary>
        /// The speed of the animation. 1 = 1 second.  0.25 = 4 seconds. 0 = non-animated (infinite animation time). Lower values decrease playback speed; higher values increase it.
        /// </summary>
        [KSPField]
        public float animationSpeed = 0.25f;

        /// <summary>
        /// Has user selected flipped wheel or set automatically from cloned state.  Determines wheel angle offset direction, wheel spin direction, door opening directions, others...
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool isFlipped = false;

        /// <summary>
        /// sideMode specific 'isFlipped', used to flip the wheel housing model to display asymmetric side folding landing gear correctly mirrored.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool flipGearState = false;

        [KSPField]
        public bool useResourceDeploy = false;

        [KSPField]
        public bool useResourceRetract = false;

        [KSPField]
        public float deployResourceCost = 0f;

        [KSPField]
        public float retractResourceCost = 0f;

        [KSPField]
        public string deployResourceName = string.Empty;

        [KSPField]
        public string retractResourceName = string.Empty;

        #endregion ENDREGION - Standard Part Config File Fields

        #region REGION - User GUI
        /// <summary>
        /// State for if default transform values have been stored yet.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool userValuesInitialised = false;

        /// <summary>
        /// Toggle for user set Advanced Controls, set to false to disallow user adjustment advanced settings.
        /// </summary>
        [KSPField]
        public bool userAdvancedAdjustments = false; 

        /// <summary>
        /// User-selectable toggle for in-game retraction angle adjustment (set to true for housing-less landing gear models)
        /// </summary>
        [KSPField(guiName = "Advanced Settings", guiActive = false, guiActiveEditor = true, isPersistant = true),
        UI_Toggle(affectSymCounterparts = UI_Scene.All, controlEnabled = true, disabledText = "Hidden", enabledText = "Shown", requireFullControl = false, suppressEditorShipModified = true, scene = UI_Scene.All)]
        public bool showAdvanced = false; //Toggle advanced settings visibility in GUI for user adjustment of retract angle, off axis angle, retain extension in stowage

        /// <summary>
        /// Angular rotation of the main strut during the retract animation;  this is around the horizontal (X) axis, and rotates the main strut upwards into the housing.
        /// </summary>
        [KSPField(guiName = "Retract Angle", guiActive = false, guiActiveEditor = false, isPersistant = true),
        UI_FloatRange(minValue = 0f, maxValue = 110f, stepIncrement = 1f, suppressEditorShipModified = true)]
        public float userMainStrutRetractedAngle = 90f;

        [KSPField]
        public float mainStrutRetractedAngle = 90f;

        public bool showRetractAngle = true; //show GUI section for Retract Angle adjustment

        /// <summary>
        /// Off axis angular rotation of the main strut during the retract animation;  this is around the vertical (Z) axis relative to deployed, and rotates the main strut off forward axis for toe-in/out stowage angles.
        /// </summary>
        [KSPField(guiName = "Off Axis Retract Angle", guiActive = false, guiActiveEditor = false, isPersistant = true),
        UI_FloatRange(minValue = -45f, maxValue = 45f, stepIncrement = 1f, suppressEditorShipModified = true)]
        public float userOffAxisStrutRetractedAngle = 0f;

        public float offAxisStrutRetractedAngle = 0f;

        public bool showOffAxis = true; //show GUI section for Off Axis adjustment

        /// <summary>
        /// Locks user adjusted extension during retraction stage so the landing gear does not change length, for those who don't want magic retraction and prefer a more realistic animation.
        /// </summary>
        [KSPField(guiName = "Lock Extension", guiActive = false, guiActiveEditor = false, isPersistant = true),
        UI_Toggle(affectSymCounterparts = UI_Scene.All, controlEnabled = true, disabledText = "Unlocked", enabledText = "Locked", requireFullControl = false, suppressEditorShipModified = true, scene = UI_Scene.All)]
        public bool lockExtension = false; //show GUI section for Lock Extension

        /// <summary>
        /// Angular rotation of the secondary strut during retract animation; this is around the vertical (Y) axis, and rotates the wheel into the housing.
        /// </summary>
        [KSPField(guiName = "Secondary Retract Angle", guiActive = false, guiActiveEditor = false, isPersistant = true),
        UI_FloatRange(minValue = -90f, maxValue = 90f, stepIncrement = 1f, suppressEditorShipModified = true)]
        public float userSecStrutRetractedAngle = 90f;

        public bool showSecRetractAngle = true; //show GUI section for Secondary Retract Angle adjustment

        /// <summary>
        /// Angular rotation of the wheel bogey during retract animation.  TODO user adjustable field
        /// </summary>
        [KSPField(guiName = "Bogey Retract Angle", guiActive = false, guiActiveEditor = false, isPersistant = true),
        UI_FloatRange(minValue = -110f, maxValue = 110f, stepIncrement = 1f, suppressEditorShipModified = true)]
        public float userWheelBogeyRetractedAngle = 0f;

        public bool showBogeyRetractAngle = true; //show GUI section for Bogey Retract Angle adjustment

        /// <summary>
        /// User-configured main strut angle in editor
        /// </summary>
        [KSPField(guiName = "Strut Angle", guiActive = false, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = -30f, maxValue = 30f, stepIncrement = 0.1f, suppressEditorShipModified = true)]
        public float strutRotation = 0f;

        /// <summary>
        /// User-set secondary angle for wheel container.  Determines the axis along which the suspension operates.
        /// </summary>
        [KSPField(guiName = "Wheel Angle", guiActive = false, guiActiveEditor = true, isPersistant = true, guiFormat = "F1"),
         UI_FloatRange(minValue = 0f, maxValue = 60f, stepIncrement = 0.1f, suppressEditorShipModified = true)]
        public float wheelRotation = 0f;

        /// <summary>
        /// User-set strut extension value.  Makes the landing leg longer or shorter.  Does not effect suspension travel range.
        /// </summary>
        [KSPField(guiName = "Strut Extension", guiActive = false, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0f, maxValue = 1, stepIncrement = 0.05f, suppressEditorShipModified = true)]
        public float strutExtension = 0f;

        /// <summary>
        /// Temporary testing compression value -- TODO remove once module is finished being developed
        /// </summary>
        [KSPField(guiName = "Comp Test", guiActive = false, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0f, maxValue = 1f, stepIncrement = 0.05f, suppressEditorShipModified = true)]
        public float compTest = 1f;

        #endregion ENDREGION - User GUI

        #region REGION - Private Working Variables

        internal List<KSPWheelSubmodule> subModules = new List<KSPWheelSubmodule>();

        private float roundToIncrement(float value, float increment) //round numbers to 1 decimal place to prevent ugly numbers showing up
        {
            float rounded = Mathf.Round(value / increment) * increment;

            if (Mathf.Abs(rounded) < increment * 0.001f) //prevent -0.0 or tiny floating point remnants
            {
                rounded = 0f;
            }

            return rounded;
        }
        /// <summary>
        /// Cached transforms for manipulation of the model
        /// </summary>
        private Transform suspensionContainer1;
        private Transform suspensionContainer2;
        private Transform suspensionTarget;
        private Transform suspensionRotator;
        private Transform wheelContainer;
        private Transform wheelMesh;
        private Transform extendTarget;
        private Transform trailingArm;
        private Transform linkageTarget;

        private Transform leftDoor;
        private Transform rightDoor;
        private Transform rearDoor;
        private Transform rearDoorFlip;

        private SphereCollider tempCollider;

        private float prevDragUpdateState = -1f; //initialize to negative value to force drag cube updating on first update tick
        
        /// <summary>
        /// Visual strut displacement from physical wheel travel and arm geometry.
        /// </summary>
        public float visualSuspensionTravel { get; private set; }

        /// <summary>
        /// Cached default orientations and locations for the above transforms
        /// Serialize these fields across parts (prefab -> editor; editor -> cloned)
        /// Fixes problems of cloned models taking on new default orientations/locations from the part they were cloned from
        /// </summary>
        [SerializeField]
        private bool initializedDefaultRotations;
        [SerializeField]
        private Quaternion sc1DefaultRotation;
        [SerializeField]
        private Quaternion sc2DefaultRotation;
        [SerializeField]
        private Quaternion wheelContainerDefaultRotation;
        [SerializeField]
        private Vector3 wheelContainerDefaultPosition;
        [SerializeField]
        private Quaternion leftDoorDefaultRotation;
        [SerializeField]
        private Quaternion rightDoorDefaultRotation;
        [SerializeField]
        private Quaternion rearDoorDefaultRotation;
        [SerializeField]
        private Quaternion rearDoorFlipDefaultRotation;
        [SerializeField]
        private bool initializedExtendTargetTransforms;
        [SerializeField]
        private Vector3 extendTargetDefaultPosition;
        [SerializeField]
        private Quaternion extendTargetDefaultRotation;
        [SerializeField]
        private Quaternion trailingArmDefaultRotation;

        #endregion ENDREGION - Private Working Variables

        #region REGION - GUI Methods

        [KSPAction(actionGroup = KSPActionGroup.Gear, guiName = "Toggle Gear", requireFullControl = false)]
        public void deployAction(KSPActionParam param)
        {
            if (param.type == KSPActionType.Activate)
            {
                switch (controller.wheelState)
                {
                    case KSPWheelState.RETRACTED:
                        if (checkResourceUse(KSPWheelState.DEPLOYING))
                        {
                            changeWheelState(KSPWheelState.DEPLOYING, true);
                            part.Effect(deployEffect, 1f);
                        }
                        break;
                    case KSPWheelState.RETRACTING:
                        if (checkResourceUse(KSPWheelState.DEPLOYING))
                        {
                            changeWheelState(KSPWheelState.DEPLOYING, true);
                            part.Effect(deployEffect, 1f);
                        }
                        break;
                    default:
                        break;
                }
            }
            else//if param.type==KSPActionType.Deactivate
            {
                switch (controller.wheelState)
                {
                    case KSPWheelState.DEPLOYED:
                        if (checkResourceUse(KSPWheelState.RETRACTING))
                        {
                            changeWheelState(KSPWheelState.RETRACTING, true);
                            part.Effect(retractEffect, 1f);
                        }
                        break;
                    case KSPWheelState.DEPLOYING:
                        if (checkResourceUse(KSPWheelState.RETRACTING))
                        {
                            changeWheelState(KSPWheelState.RETRACTING, true);
                            part.Effect(retractEffect, 1f);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        [KSPEvent(guiName = "Toggle Gear", guiActive = true, guiActiveEditor = true)]
        public void deploy()
        {
            this.symmetryUpdate(m =>
            {
                switch (m.controller.wheelState)
                {
                    case KSPWheelState.RETRACTED:
                        if (checkResourceUse(KSPWheelState.DEPLOYING))
                        {
                            m.changeWheelState(KSPWheelState.DEPLOYING, true);
                            part.Effect(deployEffect, 1f);
                        }
                        break;
                    case KSPWheelState.RETRACTING:
                        if (checkResourceUse(KSPWheelState.DEPLOYING))
                        {
                            m.changeWheelState(KSPWheelState.DEPLOYING, true);
                            part.Effect(deployEffect, 1f);
                        }
                        break;
                    case KSPWheelState.DEPLOYED:
                        if (checkResourceUse(KSPWheelState.RETRACTING))
                        {
                            m.changeWheelState(KSPWheelState.RETRACTING, true);
                            part.Effect(retractEffect, 1f);
                        }
                        break;
                    case KSPWheelState.DEPLOYING:
                        if (checkResourceUse(KSPWheelState.RETRACTING))
                        {
                            m.changeWheelState(KSPWheelState.RETRACTING, true);
                            part.Effect(retractEffect, 1f);
                        }
                        break;
                    case KSPWheelState.BROKEN:
                        break;
                    default:
                        break;
                }
            });
        }

        [KSPEvent(guiName = "Flip Gear", guiActive = false, guiActiveEditor = true)]
        public void flip()
        {
            if (!allowFlip) { return; }
            isFlipped = !isFlipped; //original flip handler
            flipGearState = !flipGearState; //new function for side mode flip
            this.symmetryUpdate(m =>
            {
                if (m.trailingArmMode)
                {
                    KSPWheelMotor motor = Array.Find(m.part.GetComponents<KSPWheelMotor>(),
                        motorModule => motorModule.baseModuleIndex == m.baseModuleIndex && motorModule.wheelIndex == m.wheelIndex);
                    if (motor != null) { motor.invertMotor = !motor.invertMotor; }
                }
                if (m != this)
                {
                    m.isFlipped = !this.isFlipped; //preserve existing opposite flipped relationship
                    m.flipGearState = this.flipGearState; //both parts recieve same flip state
                }
            });
        }

        [KSPEvent(guiName = "Align Wheel to Ground", guiActive = false, guiActiveEditor = true)]
        public void alignToGround()
        {
            if (trailingArmMode)
            {
                this.symmetryUpdate(m =>
                {
                    m.updateAnimation(0f);
                    Vector3 localUp = m.wheelContainer.InverseTransformDirection(Vector3.up);
                    float alignmentAngle = -Mathf.Atan2(localUp.x, localUp.y) * Mathf.Rad2Deg;
                    if (m.isFlipped) { alignmentAngle = -alignmentAngle; }
                    m.wheelRotation = Mathf.Clamp(m.roundToIncrement(m.wheelRotation + alignmentAngle, 0.1f), m.minWheelAngle, m.maxWheelAngle);
                    m.updateAnimation(0f);
                });
                return;
            }
            Vector3 target = wheelContainer.position + Vector3.up;//one unit above the transform, in world-space in the editor
            Vector3 localTarget = wheelContainer.InverseTransformPoint(target);//one unit above the transform, as seen in local space
                                                                               //rotating around the local Z axis, so we only care about the x and y offsets
                                                                               //erm.. feed this into Mathf.Atan2 as a slope, to get the returned angle
            float angle = -Mathf.Atan2(localTarget.x, localTarget.y) * Mathf.Rad2Deg;
            //modify align wheel range to work from minWheelAngle to maxWheelAngle, rather than limited to positive values
            wheelRotation = Mathf.Clamp(roundToIncrement(wheelRotation + angle, 0.1f), minWheelAngle, maxWheelAngle);//clamp it to the current wheel angle limits
            this.symmetryUpdate(m =>
            {
                m.wheelRotation = wheelRotation;
            });
        }

        /// <summary>
        /// Checks for AND CONSUMES resources for change to the specified state.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private bool checkResourceUse(KSPWheelState state)
        {
            if (state == KSPWheelState.DEPLOYING && useResourceDeploy && !string.IsNullOrEmpty(deployResourceName) && deployResourceCost > 0)
            {
                double used = part.RequestResource(deployResourceName, (double)deployResourceCost);
                if (used < deployResourceCost)//if not sufficient, return it to the part
                {
                    part.RequestResource(deployResourceName, -used);
                    return false;
                }
                return true;
            }
            else if (state == KSPWheelState.RETRACTING && useResourceRetract && !string.IsNullOrEmpty(retractResourceName) && retractResourceCost > 0)
            {
                double used = part.RequestResource(retractResourceName, (double)retractResourceCost);
                if (used < retractResourceCost)//if not sufficient, return it to the part
                {
                    part.RequestResource(retractResourceName, -used);
                    return false;
                }
                return true;
            }
            return true;
        }

        //NEW below
        public void updateGUIVisibility()
        {
            bool advancedVisible = userAdvancedAdjustments && showAdvanced;

            Fields[nameof(showAdvanced)].guiActiveEditor = userAdvancedAdjustments;
            Fields[nameof(userMainStrutRetractedAngle)].guiActiveEditor = advancedVisible && showRetractAngle;
            Fields[nameof(userOffAxisStrutRetractedAngle)].guiActiveEditor = advancedVisible && showOffAxis;
            Fields[nameof(lockExtension)].guiActiveEditor = advancedVisible;
            Fields[nameof(userSecStrutRetractedAngle)].guiActiveEditor = advancedVisible && showSecRetractAngle;
            Fields[nameof(userWheelBogeyRetractedAngle)].guiActiveEditor = advancedVisible && showBogeyRetractAngle;
            Events[nameof(resetRetractAngle)].guiActiveEditor = advancedVisible && showResetRetract;
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

        //reset retraction angle to stored default config value
        [KSPEvent(guiName = "Reset Retract Angle", guiActive = false, guiActiveEditor = false)]
        public void resetRetractAngle()
        {
            userMainStrutRetractedAngle = mainStrutRetractedAngle;
            userOffAxisStrutRetractedAngle = offAxisStrutRetractedAngle;
            userSecStrutRetractedAngle = secStrutRetractedAngle;
            userWheelBogeyRetractedAngle = wheelBogeyRetractedAngle;
            // Update symmetry counterparts
            this.symmetryUpdate(m =>
            {
                m.userMainStrutRetractedAngle = m.mainStrutRetractedAngle;
                m.userOffAxisStrutRetractedAngle = m.offAxisStrutRetractedAngle;
                m.userSecStrutRetractedAngle = m.secStrutRetractedAngle;
                m.userWheelBogeyRetractedAngle = m.wheelBogeyRetractedAngle;
            });
        }
        private bool showResetRetract = true;

        //Initialise user-adjustable retraction values from the part CFG defaults.
        //After initialisation, user values are only changed by the GUI or Reset event.
        public void initialiseUserValues()
        {
            if (!userValuesInitialised)
            {
                userMainStrutRetractedAngle = mainStrutRetractedAngle;
                userOffAxisStrutRetractedAngle = offAxisStrutRetractedAngle;
                userSecStrutRetractedAngle = secStrutRetractedAngle;
                userWheelBogeyRetractedAngle = wheelBogeyRetractedAngle;

                userValuesInitialised = true;
            }
        }

        #endregion ENDREGION - GUI Methods

        #region REGION - Standard KSP/Unity Overrides

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            locateTransforms();
            Events[nameof(flip)].guiActiveEditor = allowFlip;
            this.updateUIFloatRangeControl(nameof(strutRotation), strutRotation, minStrutAngle, maxStrutAngle, 0.5f);
            this.updateUIFloatRangeControl(nameof(wheelRotation), wheelRotation, minWheelAngle, maxWheelAngle, 0.5f);
            Fields[nameof(showAdvanced)].uiControlEditor.onFieldChanged = onShowUIUpdated;//connect the advanced settings toggle to its UI update handler

            //set initial visibility
            onShowUIUpdated(Fields[nameof(showAdvanced)], null);
            initialiseUserValues();
        }

        internal override void postControllerSetup()
        {
            base.postControllerSetup();
            if (vessel != null && controller != null)
            {
                bool state = vessel.ActionGroups[KSPActionGroup.Gear];
                vessel.ActionGroups[KSPActionGroup.Gear] = state || controller.wheelState == KSPWheelState.DEPLOYED;
            }
        }

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            // The ALG container already rotates its children by wheel.steeringAngle.
            // A collider inside it must not apply the same angle again to its forward direction.
            wheel.steeringAngleAppliedByTransform = wheel.transform == wheelContainer || wheel.transform.IsChildOf(wheelContainer);
            if (part.symmetryCounterparts != null && part.symmetryCounterparts.Count > 0)
            {
                //this must be a clone, or part is being reloaded
                //the 'symmetry counterpart' that exists should be the original part
                this.isFlipped = !part.symmetryCounterparts[0].GetComponent<KSPWheelAdjustableGear>().isFlipped;
            }
            if (HighLogic.LoadedSceneIsFlight)
            {
                tempCollider = new GameObject("StandInCollider").AddComponent<SphereCollider>();
                tempCollider.radius = wheel.radius;
                tempCollider.gameObject.layer = 26;
                tempCollider.transform.parent = suspensionTarget;
                tempCollider.transform.position = wheel.transform.position;
                CollisionManager.IgnoreCollidersOnVessel(vessel, tempCollider);//have to run this before the collider is disabled
                tempCollider.enabled = controller.wheelState == KSPWheelState.RETRACTING || controller.wheelState == KSPWheelState.DEPLOYING;
                KSPWheelSteering steering = part.GetComponent<KSPWheelSteering>();
                if (steering != null)
                {
                    steering.steeringDisabledByAdjustableGear = Mathf.Abs(wheelRotation) >= 0.25f;
                }
            }
            updateAnimation(0f);//force update the animation based on current time
        }

        public void Update()
        {
            base.preWheelFrameUpdate();
            float animTime = 0f;
            if (controller.wheelState == KSPWheelState.DEPLOYING)
            {
                animTime = Time.deltaTime * animationSpeed;
                part.Effect(retractEffect, 0f);
            }
            else if (controller.wheelState == KSPWheelState.RETRACTING)
            {
                animTime = Time.deltaTime * -animationSpeed;
                part.Effect(deployEffect, 0f);
            }
            else if (controller.wheelState == KSPWheelState.DEPLOYED)
            {
                float wheelSpin = wheel.perFrameRotation;
                if (trailingArmMode && Vector3.Dot(wheelMesh.right, wheel.transform.right) < 0f) { wheelSpin = -wheelSpin; }
                wheelMesh.Rotate(wheelSpin, 0, 0, Space.Self);
                part.Effect(deployEffect, 0f);
                part.Effect(retractEffect, 0f);
            }
            else
            {
                part.Effect(deployEffect, 0f);
                part.Effect(retractEffect, 0f);
            }
            updateAnimation(animTime);
            float diff = prevDragUpdateState > animationTime ? prevDragUpdateState - animationTime : animationTime - prevDragUpdateState;
            if (HighLogic.LoadedSceneIsFlight)
            {
                part.DragCubes.SetCubeWeight("Retracted", 1f - animationTime);
                part.DragCubes.SetCubeWeight("Deployed", animationTime);
            }
            if (diff > 0.1f || (prevDragUpdateState != animationTime && (animationTime <= 0 || animationTime >= 1)))
            {
                part.SendMessage("GeometryPartModuleRebuildMeshData");
                prevDragUpdateState = animationTime;
            }
        }

        #endregion ENDREGION - Standard KSP/Unity Overrides

        #region REGION - Custom Update Methods

        internal override void onStateChanged(KSPWheelState oldState, KSPWheelState newState)
        {
            base.onStateChanged(oldState, newState);
            if (tempCollider != null)//can be null in the editor?
            {
                tempCollider.enabled = newState == KSPWheelState.RETRACTING || newState == KSPWheelState.DEPLOYING;
                if (tempCollider.enabled)
                {
                    CollisionManager.IgnoreCollidersOnVessel(vessel, tempCollider);
                }
            }
        }

        /// <summary>
        /// Locates all of the relevant transforms for this module, and sets up their default orientation/location cached values
        /// </summary>
        private void locateTransforms()
        {
            //locate required transforms
            suspensionContainer1 = part.transform.FindRecursive(suspensionContainer1Name);
            suspensionContainer2 = part.transform.FindRecursive(suspensionContainer2Name);
            suspensionTarget = part.transform.FindRecursive(suspensionTargetName);
            suspensionRotator = part.transform.FindRecursive(suspensionRotatorName);
            wheelContainer = part.transform.FindRecursive(wheelContainerName);
            wheelMesh = part.transform.FindRecursive(wheelMeshName);

            if (altExtendMode)
            {
                extendTarget = part.transform.FindRecursive(extendTargetName);
            }

            if (trailingArmMode)
            {
                trailingArm = part.transform.FindRecursive(trailingArmName);
                linkageTarget = part.transform.FindRecursive(linkageTargetName);
            }

            //locate doors
            leftDoor = part.transform.FindRecursive(leftDoorName);
            rightDoor = part.transform.FindRecursive(rightDoorName);
            rearDoor = part.transform.FindRecursive(rearDoorName);
            rearDoorFlip = part.transform.FindRecursive(rearDoorFlipName);

            //cache original transform states
            if (!initializedDefaultRotations)
            {
                initializedDefaultRotations = true;
                sc1DefaultRotation = suspensionContainer1.localRotation;
                sc2DefaultRotation = suspensionContainer2.localRotation;
                wheelContainerDefaultRotation = wheelContainer.localRotation;
                wheelContainerDefaultPosition = wheelContainer.localPosition;

                leftDoorDefaultRotation = leftDoor.localRotation;
                rightDoorDefaultRotation = rightDoor.localRotation;
                rearDoorDefaultRotation = rearDoor.localRotation;
                rearDoorFlipDefaultRotation = rearDoorFlip.localRotation;
            }

            if ((altExtendMode || trailingArmMode) && !initializedExtendTargetTransforms)
            {
                initializedExtendTargetTransforms = true;
                if (altExtendMode)
                {
                    extendTargetDefaultPosition = extendTarget.localPosition;
                    extendTargetDefaultRotation = extendTarget.localRotation;
                }
                if (trailingArmMode)
                {
                    trailingArmDefaultRotation = trailingArm.localRotation;
                }
            }
        }

        private void updateAnimation(float dt)
        {
            animationTime += dt;
            float lrp = 0f;
            bool deployed = animationTime >= 1f;

            float animationStart = 0.15f;
            float animationEnd = 0.85f;

            float absRetractAngle = Mathf.Abs(userMainStrutRetractedAngle);
            float absBogeyAngle = Mathf.Abs(userWheelBogeyRetractedAngle);
            bool bogeyHasMovement = absBogeyAngle > 0.001f;
            bool needs90DegreeStage = absRetractAngle > 90f;
            bool bogeyNeeds90DegreeStage = absBogeyAngle > 90f;
            float transitionAngle = Mathf.Sign(userMainStrutRetractedAngle) * 90f;
            float bogeyTransitionAngle = Mathf.Sign(userWheelBogeyRetractedAngle) * 90f;
            float transitionTime = animationStart;
            float bogeyTransitionTime = animationStart;

            if (needs90DegreeStage)
            {
                transitionTime = Mathf.Lerp(animationStart, animationEnd, (absRetractAngle - 90f) / absRetractAngle);
            }
            if (bogeyNeeds90DegreeStage)
            {
                bogeyTransitionTime = Mathf.Lerp(animationStart, animationEnd, (absBogeyAngle - 90f) / absBogeyAngle);
            }

            float mainStrutRot = 0f;
            float secStrutRot = 0f;
            float strutAngleRot = 0f;
            float wheelAngleRot = 0f;
            float doorLeftRot = 0f;
            float doorRightRot = 0f;
            float doorRearRot = 0f;
            float doorFlipRot = isFlipped && allowFlip ? 180f : 0f;
            float susTargetPos = 0f;
            float bogeyAngleRot = 0f;
            float offAxisRot = 0f;
            if (animationTime <= 0)//fully retracted, everything in retracted state
            {
                animationTime = 0f;
                if (controller.wheelState != KSPWheelState.RETRACTED)
                {
                    changeWheelState(KSPWheelState.RETRACTED, true);
                    part.Effect(retractedEffect);
                }
                mainStrutRot = userMainStrutRetractedAngle;
                secStrutRot = userSecStrutRetractedAngle;
                bogeyAngleRot = userWheelBogeyRetractedAngle;
                strutAngleRot = 0f;
                wheelAngleRot = 0f;
                doorLeftRot = 0f;
                doorRightRot = 0f;
                doorRearRot = 0f;
                susTargetPos = 0f;
                offAxisRot = userOffAxisStrutRetractedAngle;
            }
            else if (animationTime < animationStart)//open doors
            {
                lrp = lerp(animationTime, 0, animationStart);
                mainStrutRot = userMainStrutRetractedAngle;
                secStrutRot = userSecStrutRetractedAngle;
                bogeyAngleRot = userWheelBogeyRetractedAngle;
                strutAngleRot = 0f;
                wheelAngleRot = 0f;
                doorLeftRot = lrp * doorAngle;
                doorRightRot = lrp * doorAngle;
                doorRearRot = lrp * doorAngle;
                susTargetPos = 0f;
                offAxisRot = userOffAxisStrutRetractedAngle;
            }
            else if (animationTime < animationEnd)
            {
                // Main strut and bogey each use their own transition timing
                mainStrutRot = getStagedRetractRotation(animationTime, userMainStrutRetractedAngle, transitionAngle, transitionTime, needs90DegreeStage, animationStart, animationEnd);

                bogeyAngleRot = bogeyHasMovement ? getStagedRetractRotation(animationTime, userWheelBogeyRetractedAngle, bogeyTransitionAngle, bogeyTransitionTime, bogeyNeeds90DegreeStage, animationStart, animationEnd) : 0f;

                // Keep other adjustments inactive during main strut's >90° transition
                if (needs90DegreeStage && animationTime < transitionTime)
                {
                    secStrutRot = userSecStrutRetractedAngle;
                    strutAngleRot = 0f;
                    wheelAngleRot = 0f;
                    susTargetPos = 0f;
                    offAxisRot = userOffAxisStrutRetractedAngle;
                }
                else
                {
                    float mainStartTime = needs90DegreeStage ? transitionTime : animationStart;

                    lrp = lerp(animationTime, mainStartTime, animationEnd);

                    secStrutRot = (1f - lrp) * userSecStrutRetractedAngle;
                    strutAngleRot = strutRotation * lrp;
                    wheelAngleRot = wheelRotation * lrp;
                    susTargetPos = lrp;
                    offAxisRot = (1f - lrp) * userOffAxisStrutRetractedAngle;
                }

                doorLeftRot = doorAngle;
                doorRightRot = doorAngle;
                doorRearRot = doorAngle + (allowFlip ? Mathf.Max(0, -strutRotation) : 0f);
            }
            else if (animationTime < 1.0f)//last stage before fully deployed. close back end doors, lerp into user-configured positions
            {
                lrp = lerp(animationTime, animationEnd, 1f);
                mainStrutRot = 0f;
                secStrutRot = 0f;
                bogeyAngleRot = 0f;
                strutAngleRot = strutRotation;
                wheelAngleRot = wheelRotation;
                doorLeftRot = (1 - lrp) * doorAngle;
                doorRightRot = (1 - lrp) * doorAngle;
                doorRearRot = doorAngle + (allowFlip ? Mathf.Max(0, -strutRotation) : 0);
                susTargetPos = 1f;
                offAxisRot = 0f;
            }
            else if (animationTime >= 1.0f)//fully deployed
            {
                animationTime = 1.0f;
                if (controller.wheelState != KSPWheelState.DEPLOYED)
                {
                    changeWheelState(KSPWheelState.DEPLOYED, true);
                    part.Effect(deployedEffect);
                }
                mainStrutRot = 0f;
                secStrutRot = 0f;
                bogeyAngleRot = 0f;
                strutAngleRot = strutRotation;
                wheelAngleRot = wheelRotation;
                doorLeftRot = 0f;
                doorRightRot = 0f;
                doorRearRot = doorAngle + (allowFlip ? Mathf.Max(0, -strutRotation) : 0);
                susTargetPos = 1f;
                offAxisRot = 0f;
            }

            if (isFlipped)
            {
                offAxisRot = -offAxisRot; //toe direction mirrors for both standard and sidemode
                secStrutRot = -secStrutRot;
                bogeyAngleRot = -bogeyAngleRot;
                if (trailingArmMode) { wheelAngleRot = -wheelAngleRot; }
            }
            if (isFlipped && !sideMode)
            {
                strutAngleRot = -strutAngleRot;
                //secStrutRot = -secStrutRot;
                //bogeyAngleRot = -bogeyAngleRot;
            }

            leftDoor.localRotation = leftDoorDefaultRotation;
            rightDoor.localRotation = rightDoorDefaultRotation;
            rearDoor.localRotation = rearDoorDefaultRotation;
            rearDoorFlip.localRotation = rearDoorFlipDefaultRotation;
            //update door rotations from animation state
            leftDoor.Rotate(doorLeftRot, 0, 0, Space.Self);
            rightDoor.Rotate(doorRightRot, 0, 0, Space.Self);
            rearDoor.Rotate(doorRearRot, 0, 0, Space.Self);
            rearDoorFlip.Rotate(0, 0, doorFlipRot, Space.Self);

            suspensionContainer1.localRotation = sc1DefaultRotation;//user strut angle setting
            suspensionContainer1.Rotate(0, 0, offAxisRot, Space.Self);//user off axis retract strut angle setting
            if (altExtendMode)
            {
                extendTarget.localPosition = extendTargetDefaultPosition;
                extendTarget.localRotation = extendTargetDefaultRotation * wheelContainerDefaultRotation;
                wheelContainer.localRotation = Quaternion.identity;
            }
            else
            {
                wheelContainer.localRotation = wheelContainerDefaultRotation;//user wheel angle setting
            }
            wheelContainer.localPosition = wheelContainerDefaultPosition;//user strut extension setting
            float extensionAnimationPosition = lockExtension && userAdvancedAdjustments ? 1f : susTargetPos;//extension behaviour: unlocked = extension follows normal animation, locked = user selected extension remains fully applied
            if (altExtendMode)
            {
                extendTarget.position -= extendTarget.up * controller.scale * strutExtension * maxExtension * extensionAnimationPosition;
            }
            else
            {
                wheelContainer.position -= wheelContainer.up * controller.scale * strutExtension * maxExtension * extensionAnimationPosition;
            }
            suspensionContainer1.Rotate(strutAngleRot, 0, 0, Space.Self);//user strut angle setting
            if (altExtendMode)
            {
                extendTarget.RotateAround(suspensionContainer1.position, sideMode ? suspensionContainer1.right : suspensionContainer1.up, mainStrutRot);
            }

            bool flipWheelHousing = (!sideMode && isFlipped) || (sideMode && flipGearState);
            if (trailingArmMode) { flipWheelHousing = sideMode ? isFlipped : flipGearState; }
            if (flipWheelHousing)
            {
                wheelContainer.Rotate(0, 180, 0, Space.Self);
            }

            //change sequence of rotation tranforms to stop inversion of targets
            wheelContainer.Rotate(0, secStrutRot + wheel.steeringAngle, 0, Space.Self);
            wheelContainer.Rotate(0, 0, wheelAngleRot, Space.Self);
            float wheelSuspensionTravel = HighLogic.LoadedSceneIsFlight ? (wheel.length - wheel.compressionDistance) : (wheel.length * (1 - compTest));
            Vector3 suspensionBasePosition = wheelContainer.position;
            Vector3 visualTravelDirection = altExtendMode ? -extendTarget.up : -wheelContainer.up;

            Vector3 p2 = suspensionBasePosition + visualTravelDirection * wheelSuspensionTravel;
            suspensionTarget.position = trailingArmMode ? suspensionBasePosition : Vector3.Lerp(suspensionBasePosition, p2, susTargetPos);
            suspensionTarget.rotation = wheelContainer.rotation;
            if (!altExtendMode)
            {
                suspensionTarget.RotateAround(suspensionContainer1.position, sideMode ? suspensionContainer1.right : suspensionContainer1.up, mainStrutRot);
            }
            suspensionTarget.Rotate(-bogeyAngleRot, 0, 0, Space.Self);

            if (trailingArmMode)
            {
                updateTrailingArmVisual(wheelSuspensionTravel * susTargetPos, visualTravelDirection);
            }
            else
            {
                visualSuspensionTravel = wheelSuspensionTravel * susTargetPos;
            }

            suspensionRotator.rotation = suspensionTarget.rotation;
            suspensionRotator.Rotate(bogeyAngleRot, 0, 0, Space.Self);
            suspensionRotator.LookAtLocked(suspensionContainer1.position, Vector3.up, Vector3.forward);
            suspensionContainer2.localRotation = sc2DefaultRotation;

            if (susTargetPos > 0)
            {
                suspensionContainer2.LookAtLocked(suspensionTarget.position, Vector3.back, Vector3.right);
            }
        }

        private float lerp(float time, float pStart, float pEnd)
        {
            float p = pEnd - pStart;
            float t = time - pStart;
            return p <= 0 ? 0 : t / p;
        }

        /// <summary>
        /// S is actual wheel travel; a is the wheel mesh origin, b is the trailing arm pivot, and c is linkageTarget, y is the vertical movement of the suspension strut (visual pivot). 
        /// The visual pivot travels: y = S * (c-b) / (a-c). The remaining wheel travel comes from a single rotation of the trailing arm about its local X axis.
        /// </summary>
        private void updateTrailingArmVisual(float requestedWheelTravel, Vector3 travelDirection)
        {
            trailingArm.localRotation = trailingArmDefaultRotation;

            Vector3 armAxis = trailingArm.right;
            Vector3 pivot = trailingArm.position;
            Vector3 wheelVector = Vector3.ProjectOnPlane(wheelMesh.position - pivot, armAxis);
            Vector3 frontVector = Vector3.ProjectOnPlane(linkageTarget.position - pivot, armAxis);
            float frontLength = frontVector.magnitude;                      // (c-b)
            float armLength = (wheelVector - frontVector).magnitude;        // (a-c)

            visualSuspensionTravel = requestedWheelTravel * frontLength / armLength;
            float angle = Mathf.Asin(Mathf.Clamp(visualSuspensionTravel / frontLength, -1f, 1f)) * Mathf.Rad2Deg;
            float angleDirection = Mathf.Sign(-Vector3.Dot(Vector3.Cross(armAxis, frontVector), travelDirection));

            suspensionTarget.position += travelDirection * visualSuspensionTravel;
            trailingArm.Rotate(angleDirection * angle, 0f, 0f, Space.Self);
        }

        private float getStagedRetractRotation(float animationTime, float retractAngle, float transitionAngle, float transitionTime, bool needs90DegreeStage, float animationStart, float animationEnd)

        {
            if (needs90DegreeStage && animationTime < transitionTime)
            {
                float t = lerp(animationTime, animationStart, transitionTime);
                return Mathf.Lerp(retractAngle, transitionAngle, t);
            }

            float startTime = needs90DegreeStage ? transitionTime : animationStart;
            float t2 = lerp(animationTime, startTime, animationEnd);

            float startAngle = needs90DegreeStage ? transitionAngle : retractAngle;

            return Mathf.Lerp(startAngle, 0f, t2);
        }

        #endregion ENDREGION - Custom Update Methods

    }

}
