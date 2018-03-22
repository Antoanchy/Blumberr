/*======= (c) Blueprint Reality Inc., 2017. All rights reserved =======*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlueprintReality.MixCast
{
    public class MixCast
    {
        public const string WEBSITE_URL = "http://blueprinttools.com/mixcast";
        public const string VERSION_STRING = "1.4.0";

        public static bool Active { get; protected set; }

        public static event System.Action MixCastEnabled;
        public static event System.Action MixCastDisabled;


        public static MixCastData Settings { get; protected set; }
        public static MixCastData.CameraCalibrationData DisplayingCamera { get; set; }
        public static List<MixCastData.CameraCalibrationData> RecordingCameras { get; protected set; }
        public static List<MixCastData.CameraCalibrationData> StreamingCameras { get; protected set; }

        static MixCast()
        {
            Settings = MixCastRegistry.ReadData();
            DisplayingCamera = null;
            RecordingCameras = new List<MixCastData.CameraCalibrationData>();
            StreamingCameras = new List<MixCastData.CameraCalibrationData>();
        }

        public static void SetActive(bool active)
        {
            if (Active == active)
                return;

            Active = active;
            if (Active)
            {
                MixCastEnabled();
                if (Settings.cameras.Count > 0)
                    DisplayingCamera = Settings.cameras[0];
            }
            else
                MixCastDisabled();
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("MixCast/Go to Website")]
        public static void GoToWebsite()
        {
            Application.OpenURL(WEBSITE_URL);
        }
#endif
    }
}