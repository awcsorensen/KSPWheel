using System.Collections.Generic;
using UnityEngine;
using KSPWheel.PartModules;

namespace KSPWheel
{
    /// <summary>
    /// Raises a gear's visual suspension target when raycasts beneath its probes find nearby geometry.
    /// The physical wheel collider and suspension are unchanged. Place probes as child object of wheel mesh.
    /// </summary>
    public class KSPWheelVisualClearance : PartModule
    {
        #region REGION - Standard Part Config File Fields

        /// <summary>
        /// Index among KSPWheelBase modules on this part.
        /// </summary>
        [KSPField]
        public int baseModuleIndex = 0;

        /// <summary>
        /// Index of the physical wheel used for direction and collision layers.
        /// </summary>
        [KSPField]
        public int wheelIndex = 0;

        /// <summary>
        /// Constraint target moved to raise the visual suspension.
        /// </summary>
        [KSPField]
        public string targetName = string.Empty;

        /// <summary>
        /// Visual branch containing the tyre probes.
        /// </summary>
        [KSPField]
        public string visualRootName = string.Empty;

        /// <summary>
        /// Comma-separated names of tyre-centre probe transforms.
        /// </summary>
        [KSPField]
        public string probeNames = string.Empty;

        /// <summary>
        /// Visual tyre radius in model units, before part and wheel scaling.
        /// </summary>
        [KSPField]
        public float tyreRadius = 0f;

        /// <summary>
        /// Extra radial gap around each visual tyre.
        /// </summary>
        [KSPField]
        public float clearance = 0.003f;

        /// <summary>
        /// Maximum visual lift in model units, before scaling.
        /// </summary>
        [KSPField]
        public float maxLift = 0.15f;

        #endregion ENDREGION - Standard Part Config File Fields

        #region REGION - User GUI

        /// <summary>
        /// Current visual lift in scaled world units.
        /// </summary>
        [KSPField(guiActive = true, guiName = "Visual lift (m)", guiFormat = "F1")]
        public float appliedLift;

        /// <summary>
        /// True if the required lift exceeds the cap.
        /// </summary>
        [KSPField(guiActive = true, guiName = "Clearance limit reached")]
        public bool limitReached;

        #endregion ENDREGION - User GUI

        #region REGION - Private Working Variables

        private KSPWheelBase controller;
        private KSPWheelBogey bogey;
        private Transform target;
        private Transform[] probes;
        private readonly List<ConstraintData> constraints = new List<ConstraintData>();

        #endregion ENDREGION - Private Working Variables

        #region REGION - Standard KSP/Unity Overrides

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight) { return; }

            controller = part.GetComponents<KSPWheelBase>()[baseModuleIndex];
            foreach (KSPWheelBogey candidate in part.GetComponents<KSPWheelBogey>())
            {
                if (candidate.baseModuleIndex == baseModuleIndex && candidate.wheelIndex == wheelIndex)
                {
                    bogey = candidate;
                    bogey.probeControlEnabled = true;
                    break;
                }
            }
            target = part.transform.FindChildren(targetName)[0];
            Transform visualRoot = part.transform.FindChildren(visualRootName)[0];

            string[] names = probeNames.Split(',');
            probes = new Transform[names.Length];
            for (int i = 0; i < names.Length; ++i)
            {
                probes[i] = visualRoot.FindChildren(names[i].Trim())[0];
            }

