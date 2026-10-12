using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelTracks : KSPWheelMotor
    {
        [KSPField(guiName = "Track Texture Length", guiActive = true, guiActiveEditor = true, isPersistant = true, guiUnits = "m"),
         UI_FloatEdit(minValue = -20f, maxValue = 20f, incrementLarge = 0.25f, incrementSmall = 0.01f, incrementSlide = 0.01f, sigFigs = 3, affectSymCounterparts = UI_Scene.Editor)]
        public float trackLength = 10f;

        [KSPField]
        public int trackDir = 1;

        [KSPField]
        public int smrIndex = 0;

        [KSPField(guiName = "Invert Track Surface Direction", guiActive = false, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(scene = UI_Scene.All, controlEnabled = true, requireFullControl = false, disabledText = "Standard", enabledText = "Inverted", affectSymCounterparts = UI_Scene.Editor, suppressEditorShipModified = true)]
        public bool invertTrackTexture = false;

        [KSPField(guiName = "Display Track Fwd Rotation", guiActive = false, guiActiveEditor = true, isPersistant = false),
         UI_Toggle(enabledText = "True", disabledText = "False", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.Editor)]
        public bool editorRotation = false;

        private float factorSum;
        private float[] shares;
        private SkinnedMeshRenderer smr;
        private Vector2 offset = Vector2.zero;
        private Material mat;
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Fields[nameof(trackLength)].uiControlEditor.onFieldChanged = Fields[nameof(trackLength)].uiControlFlight.onFieldChanged = trackLengthChanged;
            Fields[nameof(invertTrackTexture)].uiControlEditor.onFieldChanged = Fields[nameof(invertTrackTexture)].uiControlFlight.onFieldChanged = invertTrackTextureClicked;
            if (HighLogic.LoadedSceneIsEditor && part.isClone && invertMirror && part.symmetryCounterparts != null && part.symmetryCounterparts.Count == 1)
            {
                int moduleIndex = part.Modules.IndexOf(this);
                KSPWheelTracks counterpart = part.symmetryCounterparts[0].Modules[moduleIndex] as KSPWheelTracks;
                if (counterpart != null) { invertTrackTexture = !counterpart.invertTrackTexture; }
            }
        }

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            updateTrackTextureScale();
            if (HighLogic.LoadedSceneIsEditor) { return; }
            updateScaleValues();
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
            if (mat != null && controller.wheelState==KSPWheelState.DEPLOYED)
            {
                advanceTrackTexture(wheel.linearVelocity * Time.deltaTime);
            }
        }

        public void Update()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (editorRotation && mat != null && controller.wheelState == KSPWheelState.DEPLOYED)
                {
                    advanceTrackTexture((invertMotor ? -1f : 1f) * Time.deltaTime);
                }
            }
        }

        protected void updateScaleValues()
        {
            if (this.wheel == null) { return; }//wheel not initialized

            KSPWheelCollider wheel;
            int len = controller.wheelData.Length;
            factorSum = 0;
            float[] factors = new float[len];
            shares = new float[len];
            for (int i = 0; i < len; i++)
            {
                wheel = controller.wheelData[i].wheel;
                factors[i] = wheel.momentOfInertia / wheel.radius;
                factorSum += factors[i];
            }
            for (int i = 0; i < len; i++)
            {
                shares[i] = factors[i] / factorSum;
            }
        }

        /// <summary>
        /// This method sets each wheel torque to that which would be needed
        /// exert the same linear velocity change on each wheel in the group.
        /// The moment of inertia and radius of each wheel determines a wheels share
        /// of the torque.
        /// </summary>
        protected override void updateMotor()
        {
            base.updateMotor();
            if (shares == null) { updateScaleValues(); }
            float totalSystemTorque = 0f;
            float totalBrakeTorque = this.wheel.brakeTorque;
            float totalMotorTorque = torqueOutput;
            KSPWheelCollider wheel;
            int len = controller.wheelData.Length;
            for (int i = 0; i < len; i++)
            {
                wheel = controller.wheelData[i].wheel;
                totalSystemTorque += wheel.angularVelocity * wheel.momentOfInertia;
            }
            for (int i = 0; i < len; i++)
            {
                wheel = controller.wheelData[i].wheel;
                wheel.motorTorque = shares[i] * totalMotorTorque;
                wheel.brakeTorque = shares[i] * totalBrakeTorque;
                wheel.angularVelocity = shares[i] * totalSystemTorque / wheel.momentOfInertia;
            }
        }

        private void advanceTrackTexture(float distance)
        {
            if (trackLength == 0f) { return; }
            float scaledLength = trackLength * part.rescaleFactor * controller.scale;
            if (scaledLength == 0f) { return; }

            offset.x = Mathf.Repeat(offset.x + distance * trackDir * (invertTrackTexture ? -1f : 1f) / scaledLength, 1f);
            mat.SetTextureOffset("_MainTex", offset);
            mat.SetTextureOffset("_BumpMap", offset);
        }

        private void trackLengthChanged(BaseField a, System.Object b)
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                this.symmetryUpdate(m =>
                {
                    m.trackLength = trackLength;
                    m.updateUIFloatEditControl(nameof(m.trackLength), m.trackLength);
                });
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
                {
                    m.trackLength = trackLength;
                    m.updateUIFloatEditControl(nameof(m.trackLength), m.trackLength);
                });
            }
        }

        private void invertTrackTextureClicked(BaseField a, System.Object b)
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                this.symmetryUpdate(m =>
                {
                    // Mirror the motor inversion: the opposite side needs opposite UV orientation.
                    m.invertTrackTexture = m == this || !invertMirror || part.symmetryCounterparts.Count != 1
                        ? invertTrackTexture : !invertTrackTexture;
                    m.updateTrackTextureScale();
                });
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
                {
                    m.invertTrackTexture = invertTrackTexture;
                    m.updateTrackTextureScale();
                });
            }
        }

        private void updateTrackTextureScale()
        {
            SkinnedMeshRenderer[] smrs = part.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (smrs != null && smrs.Length > 0)
            {
                smr = part.GetComponentsInChildren<SkinnedMeshRenderer>()[smrIndex];
                if (smr != null)
                {
                    mat = smr.material;
                    if (mat != null)
                    {
                        Vector2 scaling = mat.mainTextureScale;
                        scaling.x = trackDir * (invertTrackTexture ? -1 : 1);
                        mat.SetTextureScale("_MainTex", scaling);
                        //special feature of TU shaders to flip the X coordinate of the normal map, fixes lighting issues with negative UV scaling
                        mat.SetFloat("_NormalFlipX", scaling.x);
                    }
                }
            }
        }

    }
}
