/*======= (c) Blueprint Reality Inc., 2017. All rights reserved =======*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlueprintReality.MixCast
{
    public class MixCastCameras : MonoBehaviour
    {
        public static MixCastCameras Instance { get; protected set; }

        public CameraConfigContext cameraPrefab;

        public List<CameraConfigContext> CameraInstances { get; protected set; }
        
        public event System.Action OnBeforeRender;

        private void OnEnable()
        {
            if ( Instance != null )
            {
                Debug.LogError("Should only have one MixCastCameras in the game");
                return;
            }

            
            Instance = this;

            MixCast.MixCastEnabled += HandleMixCastEnabled;
            MixCast.MixCastDisabled += HandleMixCastDisabled;

            GenerateCameras();

            StartCoroutine(RenderUsedCameras());
            StartCoroutine(RenderSpareCameras());
        }

        private void OnDisable()
        {
            StopCoroutine("RenderUsedCameras");
            StopCoroutine("RenderSpareCameras");

            DestroyCameras();

            MixCast.MixCastEnabled -= HandleMixCastEnabled;
            MixCast.MixCastDisabled -= HandleMixCastDisabled;

            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            List<MixCastData.CameraCalibrationData> createCams = new List<MixCastData.CameraCalibrationData>();
            List<CameraConfigContext> destroyCams = new List<CameraConfigContext>();

            createCams.AddRange(MixCast.Settings.cameras);
            destroyCams.AddRange(CameraInstances);
            for (int i = 0; i < CameraInstances.Count; i++)
                createCams.RemoveAll(c => c == CameraInstances[i].Data);
            for (int i = 0; i < MixCast.Settings.cameras.Count; i++)
                destroyCams.RemoveAll(c => c.Data == MixCast.Settings.cameras[i]);

            for (int i = 0; i < destroyCams.Count; i++)
            {
                CameraInstances.Remove(destroyCams[i]);
                Destroy(destroyCams[i].gameObject);
            }

            for( int i = 0; i < createCams.Count; i++ )
            {
                bool wasPrefabActive = cameraPrefab.gameObject.activeSelf;
                cameraPrefab.gameObject.SetActive(false);

                CameraConfigContext instance = Instantiate(cameraPrefab, transform, false);

                instance.Data = createCams[i];

                CameraInstances.Add(instance);

                cameraPrefab.gameObject.SetActive(wasPrefabActive);

                instance.gameObject.SetActive(MixCast.Active);
            }
        }

        
        IEnumerator RenderUsedCameras()
        {
            while (isActiveAndEnabled)
            {
                float startTime = Time.realtimeSinceStartup;
                float targetFrameTime = 1f / MixCast.Settings.global.targetFramerate;
                float endTime = startTime + targetFrameTime;

                yield return new WaitForEndOfFrame();

                // Manually call to update position of controllers right before we render.
                UpdatePoses();

                if (OnBeforeRender != null)
                {
                    OnBeforeRender();
                }

                foreach (MixCastCamera cam in MixCastCamera.ActiveCameras)
                    if (cam.IsInUse)
                        cam.RenderScene();

                if (Time.realtimeSinceStartup < endTime)
                    yield return new WaitForSecondsRealtime(endTime - Time.realtimeSinceStartup);
            }
        }

        IEnumerator RenderSpareCameras()
        {
            int lastSpareRenderedIndex = 0;
            while (isActiveAndEnabled)
            {
                if (MixCastCamera.ActiveCameras.Count > 0)
                {
                    for (int i = 0; i < MixCast.Settings.global.spareRendersPerFrame; i++)
                    {
                        int startIndex = lastSpareRenderedIndex;
                        lastSpareRenderedIndex++;
                        while (MixCastCamera.ActiveCameras[lastSpareRenderedIndex % MixCastCamera.ActiveCameras.Count].IsInUse && (lastSpareRenderedIndex - startIndex) <= MixCastCamera.ActiveCameras.Count)
                            lastSpareRenderedIndex++;

                        if (lastSpareRenderedIndex - startIndex <= MixCastCamera.ActiveCameras.Count)
                            MixCastCamera.ActiveCameras[lastSpareRenderedIndex % MixCastCamera.ActiveCameras.Count].RenderScene();
                    }
                }

                yield return null;
            }
        }
        
        private void UpdatePoses()
        {
#if MIXCAST_STEAMVR
            if (VRInfo.IsDeviceOpenVR())
            {
                var compositor = Valve.VR.OpenVR.Compositor;
                if (compositor != null)
                {
                    var render = SteamVR_Render.instance;
                    compositor.GetLastPoses(render.poses, render.gamePoses);
                    SteamVR_Events.NewPoses.Send(render.poses);
                    SteamVR_Events.NewPosesApplied.Send();
                }
            }
#endif
            // Oculus updates its poses by default on FixedUpdate()
        }

        void GenerateCameras()
        {
            CameraInstances = new List<CameraConfigContext>();

            bool wasPrefabActive = cameraPrefab.gameObject.activeSelf;
            cameraPrefab.gameObject.SetActive(false);
            for (int i = 0; i < MixCast.Settings.cameras.Count; i++)
            {
                CameraConfigContext instance = Instantiate(cameraPrefab, transform, false);

                instance.Data = MixCast.Settings.cameras[i];

                CameraInstances.Add(instance);
            }
            cameraPrefab.gameObject.SetActive(wasPrefabActive);

            SetCamerasActive(MixCast.Active);
        }
        void DestroyCameras()
        {
            for (int i = 0; i < CameraInstances.Count; i++)
                Destroy(CameraInstances[i].gameObject);

            CameraInstances.Clear();
            CameraInstances = null;
        }
        private void HandleMixCastEnabled()
        {
            SetCamerasActive(true);
        }
        private void HandleMixCastDisabled()
        {
            SetCamerasActive(false);
        }

        void SetCamerasActive(bool active)
        {
            if (CameraInstances == null)
                return;

            for (int i = 0; i < CameraInstances.Count; i++)
                CameraInstances[i].gameObject.SetActive(active);
        }
    }
}