            // Use the same linkage definitions as the part's constraint modules.
            foreach (KSPWheelConstraints module in part.GetComponents<KSPWheelConstraints>())
            {
                ConfigNode root = ConfigNode.Parse(module.configNodeData).nodes[0];
                foreach (ConfigNode node in root.GetNodes("CONSTRAINT"))
                {
                    constraints.Add(new ConstraintData(part, node));
                }
            }
        }

        /// <summary>
        /// Rebuilds the normal visual pose, then adds any needed clearance lift.
        /// </summary>
        public void LateUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || vessel.packed) { return; }

            appliedLift = 0f;
            limitReached = false;
            KSPWheelCollider master = controller.wheelData[wheelIndex].wheel;

            ApplyConstraints();
            if (controller.wheelState != KSPWheelState.DEPLOYED) { return; }

            float scale = part.rescaleFactor * controller.scale;
            float radius = (tyreRadius + Mathf.Max(0f, clearance)) * scale;
            float cap = maxLift * scale;
            float lift = radius + cap;
            Vector3 up = master.transform.up.normalized;
            int worstProbe;
            float required = MeasureRequiredLift(master, up, lift, radius, out worstProbe);

            // Let the lowest tyre act like a second, visual wheel contact.
            // Rotate only if doing so reduces the greatest tyre penetration.
            if (bogey != null && worstProbe >= 0 && required > 0.01f * scale &&
                bogey.groundRotationSpeed > 0f && bogey.ProbeTransform != null &&
                probes[worstProbe].IsChildOf(bogey.ProbeTransform))
            {
                Transform pivot = bogey.ProbeTransform;
                bogey.MarkProbeContact();
                Vector3 axis = pivot.TransformDirection(bogey.bogeyRotAxis.normalized);
                float risePerDegree = Vector3.Dot(up,
                    Vector3.Cross(axis, probes[worstProbe].position - pivot.position)) * Mathf.Deg2Rad;
                if (Mathf.Abs(risePerDegree) > 0.00001f)
                {
                    float degrees = Mathf.Clamp(required / risePerDegree,
                        -bogey.groundRotationSpeed * Time.deltaTime,
                        bogey.groundRotationSpeed * Time.deltaTime);
                    Quaternion before = pivot.localRotation;
                    pivot.Rotate(bogey.bogeyRotAxis, degrees, Space.Self);
                    if (Quaternion.Angle(bogey.ProbeRestRotation, pivot.localRotation) <= bogey.maxBogeyDroopAngle)
                    {
                        ApplyConstraints();
                        float after = MeasureRequiredLift(master, up, lift, radius, out worstProbe);
                        if (after < required - 0.0005f * scale)
                        {
                            required = after;
                        }
                        else
                        {
                            pivot.localRotation = before;
                            ApplyConstraints();
                        }
                    }
                    else
                    {
                        pivot.localRotation = before;
                    }
                }
            }
            else if (bogey != null && master.isGrounded && bogey.groundRotationSpeed > 0f)
            {
                // With no tyre penetration, retain the original ground-normal leveling.
                bogey.FollowGroundNormal();
                ApplyConstraints();
                required = MeasureRequiredLift(master, up, lift, radius, out worstProbe);
            }

            limitReached = required > cap;
            appliedLift = Mathf.Clamp(required, 0f, cap);
            if (appliedLift <= 0f) { return; }

            // Move only the target while updating its linked visual transforms.
            Vector3 original = target.position;
            try
            {
                target.position = original + up * appliedLift;
                ApplyConstraints();
            }
            finally
            {
                target.position = original;
            }
        }

        #endregion ENDREGION - Standard KSP/Unity Overrides

        #region REGION - Custom Update Methods

        private void ApplyConstraints()
        {
            foreach (ConstraintData constraint in constraints)
            {
                constraint.updateConstraint();
            }
        }

        private float MeasureRequiredLift(KSPWheelCollider master, Vector3 up,
            float lift, float radius, out int worstProbe)
        {
            float required = 0f;
            worstProbe = -1;
            for (int i = 0; i < probes.Length; ++i)
            {
                RaycastHit hit;
                if (Physics.Raycast(probes[i].position + up * lift, -up, out hit,
                    lift + radius, master.raycastMask, QueryTriggerInteraction.Ignore))
                {
                    float penetration = radius + lift - hit.distance;
                    if (penetration > required)
                    {
                        required = penetration;
                        worstProbe = i;
                    }
                }
            }
            return required;
        }

        #endregion ENDREGION - Custom Update Methods
    }
}
