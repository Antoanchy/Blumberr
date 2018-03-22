/*======= (c) Blueprint Reality Inc., 2017. All rights reserved =======*/
using UnityEngine;
using UnityEngine.UI;

namespace BlueprintReality.MixCast
{
    public class RotateRoomControls : MonoBehaviour
    {
        public float angleStep = 90f;

        public KeyCode cwKey = KeyCode.LeftArrow;
        public KeyCode cwModifier = KeyCode.None;

        public KeyCode ccwKey = KeyCode.RightArrow;
        public KeyCode ccwModifier = KeyCode.None;

        void Update()
        {
            if (Selectable.allSelectables.Find(s => s is InputField && (s as InputField).isFocused) != null)
            {
                return;
            }

            bool goCw = Input.GetKeyDown(cwKey);
            if (cwModifier != KeyCode.None)
                goCw &= Input.GetKey(cwModifier);

            bool goCcw = Input.GetKeyDown(ccwKey);
            if (ccwModifier != KeyCode.None)
                goCcw &= Input.GetKey(ccwModifier);

            if (goCw)
                Camera.main.transform.parent.Rotate(0, angleStep, 0);
            else if (goCcw)
                Camera.main.transform.parent.Rotate(0, -angleStep, 0);
        }
    }
}