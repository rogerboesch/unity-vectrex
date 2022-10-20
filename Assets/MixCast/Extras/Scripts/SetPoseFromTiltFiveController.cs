/**********************************************************************************
* Blueprint Reality Inc. CONFIDENTIAL
* 2022 Blueprint Reality Inc.
* All Rights Reserved.
*
* NOTICE:  All information contained herein is, and remains, the property of
* Blueprint Reality Inc. and its suppliers, if any.  The intellectual and
* technical concepts contained herein are proprietary to Blueprint Reality Inc.
* and its suppliers and may be covered by Patents, pending patents, and are
* protected by trade secret or copyright law.
*
* Dissemination of this information or reproduction of this material is strictly
* forbidden unless prior written permission is obtained from Blueprint Reality Inc.
***********************************************************************************/

using System.Collections;
using System.Collections.Generic;

#if MIXCAST_TILTFIVE
using TiltFive;
#endif

namespace BlueprintReality.MixCast
{
    public class SetPoseFromTiltFiveController : UnityEngine.MonoBehaviour
    {
#if MIXCAST_TILTFIVE && UNITY_STANDALONE_WIN
        public ControllerIndex controller;
        public UnityEngine.GameObject gripObject;

        private void OnEnable()
        {
            ExpCameraScheduler.OnBeforeRender += UpdateWand;
        }
        private void OnDisable()
        {
            ExpCameraScheduler.OnBeforeRender -= UpdateWand;
        }

        void UpdateWand()
        {
            if (TiltFiveManager.Instance == null)
                return;

            if (GetTrackingAvailability())
            {
                GameBoardSettings boardSettings = TiltFiveManager.Instance.gameBoardSettings;
                ScaleSettings scaleSettings = TiltFiveManager.Instance.scaleSettings;

                gripObject.transform.position = Wand.GetPosition(controller, ControllerPosition.Grip);
                gripObject.transform.rotation = Wand.GetRotation(controller);
                gripObject.transform.localScale = UnityEngine.Vector3.one * scaleSettings.GetScaleToUWRLD_UGBD(boardSettings.gameBoardScale);

                gripObject.SetActive(true);
            }
            else
                gripObject.SetActive(false);
        }

        bool GetTrackingAvailability()
        {
            return Display.GetGlassesAvailability()
                && GameBoard.TryGetGameboardType(out var gameboardType)
                && gameboardType != GameboardType.GameboardType_None
                && Input.GetWandAvailability(controller);
        }
#endif
    }
}
