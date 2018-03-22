/*======= (c) Blueprint Reality Inc., 2017. All rights reserved =======*/
using System.Collections.Generic;
using UnityEngine;

namespace BlueprintReality.MixCast
{
    public class MixCastCamera : CameraComponent
    {
        public static List<MixCastCamera> ActiveCameras { get; protected set; }
        public static event System.Action<MixCastCamera> GameRenderStarted;
        public static event System.Action<MixCastCamera> GameRenderEnded;
        public static event System.Action<MixCastCamera> FrameCompleted;

        static MixCastCamera()
        {
            ActiveCameras = new List<MixCastCamera>();
        }

        public Transform displayTransform;
        public Camera gameCamera;

        public Texture Output { get; protected set; }

        public List<InputFeed> ActiveFeeds { get; protected set; }

        public bool IsInUse
        {
            get
            {
                return MixCast.DisplayingCamera == context.Data || MixCast.RecordingCameras.Contains(context.Data) || MixCast.StreamingCameras.Contains(context.Data);
            }
        }

        protected virtual void Awake()
        {
            if (ActiveFeeds == null)
                ActiveFeeds = new List<InputFeed>();
        }

        protected override void OnEnable()
        {
            if (gameCamera != null)
            {
                gameCamera.stereoTargetEye = StereoTargetEyeMask.None;
                gameCamera.enabled = false;
            }

            base.OnEnable();

            if( context.Data != null )
                BuildOutput();

            HandleDataChanged();

            ActiveCameras.Add(this);
        }
        protected override void OnDisable()
        {
            if (ActiveFeeds != null)
            {
                for (int i = ActiveFeeds.Count - 1; i >= 0; i--)
                    UnregisterFeed(ActiveFeeds[i]);
            }

            ReleaseOutput();

            ActiveCameras.Remove(this);

            base.OnDisable();

            if (gameCamera != null)
                gameCamera.enabled = true;
        }

        protected override void HandleDataChanged()
        {
            base.HandleDataChanged();

            if (context.Data == null)
                return;

            if (context.Data.deviceFoV > 0 && gameCamera != null)
                gameCamera.fieldOfView = context.Data.deviceFoV;

            if (CalculateOutputWidth() != Output.width || CalculateOutputHeight() != Output.height)
            {
                ReleaseOutput();
                BuildOutput();
            }
        }

        protected virtual void LateUpdate()
        {
            if (context.Data == null)
                return;

            if (context.Data.deviceFoV > 0 && gameCamera != null)
                gameCamera.fieldOfView = context.Data.deviceFoV;

            if (CalculateOutputWidth() != Output.width || CalculateOutputHeight() != Output.height)
            {
                ReleaseOutput();
                BuildOutput();
            }
        }

        protected virtual void BuildOutput()
        {
            Output = new RenderTexture(CalculateOutputWidth(), CalculateOutputHeight(), 24, gameCamera.hdr ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            (Output as RenderTexture).useMipMap = (Output as RenderTexture).autoGenerateMips = false;
        }
        protected virtual void ReleaseOutput()
        {
            if (Output != null)
            {
                (Output as RenderTexture).Release();
                Output = null;
            }
        }

        protected int CalculateOutputWidth()
        {
            if (context.Data.outputWidth <= 0)
            {
                if (context.Data.outputHeight > 0)
                    return Mathf.RoundToInt((float)context.Data.outputHeight * Screen.width / Screen.height);
                else
                    return Screen.width;
            }
            else
                return context.Data.outputWidth;
        }
        protected int CalculateOutputHeight()
        {
            if (context.Data.outputHeight <= 0)
            {
                if (context.Data.outputWidth > 0)
                    return Mathf.RoundToInt((float)context.Data.outputWidth * Screen.height / Screen.width);
                else
                    return Screen.height;
            }
            else
                return context.Data.outputHeight;
        }

        public virtual void RenderScene()
        {

        }

        public virtual void RegisterFeed(InputFeed feed)
        {
            if (ActiveFeeds == null)
                ActiveFeeds = new List<InputFeed>();
            ActiveFeeds.Add(feed);
        }
        public virtual void UnregisterFeed(InputFeed feed)
        {
            ActiveFeeds.Remove(feed);
        }

#region Event Firing
        protected void RenderGameCamera(Camera cam, RenderTexture target)
        {
            cam.targetTexture = target;
            if (GameRenderStarted != null)
                GameRenderStarted(this);
            cam.aspect = (float)target.width / target.height;
            cam.Render();
            if (GameRenderEnded != null)
                GameRenderEnded(this);
            cam.targetTexture = null;
        }

        protected void CompleteFrame()
        {
            if (FrameCompleted != null)
                FrameCompleted(this);
        }
#endregion
    }
}