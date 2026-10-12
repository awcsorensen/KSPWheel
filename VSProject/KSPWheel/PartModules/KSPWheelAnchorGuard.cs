using System.Collections;
using UnityEngine;

namespace KSPWheel
{
    /// <summary>
    /// Keeps KSP's vessel anchor off KSPWheel craft until they have remained
    /// stationary for at least half a second, with or without brakes.
    /// A tank turn is not stationary even when linear vessel speed is zero.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public class KSPWheelAnchorGuard : VesselModule
    {
        private const float RequiredStopSeconds = 0.5f;
        private const float StoppedSpeedSquared = 0.0001f; // (0.01 m/s)^2
        private const float MovingSpeedSquared = 0.0025f; // (0.05 m/s)^2
        private const float StoppedAngularSpeedSquared = 0.000076154f; // (0.5 degrees/s)^2
        private const float MovingAngularSpeedSquared = 0.00761544f; // (5 degrees/s)^2
        private const float DriveInputThreshold = 0.01f;

        private float stoppedSeconds;
        private bool hasKSPWheel;
        private bool hasDrivenMotor;
        private bool hasTankSteeringMotor;
        private bool anchorPermitted;

        protected override void OnStart()
        {
            base.OnStart();
            StartCoroutine(RemovePrematureAnchors());
        }

        public void FixedUpdate()
        {
            RefreshWheelState();
            if (!hasKSPWheel || vessel == null || !vessel.loaded || vessel.packed || HasDriveInput())
            {
                ResetGate();
            }
            else if (anchorPermitted)
            {
                // Once qualified, leave an existing anchor alone. Only clear
                // permission after KSP has released it and the craft moves away.
                if (!vessel.IsAnchored && (vessel.rb_velocity.sqrMagnitude > MovingSpeedSquared
                    || vessel.angularVelocity.sqrMagnitude > MovingAngularSpeedSquared))
                {
                    ResetGate();
                }
            }
            else if (IsStopped())
            {
                stoppedSeconds = Mathf.Min(RequiredStopSeconds, stoppedSeconds + Time.fixedDeltaTime);
                if (stoppedSeconds >= RequiredStopSeconds) { anchorPermitted = true; }
            }
            else
            {
                stoppedSeconds = 0f;
            }

            // Covers anchors left from loading or a frame without rendering.
            RemovePrematureAnchor();
        }

        public void LateUpdate()
        {
            // Run after Vessel.LateUpdate so Unity destroys an early joint before
            // the next physics step. End-of-frame removal remains a fallback.
            RemovePrematureAnchor();
        }

        private IEnumerator RemovePrematureAnchors()
        {
            WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();
            while (true)
            {
                yield return endOfFrame;
                // Vessel.LateUpdate creates stock anchors; this runs afterward,
                // before the next physics step.
                RemovePrematureAnchor();
            }
        }

        private void RemovePrematureAnchor()
        {
            if (!hasKSPWheel || vessel == null || !vessel.loaded || vessel.packed) { return; }
            // Steering can begin after the last physics tick. Release the joint
            // before it can hold a zero-radius turn at the next physics step.
            if (HasDriveInput() || vessel.angularVelocity.sqrMagnitude > MovingAngularSpeedSquared)
            {
                ResetGate();
            }
            if (vessel.IsAnchored && !anchorPermitted)
            {
                vessel.ResetRBAnchor();
            }
        }

        private bool IsStopped()
        {
            return vessel.rb_velocity.sqrMagnitude <= StoppedSpeedSquared
                && vessel.angularVelocity.sqrMagnitude <= StoppedAngularSpeedSquared
                && !HasDriveInput();
        }

        private bool HasDriveInput()
        {
            if (vessel == null || vessel.ctrlState == null) { return false; }
            FlightCtrlState controls = vessel.ctrlState;
            return (hasDrivenMotor && Mathf.Abs(controls.wheelThrottle + controls.wheelThrottleTrim) > DriveInputThreshold)
                || (hasTankSteeringMotor && Mathf.Abs(controls.wheelSteer + controls.wheelSteerTrim) > DriveInputThreshold);
        }

        private void ResetGate()
        {
            stoppedSeconds = 0f;
            anchorPermitted = false;
        }

        private void RefreshWheelState()
        {
            hasKSPWheel = false;
            hasDrivenMotor = false;
            hasTankSteeringMotor = false;
            if (vessel == null || vessel.Parts == null) { return; }
            foreach (Part part in vessel.Parts)
            {
                if (part == null) { continue; }
                foreach (PartModule module in part.Modules)
                {
                    if (module is KSPWheelBase) { hasKSPWheel = true; }
                    KSPWheelMotor motor = module as KSPWheelMotor;
                    if (motor != null && !motor.motorLocked && motor.motorOutput > 0f)
                    {
                        hasDrivenMotor = true;
                        if (motor.tankSteering && !motor.steeringLocked) { hasTankSteeringMotor = true; }
                    }
                }
            }
        }
    }
}
