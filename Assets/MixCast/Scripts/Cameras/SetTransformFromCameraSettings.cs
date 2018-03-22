/*======= (c) Blueprint Reality Inc., 2017. All rights reserved =======*/
using System.Collections.Generic;
using UnityEngine;

namespace BlueprintReality.MixCast
{
    public class SetTransformFromCameraSettings : CameraComponent
    {
        protected Vector3 smoothPositionVel, smoothRotationVel;

        protected virtual void Awake()
        {
            if (MixCastCameras.Instance != null)
            {
                MixCastCameras.Instance.OnBeforeRender += LateUpdate;
            }

#if MIXCAST_STEAMVR
            newPosesAppliedAction = SteamVR_Events.NewPosesAppliedAction(OnNewPosesApplied);

            isTrackedDeviceReady = true;
#endif
        }

        protected virtual void OnDestroy()
        {
            if (MixCastCameras.Instance != null)
            {
                MixCastCameras.Instance.OnBeforeRender -= LateUpdate;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            HandleDataChanged();
        }

        protected override void HandleDataChanged()
        {
            base.HandleDataChanged();

            if (context.Data != null)
            {
                Vector3 pos;
                Quaternion rot;
                GetCameraTransform(out pos, out rot);
                SetTransformFromTarget(pos, rot, true);
            }
        }

        protected virtual void LateUpdate()
        {
            if (context.Data == null)
                return;

            Vector3 pos;
            Quaternion rot;
            GetCameraTransform(out pos, out rot);
            SetTransformFromTarget(pos, rot);
        }



        public void GetCameraTransform(out Vector3 position, out Quaternion rotation)
        {
            if (context.Data.wasTracked && GetTrackingTransform(out position, out rotation))
            {
                position = position + rotation * context.Data.trackedPosition;
                rotation = rotation * context.Data.trackedRotation;
            }
            else
            {
                position = context.Data.worldPosition;
                rotation = context.Data.worldRotation;

#if MIXCAST_STEAMVR
                //clear cache if not tracked anymore
                if( !context.Data.wasTracked )
                    lastTrackingDeviceId = null;
#endif
            }
        }

        void SetTransformFromTarget(Vector3 newPos, Quaternion newRot, bool instant = false)
        {
            if (context.Data.positionSmoothTime > 0 && !instant)
                newPos = Vector3.SmoothDamp(transform.localPosition, newPos, ref smoothPositionVel, context.Data.positionSmoothTime);
            transform.localPosition = newPos;

            if (context.Data.rotationSmoothTime > 0 && !instant)
            {
                Vector3 curEuler = transform.localEulerAngles;
                Vector3 newEuler = newRot.eulerAngles;
                newRot = Quaternion.Euler(new Vector3(
                    Mathf.SmoothDampAngle(curEuler.x, newEuler.x, ref smoothRotationVel.x, context.Data.rotationSmoothTime),
                    Mathf.SmoothDampAngle(curEuler.y, newEuler.y, ref smoothRotationVel.y, context.Data.rotationSmoothTime),
                    Mathf.SmoothDampAngle(curEuler.z, newEuler.z, ref smoothRotationVel.z, context.Data.rotationSmoothTime)
                    ));
            }
            transform.localRotation = newRot;
        }

        public bool GetTrackingTransform(out Vector3 position, out Quaternion rotation)
        {
#if MIXCAST_STEAMVR
            if (VRInfo.IsDeviceOpenVR())
            {
                return GetTrackedTransform_Steam(out position, out rotation);
            }
#endif
#if MIXCAST_OCULUS
            if (VRInfo.IsDeviceOculus())
            {
                return GetTrackedTransform_Oculus(out position, out rotation);
            }
#endif
            position = Vector3.zero;
            rotation = Quaternion.identity;

            return false;
        }

        protected virtual void OnTrackedDeviceSwitchAndReady()
        {

        }

#if MIXCAST_STEAMVR

        private SteamVR_Events.Action newPosesAppliedAction;
        private bool isTrackedDeviceReady;
        private string lastTrackingDeviceId = null;
        private SteamVR_TrackedObject trackedObject;

        private void OnNewPosesApplied()
        {
            if (!trackedObject.isValid)
                return;

            newPosesAppliedAction.enabled = false;
            isTrackedDeviceReady = true;

            OnTrackedDeviceSwitchAndReady();
        }

        bool GetTrackedTransform_Steam(out Vector3 pos, out Quaternion rot)
        {
            pos = Vector3.zero;
            rot = Quaternion.identity;

            if (string.IsNullOrEmpty(context.Data.trackedByDeviceId))
            {
                lastTrackingDeviceId = null;
                if (!string.IsNullOrEmpty(context.Data.trackedByDevice))
                {
                    SteamVR_TrackedObject.EIndex deviceIndex = (SteamVR_TrackedObject.EIndex)System.Enum.Parse(typeof(SteamVR_TrackedObject.EIndex), context.Data.trackedByDevice);
                    context.Data.trackedByDeviceId = VRInfo.GetDeviceSerial((uint)deviceIndex);
                }
                else
                    return false;
            }

            try
            {
                if (lastTrackingDeviceId != context.Data.trackedByDeviceId)
                {
                    if (trackedObject == null)
                    {
                        var proxy = new GameObject("Tracking Proxy");
                        proxy.transform.parent = transform;
                        trackedObject = proxy.AddComponent<SteamVR_TrackedObject>();
                    }

                    lastTrackingDeviceId = null;

                    for (int i = 0; i < Valve.VR.OpenVR.k_unMaxTrackedDeviceCount; i++)
                    {
                        string deviceId = VRInfo.GetDeviceSerial((uint)i);
                        if (deviceId == context.Data.trackedByDeviceId)
                        {
                            trackedObject.SetDeviceIndex(i);
                            trackedObject.enabled = false;  //Reset component so trackedObject.isValid will be helpful again
                            trackedObject.enabled = true;
                            lastTrackingDeviceId = context.Data.trackedByDeviceId;
                            break;
                        }
                    }
                    newPosesAppliedAction.enabled = true;
                    isTrackedDeviceReady = false;
                    
                    return false;
                }

                if (!isTrackedDeviceReady
                    || string.IsNullOrEmpty(lastTrackingDeviceId)
                    || trackedObject == null 
                    || !trackedObject.isValid)
                {
                    return false;
                }

                pos = trackedObject.transform.localPosition;
                rot = trackedObject.transform.localRotation;
                
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
#endif

#if MIXCAST_OCULUS

        private string trackedByOculusDevice;

        bool GetTrackedTransform_Oculus(out Vector3 pos, out Quaternion rot)
        {
            pos = Vector3.zero;
            rot = Quaternion.identity;

            if (string.IsNullOrEmpty(context.Data.trackedByDevice))
                return false;

            try
            {
                OVRInput.Controller controller = OVRInput.Controller.None;
                if (context.Data.trackedByDevice == "Device1")
                    controller = OVRInput.Controller.LTouch;
                else if (context.Data.trackedByDevice == "Device2")
                    controller = OVRInput.Controller.RTouch;

                if (!OVRInput.IsControllerConnected(controller) || !OVRInput.GetControllerPositionTracked(controller) || !OVRInput.GetControllerOrientationTracked(controller))
                    return false;

                pos = OVRInput.GetLocalControllerPosition(controller);
                rot = OVRInput.GetLocalControllerRotation(controller);

                if (trackedByOculusDevice != context.Data.trackedByDevice)
                {
                    trackedByOculusDevice = context.Data.trackedByDevice;

                    OnTrackedDeviceSwitchAndReady();
                }

                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
#endif
    }
}