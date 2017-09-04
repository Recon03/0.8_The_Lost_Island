using UnityEngine;
using com.ootii.Base;
using com.ootii.Helpers;

namespace com.ootii.Cameras
{
    /// <summary>
    /// Motor that stays at the fixed offset from the anchor
    /// </summary>
    [BaseName("Fixed Motor")]
    [BaseDescription("Rig Motor that keeps the camera at the fixed position and rotation from the anchor.")]
    public class FixedMotor : CameraMotor
    {
        /// <summary>
        /// Offset from the anchor's rotation
        /// </summary>
        public Quaternion _RotationOffset = Quaternion.identity;
        public Quaternion RotationOffset
        {
            get { return _RotationOffset; }
            set { _RotationOffset = value; }
        }

        /// <summary>
        /// Updates the motor over time. This is called by the controller
        /// every update cycle so movement can be updated. 
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        /// <param name="rTiltAngle">Amount of tilting the camera needs to do to match the anchor</param>
        public override CameraTransform RigLateUpdate(float rDeltaTime, int rUpdateIndex, float rTiltAngle = 0f)
        {
            if (RigController == null) { return mRigTransform; }

            if (_UseRigAnchor && Anchor != null)
            {
                mRigTransform.Position = AnchorPosition + (Anchor.rotation * _Offset);
            }
            else
            {
                mRigTransform.Position = _Offset;
            }

            if (_UseRigAnchor && Anchor != null)
            {
                mRigTransform.Rotation = Anchor.rotation * _RotationOffset;
            }
            else
            {
                mRigTransform.Rotation = _RotationOffset;
            }

            return mRigTransform;
        }

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

#if UNITY_EDITOR

        /// <summary>
        /// Allow the motion to render it's own GUI
        /// </summary>
        public override bool OnInspectorGUI()
        {
            bool lIsDirty = false;

            GUILayout.Space(5f);

            if (base.OnInspectorGUI())
            {
                lIsDirty = true;
            }

            return lIsDirty;
        }

#endif

    }
}
