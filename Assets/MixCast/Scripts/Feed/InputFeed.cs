/*======= (c) Blueprint Reality Inc., 2017. All rights reserved =======*/
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BlueprintReality.MixCast
{
    public class InputFeed : CameraComponent
    {
        public class FramePlayerData
        {
            public float playerDepth;

            public Vector3 playerHeadPos;
            public Vector3 playerBasePos;
            public Vector3 playerLeftHandPos;
            public Vector3 playerRightHandPos;
        }

        private const string KEYWORD_CROP_PLAYER = "CROP_PLAYER";
        private const string KEYWORD_WRITE_DEPTH = "DEPTH_ON";

        public const string COLOR_SPACE_EXP_PROP = "_ColorExponent";

        public static List<Material> activeMaterials = new List<Material>();

        public MixCastCamera cam;

        public Vector3 playerHeadsetOffset = new Vector3(0, 0, -0.05f);     //HMD tracked point about 5cm in front of center of skull

        public Material blitMaterial;

        public virtual Texture Texture
        {
            get
            {
                return null;
            }
        }

        public event Action OnRenderStarted;
        public event Action OnRenderEnded;

        private string texSourceName = "";
        private Material setMaterial;
        private FrameDelayQueue<FramePlayerData> frames;

        protected override void OnEnable()
        {
            if (cam == null)
                cam = GetComponentInParent<MixCastCamera>();

            frames = new FrameDelayQueue<FramePlayerData>();

            base.OnEnable();
            Invoke("HandleDataChanged", 0.01f);
        }

        protected override void OnDisable()
        {
            frames = null;

            ClearTexture();
            base.OnDisable();
        }

        protected override void HandleDataChanged()
        {
            base.HandleDataChanged();

            if (context.Data.deviceName == texSourceName)
                return;

            ClearTexture();

            if (context.Data != null && !string.IsNullOrEmpty(context.Data.deviceName))
                SetTexture();
        }

        protected virtual void LateUpdate()
        {
            if (context.Data != null)
            {
                if ((context.Data.deviceName != texSourceName))
                {
                    ClearTexture();
                    SetTexture();
                }
                else if ((blitMaterial != setMaterial) && cam.ActiveFeeds.Contains(this))
                {
                    UnregisterFromCamera();
                    RegisterWithCamera();
                }
            }
        }

        public virtual void StartRender()
        {
            if (context.Data == null || frames == null)
                return;
            
            frames.delayDuration = context.Data.outputMode == MixCastData.OutputMode.Buffered ? context.Data.bufferTime : 0;
            frames.Update();

            if (blitMaterial != null)
            {
                blitMaterial.mainTexture = Texture;

                //Tell the material if linear color space needs to be corrected
                blitMaterial.SetFloat(COLOR_SPACE_EXP_PROP, QualitySettings.activeColorSpace == ColorSpace.Linear ? 2.2f : 1);

                if (Texture != null && cam.Output != null)
                {
                    //set transform to correct for different aspect ratios between screen and camera texture
                    float ratioRatio = ((float)Texture.width / Texture.height) / ((float)cam.Output.width / cam.Output.height);
                    blitMaterial.SetVector("_TextureTransform", new Vector4(1f / ratioRatio, 1, 0.5f * (1f - 1f / ratioRatio), 0));
                }

                //update the player's depth for the material
                FrameDelayQueue<FramePlayerData>.Frame<FramePlayerData> nextFrame = frames.GetNewFrame();
                if (nextFrame.data == null)
                    nextFrame.data = new FramePlayerData();

                if (cam.gameCamera != null)
                    nextFrame.data.playerDepth = CalculatePlayerDepth();

                Transform roomTransform = Camera.main.transform.parent;
                nextFrame.data.playerHeadPos = Camera.main.transform.position;
                nextFrame.data.playerBasePos = roomTransform.TransformPoint(new Vector3(Camera.main.transform.localPosition.x, 0, Camera.main.transform.localPosition.z));
                nextFrame.data.playerLeftHandPos = roomTransform.TransformPoint(GetTrackingPosition(UnityEngine.XR.XRNode.LeftHand));
                nextFrame.data.playerRightHandPos = roomTransform.TransformPoint(GetTrackingPosition(UnityEngine.XR.XRNode.RightHand));

                FramePlayerData oldFrameData = frames.OldestFrameData;

                blitMaterial.SetFloat("_PlayerDepth", oldFrameData.playerDepth);
                blitMaterial.SetVector("_PlayerHeadPos", oldFrameData.playerHeadPos);
                blitMaterial.SetVector("_PlayerLeftHandPos", oldFrameData.playerLeftHandPos);
                blitMaterial.SetVector("_PlayerRightHandPos", oldFrameData.playerRightHandPos);
                blitMaterial.SetVector("_PlayerBasePos", oldFrameData.playerBasePos);

                blitMaterial.SetFloat("_PlayerScale", Camera.main.transform.parent.TransformVector(Vector3.forward).magnitude);

                blitMaterial.SetFloat("_PlayerHeadCropRadius", context.Data.croppingData.headRadius);
                blitMaterial.SetFloat("_PlayerHandCropRadius", context.Data.croppingData.handRadius);
                blitMaterial.SetFloat("_PlayerFootCropRadius", context.Data.croppingData.baseRadius);

                
                if (context.Data.croppingData.active)
                    blitMaterial.EnableKeyword(KEYWORD_CROP_PLAYER);
                else
                    blitMaterial.DisableKeyword(KEYWORD_CROP_PLAYER);


                if (cam is ImmediateMixCastCamera)
                {
                    blitMaterial.EnableKeyword(KEYWORD_WRITE_DEPTH);
                }
                else
                {
                    blitMaterial.DisableKeyword(KEYWORD_WRITE_DEPTH);
                }
            }

            if (OnRenderStarted != null)
                OnRenderStarted();
        }
        public virtual void StopRender()
        {
            if (blitMaterial != null)
            {
                blitMaterial.DisableKeyword(KEYWORD_CROP_PLAYER);
                blitMaterial.DisableKeyword(KEYWORD_WRITE_DEPTH);
                blitMaterial.DisableKeyword("CORRECT_TEX_COLORSPACE");
            }
            if (OnRenderEnded != null)
                OnRenderEnded();
        }

        protected void RegisterWithCamera()
        {
            cam.RegisterFeed(this);
            activeMaterials.Add(blitMaterial);
            setMaterial = blitMaterial;
        }
        protected void UnregisterFromCamera()
        {
            cam.UnregisterFeed(this);
            activeMaterials.Remove(setMaterial);
        }

        protected virtual void SetTexture()
        {
            texSourceName = context.Data.deviceName;
            RegisterWithCamera();
        }

        protected virtual void ClearTexture()
        {
            UnregisterFromCamera();
            texSourceName = "";
        }

        public float CalculatePlayerDepth()
        {
            Vector3 playerPosition = Camera.main.transform.TransformPoint(playerHeadsetOffset);
            float distance = Vector3.Dot(cam.gameCamera.transform.forward, playerPosition - cam.gameCamera.transform.position);

            // Inverse Unclamped Lerp
            var f = cam.gameCamera.farClipPlane - cam.gameCamera.nearClipPlane;
            var d = distance - cam.gameCamera.nearClipPlane;

            return d / f;
        }

        Vector3 GetTrackingPosition(UnityEngine.XR.XRNode node)
        {
            if (node == UnityEngine.XR.XRNode.Head)
                return Camera.main.transform.localPosition;

            if (UnityEngine.XR.XRDevice.isPresent)
                return UnityEngine.XR.InputTracking.GetLocalPosition(node);

#if UNITY_EDITOR && MIXCAST_STEAMVR
            //SteamVR_ControllerManager controllerManager = FindObjectOfType<SteamVR_ControllerManager>();
            //if (controllerManager != null)
            //{
            //    switch(node)
            //    {
            //        case UnityEngine.VR.VRNode.LeftHand:
            //            return controllerManager.left.transform.localPosition;
            //        case UnityEngine.VR.VRNode.RightHand:
            //            return controllerManager.right.transform.localPosition;
            //        default:
            //            return Camera.main.transform.localPosition;
            //    }
            //}
#endif

            return Vector3.zero;
        }
    }
}