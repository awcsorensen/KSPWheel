using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelVesselDebug : VesselModule
    {
        private int id = 0;
        private Rect windowRect = new Rect(20, 20, 1000, 650);
        private Vector2 scrollPos;
        private bool guiInitialized = false;
        private bool windowSized = false;
        private bool fitScreenRequested = false;
        private bool resizing = false;
        private Vector2 resizeDelta = Vector2.zero;
        private bool debugRendering = false;
        private int probeFixedUpdates = 0;
        private float probeLastFixedRealtime = 0f;

        private List<WheelDebugData> wheels = new List<WheelDebugData>();

        public void FixedUpdate()
        {
            probeFixedUpdates++;
            probeLastFixedRealtime = Time.realtimeSinceStartup;
        }

        public void drawGUI()
        {
            if (!guiInitialized)
            {
                wheels.Clear();
                guiInitialized = true;
                List<KSPWheelBase> baseModules = new List<KSPWheelBase>();
                foreach (Part p in vessel.Parts)
                {
                    baseModules.AddUniqueRange(p.GetComponents<KSPWheelBase>());
                }
                foreach (KSPWheelBase bm in baseModules)
                {
                    foreach (KSPWheelBase.KSPWheelData bd in bm.wheelData)
                    {
                        wheels.Add(new WheelDebugData(bm, bd));
                    }
                }
            }
            if (vessel.isActiveVessel)
            {
                drawDebugGUI();
            }
        }

        private void drawDebugGUI()
        {
            if (!windowSized)
            {
                fitWindowToScreen();
                windowSized = true;
            }
            windowRect = GUI.Window(id, windowRect, updateWindow, "Wheel Debug Display");
            if (fitScreenRequested)
            {
                fitWindowToScreen();
                fitScreenRequested = false;
            }
            else if (resizeDelta != Vector2.zero)
            {
                windowRect.width = Mathf.Clamp(windowRect.width + resizeDelta.x, 480f, Mathf.Max(480f, Screen.width - windowRect.x));
                windowRect.height = Mathf.Clamp(windowRect.height + resizeDelta.y, 300f, Mathf.Max(300f, Screen.height - windowRect.y));
                resizeDelta = Vector2.zero;
            }
        }

        private void fitWindowToScreen()
        {
            windowRect = new Rect(20f, 20f, Mathf.Max(480f, Screen.width - 40f), Mathf.Max(300f, Screen.height - 40f));
        }

        private void updateWindow(int id)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(debugRendering ? "Hide markers" : "Show markers", GUILayout.Width(110f)))
            {
                debugRendering = !debugRendering;
                enableDebugRendering(debugRendering);
            }
            if (GUILayout.Button("Dump data", GUILayout.Width(90f)))
            {
                dumpDebugData();
            }
            if (GUILayout.Button("Fit screen", GUILayout.Width(90f)))
            {
                fitScreenRequested = true;
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(70f)))
            {
                KSPWheelLauncher.instance.debugGuiDisable();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Vessel packed: " + vessel.packed
                + " | Anchored: " + vessel.IsAnchored
                + " | Landed: " + vessel.LandedOrSplashed
                + " | Time scale: " + Time.timeScale.ToString("F2")
                + " | Fixed ticks: " + probeFixedUpdates
                + " | Seconds since fixed tick: " + (Time.realtimeSinceStartup - probeLastFixedRealtime).ToString("F2"));
            GUILayout.Label(wheels.Count + " colliders | Scroll the table in both directions. Drag the lower-right corner to resize.");

            //per-wheel instance data view
            bool originalLabelWrap = GUI.skin.label.wordWrap;
            GUI.skin.label.wordWrap = false;
            scrollPos = GUILayout.BeginScrollView(scrollPos, true, true, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            float w1 = 32f;
            float w2 = 58f;
            float w3 = 190f;
            float nameWidth = 115f;
            GUILayout.BeginVertical(GUILayout.Width(1700f));

            //data column header row
            GUILayout.BeginHorizontal();
            GUILayout.Label("idx", GUILayout.Width(w1));//index
            GUILayout.Label("name", GUILayout.Width(nameWidth));//user-set base module label/name
            GUILayout.Label("grp", GUILayout.Width(w1));//group
            GUILayout.Label("gnd", GUILayout.Width(w2));
            GUILayout.Label("sleep", GUILayout.Width(w2));
            GUILayout.Label("kin", GUILayout.Width(w2));
            GUILayout.Label("rbVel", GUILayout.Width(w2));
            GUILayout.Label("rad", GUILayout.Width(w2));//radius
            GUILayout.Label("mass", GUILayout.Width(w2));//mass
            GUILayout.Label("len", GUILayout.Width(w2));//length of travel
            GUILayout.Label("spr", GUILayout.Width(w2));//spring rate
            GUILayout.Label("dmp", GUILayout.Width(w2));//damper rate (n * m/s)
            GUILayout.Label("ang", GUILayout.Width(w2));//angular velocity
            GUILayout.Label("vel", GUILayout.Width(w2));//linear velocity
            GUILayout.Label("trq", GUILayout.Width(w2));//motor torque
            GUILayout.Label("brk", GUILayout.Width(w2));//brake torque
            GUILayout.Label("comp", GUILayout.Width(w2));//compression
            GUILayout.Label("comp%", GUILayout.Width(w2));//compression (percent of max)
            GUILayout.Label("timeB", GUILayout.Width(w2));//time boost factor
            GUILayout.Label("fY", GUILayout.Width(w2));//springForce
            GUILayout.Label("fZ", GUILayout.Width(w2));//longForce
            GUILayout.Label("fX", GUILayout.Width(w2));//latForce
            GUILayout.Label("sZ", GUILayout.Width(w2));//longSlip
            GUILayout.Label("sX", GUILayout.Width(w2));//latSlip
            GUILayout.Label("hit", GUILayout.Width(w3));//collider hit
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical();
            int len = wheels.Count;
            KSPWheelCollider wheel;
            for (int i = 0; i < len; i++)
            {
                GUILayout.BeginHorizontal();
                wheel = wheels[i].wheelData.wheel;
                Rigidbody body = wheel == null ? null : wheel.rigidbody;
                GUILayout.Label(i.ToString(), GUILayout.Width(w1));//raw wheel index
                GUILayout.Label(wheels[i].baseModule.label, GUILayout.Width(nameWidth));//wheel name/label
                GUILayout.Label(wheels[i].baseModule.wheelGroup.ToString(), GUILayout.Width(w1));//wheel group
                GUILayout.Label(wheel.isGrounded ? "yes" : "no", GUILayout.Width(w2));
                GUILayout.Label(body == null ? "n/a" : (body.IsSleeping() ? "yes" : "no"), GUILayout.Width(w2));
                GUILayout.Label(body == null ? "n/a" : (body.isKinematic ? "yes" : "no"), GUILayout.Width(w2));
                GUILayout.Label(body == null ? "n/a" : body.velocity.magnitude.ToString("0.000"), GUILayout.Width(w2));
                GUILayout.Label(wheel.radius.ToString("0.##"), GUILayout.Width(w2));//radius
                GUILayout.Label(wheel.mass.ToString("0.##"), GUILayout.Width(w2));//mass
                GUILayout.Label(wheel.length.ToString("0.##"), GUILayout.Width(w2));//length of travel
                GUILayout.Label(wheel.spring.ToString("0.##"), GUILayout.Width(w2));//spring rate
                GUILayout.Label(wheel.damper.ToString("0.##"), GUILayout.Width(w2));//damper rate (n * m/s)
                GUILayout.Label(wheel.angularVelocity.ToString("0.##"), GUILayout.Width(w2));//angular velocity
                GUILayout.Label(wheel.linearVelocity.ToString("0.##"), GUILayout.Width(w2));//linear velocity
                GUILayout.Label(wheel.motorTorque.ToString("0.##"), GUILayout.Width(w2));//motor torque
                GUILayout.Label(wheel.brakeTorque.ToString("0.##"), GUILayout.Width(w2));//brake torque
                GUILayout.Label(wheel.compressionDistance.ToString("0.##"), GUILayout.Width(w2));//compression
                GUILayout.Label((wheel.compressionDistance / wheel.length).ToString("0.##"), GUILayout.Width(w2));//compression (percent of max)
                GUILayout.Label(wheels[i].wheelData.timeBoostFactor.ToString("0.##"), GUILayout.Width(w2));//time boost factor
                GUILayout.Label(wheel.springForce.ToString("0.000"), GUILayout.Width(w2));//springForce
                GUILayout.Label(wheel.longitudinalForce.ToString("0.000"), GUILayout.Width(w2));//longForce
                GUILayout.Label(wheel.lateralForce.ToString("0.000"), GUILayout.Width(w2));//latForce
                GUILayout.Label(wheel.longitudinalSlip.ToString("0.000"), GUILayout.Width(w2));//longSlip
                GUILayout.Label(wheel.lateralSlip.ToString("0.000"), GUILayout.Width(w2));//latSlip
                //using a button as for some retarted reason auto-word-wrap is enabled for labels...
                GUILayout.Button(wheel.contactColliderHit == null ? "none" : wheel.contactColliderHit.ToString() + ":" + wheel.contactColliderHit.gameObject.layer, GUILayout.Width(w3));//collider hit
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUI.skin.label.wordWrap = originalLabelWrap;
            GUILayout.Space(20f);
            GUILayout.EndVertical();

            Rect resizeHandle = new Rect(windowRect.width - 22f, windowRect.height - 22f, 20f, 20f);
            GUI.Box(resizeHandle, "+");
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && resizeHandle.Contains(currentEvent.mousePosition))
            {
                resizing = true;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseDrag && resizing)
            {
                resizeDelta += currentEvent.delta;
                currentEvent.Use();
            }
            else if (currentEvent.rawType == EventType.MouseUp)
            {
                resizing = false;
            }
            GUI.DragWindow(new Rect(0f, 0f, windowRect.width - 22f, 20f));
        }

        public void LateUpdate()
        {
            if (debugRendering)
            {
                int len = wheels.Count;
                for (int i = 0; i < len; i++)
                {
                    wheels[i].updateDebugRenderers();
                }
            }
        }

        private void enableDebugRendering(bool enable)
        {
            int len = wheels.Count;
            if (enable)
            {
                for (int i = 0; i < len; i++)
                {
                    wheels[i].enableDebugRenderers();
                }
            }
            else
            {
                for (int i = 0; i < len; i++)
                {
                    wheels[i].disableDebugRenderers();
                }
            }
        }

        public void dumpDebugData()
        {
            MonoBehaviour.print("---------------------------------------------------------------------------------");
            MonoBehaviour.print("KSPWheel Vessel Wheel Debug Data.");
            int len = wheels.Count;
            for (int i = 0; i < len; i++)
            {
                MonoBehaviour.print("--------------------------------------");
                MonoBehaviour.print(wheels[i].baseModule.getDebugInfo());
                MonoBehaviour.print("--------------------------------------");
            }
            //TODO
            MonoBehaviour.print("---------------------------------------------------------------------------------");
        }
    }

    public class WheelDebugData
    {
        public KSPWheelBase baseModule;
        public KSPWheelBase.KSPWheelData wheelData;

        private GameObject debugLineRenderBase;//empty parent object, holder for the rest, manually positioned into wheel position and orientation
        private GameObject debugLineRendererFwd;
        private GameObject debugLineRendererSide;
        private GameObject debugLineRendererUp;
        private GameObject debugLineRendererWheel;
        private GameObject debugLineRendererContact;

        private LineRenderer fwd;
        private LineRenderer side;
        private LineRenderer up;
        private LineRenderer wheel;
        private LineRenderer contact;

        public WheelDebugData(KSPWheelBase baseModule, KSPWheelBase.KSPWheelData wheelData)
        {
            this.baseModule = baseModule;
            this.wheelData = wheelData;
        }

        /// <summary>
        /// Set up the line renderers; fwd/side/up axis lines and wheel circle
        /// </summary>
        internal void setupDebugRenderers()
        {
            this.debugLineRenderBase = new GameObject("DebugLineRender");
            debugLineRenderBase.transform.position = wheelData.wheel.transform.position;
            debugLineRenderBase.transform.rotation = wheelData.wheel.transform.rotation;

            debugLineRendererFwd = new GameObject("DebugLineRenderFwd");
            debugLineRendererFwd.transform.parent = debugLineRenderBase.transform;
            debugLineRendererFwd.transform.localPosition = Vector3.zero;
            debugLineRendererFwd.transform.localRotation = Quaternion.identity;
            fwd = debugLineRendererFwd.AddComponent<LineRenderer>();

            debugLineRendererSide = new GameObject("DebugLineRenderSide");
            debugLineRendererSide.transform.parent = debugLineRenderBase.transform;
            debugLineRendererSide.transform.localPosition = Vector3.zero;
            debugLineRendererSide.transform.localRotation = Quaternion.identity;
            side = debugLineRendererSide.AddComponent<LineRenderer>();

            debugLineRendererUp = new GameObject("DebugLineRendererUp");
            debugLineRendererUp.transform.parent = debugLineRenderBase.transform;
            debugLineRendererUp.transform.localPosition = Vector3.zero;
            debugLineRendererUp.transform.localRotation = Quaternion.identity;
            up = debugLineRendererUp.AddComponent<LineRenderer>();

            debugLineRendererWheel = new GameObject("DebugLineRendererWheel");
            debugLineRendererWheel.transform.parent = debugLineRenderBase.transform;
            debugLineRendererWheel.transform.localPosition = Vector3.zero;
            debugLineRendererWheel.transform.localRotation = Quaternion.identity;
            wheel = debugLineRendererWheel.AddComponent<LineRenderer>();

            debugLineRendererContact = new GameObject("DebugLineRendererContact");
            debugLineRendererContact.transform.parent = debugLineRenderBase.transform;
            debugLineRendererContact.transform.localPosition = Vector3.zero;
            debugLineRendererContact.transform.localRotation = Quaternion.identity;
            contact = debugLineRendererContact.AddComponent<LineRenderer>();

            Material lineRendererMaterial = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));

            fwd.useWorldSpace = false;
            fwd.SetPositions(new Vector3[] { Vector3.zero, Vector3.forward*5f });
            fwd.material = lineRendererMaterial;
            fwd.SetColors(Color.blue, Color.blue);
            fwd.SetWidth(0.1f, 0.1f);

            side.useWorldSpace = false;
            side.SetPositions(new Vector3[] { Vector3.zero, Vector3.right*5f });
            side.material = lineRendererMaterial;
            side.SetColors(Color.red, Color.red);
            side.SetWidth(0.1f, 0.1f);

            up.useWorldSpace = false;
            up.SetPositions(new Vector3[] { Vector3.zero, Vector3.up*5f });
            up.material = lineRendererMaterial;
            up.SetColors(Color.green, Color.green);
            up.SetWidth(0.1f, 0.1f);

            int segments = 24;
            Vector3[] points = new Vector3[segments + 1];
            float radsPerSegment = (360f / (float)segments) * Mathf.Deg2Rad;
            float y, z;
            wheel.useWorldSpace = false;
            wheel.material = lineRendererMaterial;
            wheel.SetColors(Color.magenta, Color.magenta);
            wheel.SetWidth(0.1f, 0.1f);
            wheel.SetVertexCount(25);
            for (int i = 0; i <= segments; i++)//uses <= in order to close the loop
            {
                y = wheelData.wheel.radius * Mathf.Sin(i * radsPerSegment);
                z = wheelData.wheel.radius * Mathf.Cos(i * radsPerSegment);
                points[i] = new Vector3(0, y, z);
            }
            wheel.SetPositions(points);

            // A small yellow ring marks the actual contact point on the hit surface.
            const float contactMarkerRadius = 0.12f;
            const float contactMarkerWidth = 0.05f;
            int contactSegments = 12;
            Vector3[] contactPoints = new Vector3[contactSegments + 1];
            for (int i = 0; i <= contactSegments; i++)
            {
                float angle = i * 2f * Mathf.PI / contactSegments;
                contactPoints[i] = new Vector3(Mathf.Cos(angle) * contactMarkerRadius, 0f, Mathf.Sin(angle) * contactMarkerRadius);
            }
            contact.useWorldSpace = false;
            contact.material = lineRendererMaterial;
            contact.startColor = Color.yellow;
            contact.endColor = Color.yellow;
            contact.startWidth = contactMarkerWidth;
            contact.endWidth = contactMarkerWidth;
            contact.positionCount = contactPoints.Length;
            contact.SetPositions(contactPoints);
            debugLineRendererContact.SetActive(false);
        }

        /// <summary>
        /// Update the position and orientation of the line renders to mimic the current wheel position (with suspension/hit position data) and orientation (with steering)
        /// </summary>
        internal void updateDebugRenderers()
        {
            KSPWheelCollider physicalWheel = wheelData.wheel;
            bool grounded = physicalWheel.isGrounded;
            Vector3 point = physicalWheel.contactPoint;
            Vector3 normal = physicalWheel.contactNormal;
            float displayCompression = physicalWheel.compressionDistance;
            if (physicalWheel.sweepType == KSPWheelSweepType.RAY)
            {
                // Refresh only the display point from the current render-frame pose.
                const float minGroundDot = 0.5f;
                Vector3 wheelUp = physicalWheel.transform.up;
                RaycastHit hit;
                grounded = Physics.Raycast(physicalWheel.transform.position, -wheelUp, out hit,
                    physicalWheel.length + physicalWheel.radius / minGroundDot,
                    physicalWheel.raycastMask, QueryTriggerInteraction.Ignore);
                if (grounded)
                {
                    float alignment = Vector3.Dot(hit.normal, wheelUp);
                    grounded = alignment >= minGroundDot;
                    if (grounded)
                    {
                        float renderCompression = physicalWheel.length + physicalWheel.radius / alignment - hit.distance;
                        grounded = renderCompression >= 0f;
                        if (grounded)
                        {
                            displayCompression = renderCompression;
                            point = hit.point + wheelUp * (physicalWheel.radius / alignment) - hit.normal * physicalWheel.radius;
                            normal = hit.normal;
                        }
                    }
                }
            }

            debugLineRenderBase.transform.position = physicalWheel.transform.position - physicalWheel.transform.up * (physicalWheel.length - displayCompression);
            debugLineRenderBase.transform.rotation = physicalWheel.transform.rotation;
            if (!physicalWheel.steeringAngleAppliedByTransform)
            {
                debugLineRenderBase.transform.Rotate(0, physicalWheel.steeringAngle, 0, Space.Self);
            }

            debugLineRendererContact.SetActive(grounded);
            if (grounded)
            {
                debugLineRendererContact.transform.position = point;
                debugLineRendererContact.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
            }
        }

        internal void enableDebugRenderers()
        {
            if (debugLineRenderBase == null)
            {
                setupDebugRenderers();
            }
        }

        internal void disableDebugRenderers()
        {
            if (debugLineRenderBase != null)
            {
                GameObject.Destroy(debugLineRenderBase);
            }
            debugLineRenderBase = null;
        }

    }
    
}
