using UnityEngine;
using com.ootii.Cameras;
using com.ootii.Geometry;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.AnimationControllers
{
    /// <summary>
    /// </summary>
    [MotionName("Walk Run Pivot")]
    [MotionDescription("Standard movement (walk/run) for an adventure game.")]
    public class WalkRunPivot_v2 : MotionControllerMotion, IWalkRunMotion
    {
        /// <summary>
        /// Trigger values for th emotion
        /// </summary>
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 27130;
        public const int PHASE_END_RUN = 27131;
        public const int PHASE_END_WALK = 27132;
        public const int PHASE_RESUME = 27133;

        public const int PHASE_START_IDLE_PIVOT = 27135;

        /// <summary>
        /// Determines if we run by default or walk
        /// </summary>
        public bool _DefaultToRun = false;
        public bool DefaultToRun
        {
            get { return _DefaultToRun; }
            set { _DefaultToRun = value; }
        }

        /// <summary>
        /// Speed (units per second) when walking
        /// </summary>
        public float _WalkSpeed = 0f;
        public virtual float WalkSpeed
        {
            get { return _WalkSpeed; }
            set { _WalkSpeed = value; }
        }

        /// <summary>
        /// Speed (units per second) when running
        /// </summary>
        public float _RunSpeed = 0f;
        public virtual float RunSpeed
        {
            get { return _RunSpeed; }
            set { _RunSpeed = value; }
        }

        /// <summary>
        /// Determines if we rotate to match the camera
        /// </summary>
        public bool _RotateWithCamera = true;
        public bool RotateWithCamera
        {
            get { return _RotateWithCamera; }
            set { _RotateWithCamera = value; }
        }

        /// <summary>
        /// User layer id set for objects that are climbable.
        /// </summary>
        public string _RotateActionAlias = "ActivateRotation";
        public string RotateActionAlias
        {
            get { return _RotateActionAlias; }
            set { _RotateActionAlias = value; }
        }

        /// <summary>
        /// Degrees per second to rotate the actor in order to face the input direction
        /// </summary>
        public float _RotationSpeed = 180f;
        public float RotationSpeed
        {
            get { return _RotationSpeed; }
            set { _RotationSpeed = value; }
        }

        /// <summary>
        /// Determines if we shortcut the motion and start in the loop
        /// </summary>
        private bool mStartInMove = false;
        public bool StartInMove
        {
            get { return mStartInMove; }
            set { mStartInMove = value; }
        }

        /// <summary>
        /// Determines if we shortcut the motion and start in a run
        /// </summary>
        private bool mStartInWalk = false;
        public bool StartInWalk
        {
            get { return mStartInWalk; }

            set
            {
                mStartInWalk = value;
                if (value) { mStartInMove = value; }
            }
        }

        /// <summary>
        /// Determines if we shortcut the motion and start in a run
        /// </summary>
        private bool mStartInRun = false;
        public bool StartInRun
        {
            get { return mStartInRun; }

            set
            {
                mStartInRun = value;
                if (value) { mStartInMove = value; }
            }
        }

        /// <summary>
        /// Determines if we'll use the start transitions when starting from idle
        /// </summary>
        public bool _UseStartTransitions = true;
        public bool UseStartTransitions
        {
            get { return _UseStartTransitions; }
            set { _UseStartTransitions = value; }
        }

        /// <summary>
        /// Determines if we'll use the start transitions when stopping movement
        /// </summary>
        public bool _UseStopTransitions = true;
        public bool UseStopTransitions
        {
            get { return _UseStopTransitions; }
            set { _UseStopTransitions = value; }
        }

        /// <summary>
        /// Determines if the character can pivot while idle
        /// </summary>
        public bool _UseTapToPivot = false;
        public bool UseTapToPivot
        {
            get { return _UseTapToPivot; }
            set { _UseTapToPivot = value; }
        }

        /// <summary>
        /// Determines how long we wait before testing for an idle pivot
        /// </summary>
        public float _TapToPivotDelay = 0.2f;
        public float TapToPivotDelay
        {
            get { return _TapToPivotDelay; }
            set { _TapToPivotDelay = value; }
        }

        /// <summary>
        /// Minimum angle before we use the pivot speed
        /// </summary>
        public float _MinPivotAngle = 20f;
        public float MinPivotAngle
        {
            get { return _MinPivotAngle; }
            set { _MinPivotAngle = value; }
        }

        /// <summary>
        /// Number of samples to use for smoothing
        /// </summary>
        public int _SmoothingSamples = 10;
        public int SmoothingSamples
        {
            get { return _SmoothingSamples; }

            set
            {
                _SmoothingSamples = value;

                mInputX.SampleCount = _SmoothingSamples;
                mInputY.SampleCount = _SmoothingSamples;
                mInputMagnitude.SampleCount = _SmoothingSamples;
            }
        }
        
        /// <summary>
        /// Determines if the actor should be running based on input
        /// </summary>
        public bool IsRunActive
        {
            get
            {
                if (mMotionController._InputSource == null) { return _DefaultToRun; }
                return ((_DefaultToRun && !mMotionController._InputSource.IsPressed(_ActionAlias)) || (!_DefaultToRun && mMotionController._InputSource.IsPressed(_ActionAlias)));
            }
        }

        /// <summary>
        /// Determine if we're pivoting from an idle
        /// </summary>
        protected bool mStartInPivot = false;

        /// <summary>
        /// Angle of the input from when the motion was activated
        /// </summary>
        protected Vector3 mSavedInputForward = Vector3.zero;

        /// <summary>
        /// Time that has elapsed since there was no input
        /// </summary>
        protected float mNoInputElapsed = 0f;

        /// <summary>
        /// Phase ID we're using to transition out
        /// </summary>
        protected int mExitPhaseID = 0;

        /// <summary>
        /// Frame level rotation test
        /// </summary>
        protected bool mRotateWithCamera = false;

        /// <summary>
        /// Determines if the actor rotation should be linked to the camera
        /// </summary>
        protected bool mLinkRotation = false;

        /// <summary>
        /// We use these classes to help smooth the input values so that
        /// movement doesn't drop from 1 to 0 immediately.
        /// </summary>
        protected FloatValue mInputX = new FloatValue(0f, 10);
        protected FloatValue mInputY = new FloatValue(0f, 10);
        protected FloatValue mInputMagnitude = new FloatValue(0f, 15);

        /// <summary>
        /// Last time we had input activity
        /// </summary>
        protected float mLastTapStartTime = 0f;
        protected float mLastTapInputFromAvatarAngle = 0f;
        protected Vector3 mLastTapInputForward = Vector3.zero;

        /// <summary>
        /// Default constructor
        /// </summary>
        public WalkRunPivot_v2()
            : base()
        {
            _Category = EnumMotionCategories.WALK;

            _Priority = 5;
            _ActionAlias = "Run";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "WalkRunPivot v2-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public WalkRunPivot_v2(MotionController rController)
            : base(rController)
        {
            _Category = EnumMotionCategories.WALK;

            _Priority = 5;
            _ActionAlias = "Run";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "WalkRunPivot v2-SM"; }
#endif
        }

        /// <summary>
        /// Awake is called after all objects are initialized so you can safely speak to other objects. This is where
        /// reference can be associated.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            // Initialize the smoothing variables
            SmoothingSamples = _SmoothingSamples;
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion
        /// isn't actually started.
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate()
        {
            if (!mIsStartable ||
                !mMotionController.IsGrounded ||
                mActorController.State.Stance != EnumControllerStance.TRAVERSAL)
            {
                mStartInPivot = false;
                mLastTapStartTime = 0f;
                return false;
            }

            bool lIsPivotable = (_UseTapToPivot && (mLastTapStartTime > 0f || Mathf.Abs(mMotionController.State.InputFromAvatarAngle) > _MinPivotAngle));

            bool lIsIdling = (_UseTapToPivot && mMotionLayer.ActiveMotion != null && mMotionLayer.ActiveMotion.Category == EnumMotionCategories.IDLE);

            // Determine if tapping is enabled
            if (_UseTapToPivot && lIsPivotable && lIsIdling)
            {
                // If there's input, it could be the start of a tap or true movement
                if (mMotionController.State.InputMagnitudeTrend.Value > 0.1f)
                {
                    // Start the timer
                    if (mLastTapStartTime == 0f)
                    {
                        mLastTapStartTime = Time.time;
                        mLastTapInputForward = mMotionController.State.InputForward;
                        mLastTapInputFromAvatarAngle = mMotionController.State.InputFromAvatarAngle;
                    }
                    // Timer has expired. So, we must really be moving
                    else if (mLastTapStartTime + _TapToPivotDelay <= Time.time)
                    {
                        mStartInPivot = false;
                        mLastTapStartTime = 0f;
                        return true;
                    }

                    // Keep waiting
                    return false;
                }
                // No input. So, at the end of a tap or there really is nothing
                else
                {
                    if (mLastTapStartTime > 0f)
                    {
                        mStartInPivot = true;
                        mLastTapStartTime = 0f;
                        return true;
                    }
                }
            }
            // If not, we do normal processing
            else
            {
                mStartInPivot = false;
                mLastTapStartTime = 0f;

                // If there's enough movement, start the motion
                if (mMotionController.State.InputMagnitudeTrend.Value > 0.49f)
                {
                    return true;
                }
            }

            // Don't activate
            return false;
        }

        /// <summary>
        /// Tests if the motion should continue. If it shouldn't, the motion
        /// is typically disabled
        /// </summary>
        /// <returns></returns>
        public override bool TestUpdate()
        {
            if (mIsActivatedFrame) { return true; }
            if (!mMotionController.IsGrounded) { return false; }

            // Our idle pose is a good exit
            if (mMotionLayer._AnimatorStateID == STATE_IdlePose)
            {
                return false;
            }

            // Our exit pose for the idle pivots
            if (mMotionLayer._AnimatorStateID == STATE_IdleTurnEndPose)
            {
                if (mMotionController.State.InputMagnitudeTrend.Value < 0.1f)
                {
                    return false;
                }
            }

            // One last check to make sure we're in this state
            if (mIsAnimatorActive && !IsInMotionState)
            {
                return false;
            }

            // If we no longer have input and we're in normal movement, we can stop
            if (mMotionController.State.InputMagnitudeTrend.Average < 0.1f)
            {
                if (mMotionLayer._AnimatorStateID == STATE_MoveTree && mMotionLayer._AnimatorTransitionID == 0)
                {
                    return false;
                }
            }

            // Stay
            return true;
        }

        /// <summary>
        /// Raised when a motion is being interrupted by another motion
        /// </summary>
        /// <param name="rMotion">Motion doing the interruption</param>
        /// <returns>Boolean determining if it can be interrupted</returns>
        public override bool TestInterruption(MotionControllerMotion rMotion)
        {
            // Since we're dealing with a blend tree, keep the value until the transition completes            
            mMotionController.ForcedInput.x = mInputX.Average;
            mMotionController.ForcedInput.y = mInputY.Average;

            return true;
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            mExitPhaseID = 0;
            mSavedInputForward = mMotionController.State.InputForward;

            // Update the max speed based on our animation
            mMotionController.MaxSpeed = 5.668f;

            // Determine how we start
            if (mStartInPivot)
            {
                mMotionController.State.InputFromAvatarAngle = mLastTapInputFromAvatarAngle;
                mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START_IDLE_PIVOT, 0, true);
            }
            else if (mStartInMove)
            {
                mStartInMove = false;
                mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, 1, true);
            }
            else if (mMotionController._InputSource == null)
            {
                mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, (_UseStartTransitions ? 0 : 1), true);
            }
            else
            {
                // Grab the state info
                MotionState lState = mMotionController.State;

                // Convert the input to radial so we deal with keyboard and gamepad input the same.
                float lInputX = lState.InputX;
                float lInputY = lState.InputY;
                float lInputMagnitude = lState.InputMagnitudeTrend.Value;
                InputManagerHelper.ConvertToRadialInput(ref lInputX, ref lInputY, ref lInputMagnitude, (IsRunActive ? 1f : 0.5f));

                // Smooth the input
                if (lInputX != 0f || lInputY < 0f)
                {
                    mInputX.Clear(lInputX);
                    mInputY.Clear(lInputY);
                    mInputMagnitude.Clear(lInputMagnitude);
                }

                // Start the motion
                mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, (_UseStartTransitions ? 0 : 1), true);
            }

            // Register this motion with the camera
            if (_RotateWithCamera && mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate += OnCameraUpdated;
            }

            // Flag this motion as active
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Raised when we shut the motion down
        /// </summary>
        public override void Deactivate()
        {
            mLastTapStartTime = 0f;
            mLastTapInputFromAvatarAngle = 0f;

            // Clear out the start
            mStartInPivot = false;
            mStartInRun = false;
            mStartInWalk = false;

            // Register this motion with the camera
            if (mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
            }

            base.Deactivate();
        }

        /// <summary>
        /// Allows the motion to modify the velocity before it is applied. 
        /// 
        /// NOTE:
        /// Be careful when removing rotations
        /// as some transitions will want rotations even if the state they are transitioning from don't.
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        /// <param name="rMovement">Amount of movement caused by root motion this frame</param>
        /// <param name="rRotation">Amount of rotation caused by root motion this frame</param>
        /// <returns></returns>
        public override void UpdateRootMotion(float rDeltaTime, int rUpdateIndex, ref Vector3 rMovement, ref Quaternion rRotation)
        {
            if ((mMotionLayer._AnimatorTransitionID == TRANS_AnyState_MoveTree) ||
                (mMotionLayer._AnimatorStateID == STATE_MoveTree && mMotionLayer._AnimatorTransitionID == 0))
            {
                rRotation = Quaternion.identity;

                // Override root motion if we're meant to
                float lMovementSpeed = (IsRunActive ? _RunSpeed : _WalkSpeed);
                if (lMovementSpeed > 0f)
                {
                    if (rMovement.sqrMagnitude > 0f)
                    {
                    rMovement = rMovement.normalized * (lMovementSpeed * rDeltaTime);
                    }
                    else
                    {
                        Vector3 lDirection = new Vector3(0f, 0f, 1f);
                        rMovement = lDirection.normalized * (lMovementSpeed * rDeltaTime);
                    }
                }

                rMovement.x = 0f;
                rMovement.y = 0f;
                if (rMovement.z < 0f) { rMovement.z = 0f; }
            }
            else
            {
                if (_UseTapToPivot && IsIdlePivoting())
                {
                    rMovement = Vector3.zero;
                }
                // If we're stopping, add some lag
                else if (IsStopping())
                {
                    rMovement = rMovement * 0.5f;
                }
            }
        }

        /// <summary>
        /// Updates the motion over time. This is called by the controller
        /// every update cycle so animations and stages can be updated.
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        public override void Update(float rDeltaTime, int rUpdateIndex)
        {
            mMovement = Vector3.zero;
            mRotation = Quaternion.identity;

            if (_UseTapToPivot && IsIdlePivoting())
            {
                UpdateIdlePivot(rDeltaTime, rUpdateIndex);
            }
            else
            {
                UpdateMovement(rDeltaTime, rUpdateIndex);
            }
        }

        /// <summary>
        /// Update processing for the idle pivot
        /// </summary>
        /// <param name="rDeltaTime"></param>
        /// <param name="rUpdateIndex"></param>
        private void UpdateIdlePivot(float rDeltaTime, int rUpdateIndex)
        {
            int lStateID = mMotionLayer._AnimatorStateID;
            if (lStateID == STATE_IdleTurn180L ||
                lStateID == STATE_IdleTurn90L ||
                lStateID == STATE_IdleTurn20L ||
                lStateID == STATE_IdleTurn20R ||
                lStateID == STATE_IdleTurn90R ||
                lStateID == STATE_IdleTurn180R)
            {
                if (mMotionLayer._AnimatorTransitionID != 0 && mLastTapInputForward.sqrMagnitude > 0f)
                {
                    if (mMotionController._CameraTransform != null)
                    {
                        Vector3 lInputForward = mMotionController._CameraTransform.rotation * mLastTapInputForward;

                        float lAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, lInputForward, mMotionController._Transform.up);
                        mRotation = Quaternion.Euler(0f, lAngle * mMotionLayer._AnimatorTransitionNormalizedTime, 0f);
                    }
                }
            }
        }

        /// <summary>
        /// Update processing for moving
        /// </summary>
        /// <param name="rDeltaTime"></param>
        /// <param name="rUpdateIndex"></param>
        private void UpdateMovement(float rDeltaTime, int rUpdateIndex)
        {
            bool lUpdateSamples = true;

            // Store the last valid input we had
            if (mMotionController.State.InputMagnitudeTrend.Value > 0.4f)
            {
                mExitPhaseID = 0;
                mNoInputElapsed = 0f;
                mSavedInputForward = mMotionController.State.InputForward;

                // If we were stopping, allow us to resume without leaving the motion
                if (IsStopping())
                {
                    mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_RESUME, 0, true);
                }
            }
            else
            {
                mNoInputElapsed = mNoInputElapsed + rDeltaTime;

                if (_UseStopTransitions)
                {
                    lUpdateSamples = false;

                    // If we've passed the delay, we really are stopping
                    if (mNoInputElapsed > 0.2f)
                    {
                        // Determine how we'll stop
                        if (mExitPhaseID == 0)
                        {
                            mExitPhaseID = (mInputMagnitude.Average < 0.6f ? PHASE_END_WALK : PHASE_END_RUN);
                        }

                        // Ensure we actually stop that way
                        if (mExitPhaseID != 0 && mMotionLayer._AnimatorStateID == STATE_MoveTree && mMotionLayer._AnimatorTransitionID == 0)
                        {
                            mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, mExitPhaseID, 0, true);
                        }
                    }
                }
            }

            // If we need to update the samples... 
            if (lUpdateSamples)
            {
                MotionState lState = mMotionController.State;

                // Convert the input to radial so we deal with keyboard and gamepad input the same.
                float lInputMax = (IsRunActive ? 1f : 0.5f);

                float lInputX = Mathf.Clamp(lState.InputX, -lInputMax, lInputMax);
                float lInputY = Mathf.Clamp(lState.InputY, -lInputMax, lInputMax);
                float lInputMagnitude = Mathf.Clamp(lState.InputMagnitudeTrend.Value, 0f, lInputMax);
                InputManagerHelper.ConvertToRadialInput(ref lInputX, ref lInputY, ref lInputMagnitude);

                // Smooth the input
                mInputX.Add(lInputX);
                mInputY.Add(lInputY);
                mInputMagnitude.Add(lInputMagnitude);
            }

            // Modify the input values to add some lag
            mMotionController.State.InputX = mInputX.Average;
            mMotionController.State.InputY = mInputY.Average;
            mMotionController.State.InputMagnitudeTrend.Replace(mInputMagnitude.Average);

            // We may want to rotate with the camera if we're facing forward
            mRotateWithCamera = false;
            if (_RotateWithCamera && mMotionController._CameraTransform != null)
            {
                float lToCameraAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, mMotionController._CameraTransform.forward, mMotionController._Transform.up);
                mRotateWithCamera = (Mathf.Abs(lToCameraAngle) < _RotationSpeed * rDeltaTime);

                if (mRotateWithCamera && mMotionLayer._AnimatorStateID != STATE_MoveTree) { mRotateWithCamera = false; }
                if (mRotateWithCamera && mMotionLayer._AnimatorTransitionID != 0) { mRotateWithCamera = false; }
                if (mRotateWithCamera && (Mathf.Abs(mMotionController.State.InputX) > 0.05f || mMotionController.State.InputY <= 0f)) { mRotateWithCamera = false; }
                if (mRotateWithCamera && (_RotateActionAlias.Length > 0 && !mMotionController._InputSource.IsPressed(_RotateActionAlias))) { mRotateWithCamera = false; }
            }

            // If we're meant to rotate with the camera (and OnCameraUpdate isn't already attached), do it here
            if (_RotateWithCamera && !(mMotionController.CameraRig is BaseCameraRig))
            {
                OnCameraUpdated(rDeltaTime, rUpdateIndex, null);
            }

            // We only allow input rotation under certain circumstances
            if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_MoveTree ||
                (mMotionLayer._AnimatorStateID == STATE_MoveTree && mMotionLayer._AnimatorTransitionID == 0) ||

                (mMotionLayer._AnimatorStateID == STATE_IdleToWalk180L && mMotionLayer._AnimatorStateNormalizedTime > 0.7f) ||
                (mMotionLayer._AnimatorStateID == STATE_IdleToWalk90L && mMotionLayer._AnimatorStateNormalizedTime > 0.6f) ||
                (mMotionLayer._AnimatorStateID == STATE_IdleToWalk90R && mMotionLayer._AnimatorStateNormalizedTime > 0.6f) ||
                (mMotionLayer._AnimatorStateID == STATE_IdleToWalk180R && mMotionLayer._AnimatorStateNormalizedTime > 0.7f) ||

                (mMotionLayer._AnimatorStateID == STATE_IdleToRun180L && mMotionLayer._AnimatorStateNormalizedTime > 0.6f) ||
                (mMotionLayer._AnimatorStateID == STATE_IdleToRun90L && mMotionLayer._AnimatorStateNormalizedTime > 0.6f) ||
                (mMotionLayer._AnimatorStateID == STATE_IdleToRun) ||
                (mMotionLayer._AnimatorStateID == STATE_IdleToRun90R && mMotionLayer._AnimatorStateNormalizedTime > 0.6f) ||
                (mMotionLayer._AnimatorStateID == STATE_IdleToRun180R && mMotionLayer._AnimatorStateNormalizedTime > 0.6f)
                )
            {
                // Since we're not rotating with the camera, rotate with input
                if (!mRotateWithCamera)
                {
                    if (mMotionController._CameraTransform != null && mMotionController.State.InputForward.sqrMagnitude == 0f)
                    {
                        RotateToInput(mMotionController._CameraTransform.rotation * mSavedInputForward, rDeltaTime, ref mRotation);
                    }
                    else
                    {
                        RotateToInput(mMotionController.State.InputFromAvatarAngle, rDeltaTime, ref mRotation);
                    }
                }
            }
        }

        /// <summary>
        /// Create a rotation velocity that rotates the character based on input
        /// </summary>
        /// <param name="rInputForward"></param>
        /// <param name="rDeltaTime"></param>
        private void RotateToInput(Vector3 rInputForward, float rDeltaTime, ref Quaternion rRotation)
        {
            float lAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, rInputForward, mMotionController._Transform.up);
            if (lAngle != 0f)
            {
                if (_RotationSpeed > 0f && Mathf.Abs(lAngle) > _RotationSpeed * rDeltaTime)
                {
                    lAngle = Mathf.Sign(lAngle) * _RotationSpeed * rDeltaTime;
                }

                rRotation = Quaternion.Euler(0f, lAngle, 0f);
            }
        }

        /// <summary>
        /// Create a rotation velocity that rotates the character based on input
        /// </summary>
        /// <param name="rInputFromAvatarAngle"></param>
        /// <param name="rDeltaTime"></param>
        private void RotateToInput(float rInputFromAvatarAngle, float rDeltaTime, ref Quaternion rRotation)
        {
            if (rInputFromAvatarAngle != 0f)
            {
                if (_RotationSpeed > 0f && Mathf.Abs(rInputFromAvatarAngle) > _RotationSpeed * rDeltaTime)
                {
                    rInputFromAvatarAngle = Mathf.Sign(rInputFromAvatarAngle) * _RotationSpeed * rDeltaTime;
                }

                rRotation = Quaternion.Euler(0f, rInputFromAvatarAngle, 0f);
            }
        }

        /// <summary>
        /// When we want to rotate based on the camera direction, we need to tweak the actor
        /// rotation AFTER we process the camera. Otherwise, we can get small stutters during camera rotation. 
        /// 
        /// This is the only way to keep them totally in sync. It also means we can't run any of our AC processing
        /// as the AC already ran. So, we do minimal work here
        /// </summary>
        /// <param name="rDeltaTime"></param>
        /// <param name="rUpdateCount"></param>
        /// <param name="rCamera"></param>
        private void OnCameraUpdated(float rDeltaTime, int rUpdateIndex, BaseCameraRig rCamera)
        {
            if (!mRotateWithCamera)
            {
                mLinkRotation = false;
                return;
            }

            float lToCameraAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, mMotionController._CameraTransform.forward, mMotionController._Transform.up);
            if (!mLinkRotation && Mathf.Abs(lToCameraAngle) <= _RotationSpeed * rDeltaTime) { mLinkRotation = true; }

            if (!mLinkRotation)
            {
                float lRotationAngle = Mathf.Abs(lToCameraAngle);
                float lRotationSign = Mathf.Sign(lToCameraAngle);
                lToCameraAngle = lRotationSign * Mathf.Min(_RotationSpeed * rDeltaTime, lRotationAngle);
            }

            Quaternion lRotation = Quaternion.AngleAxis(lToCameraAngle, Vector3.up);
            mActorController.Yaw = mActorController.Yaw * lRotation;
            mActorController._Transform.rotation = mActorController.Tilt * mActorController.Yaw;
        }

        /// <summary>
        /// Tests if we're in one of the stopping states
        /// </summary>
        /// <returns></returns>
        private bool IsStopping()
        {
            if (!_UseStopTransitions) { return false; }

            int lStateID = mMotionLayer._AnimatorStateID;
            if (lStateID == STATE_RunToIdle_LDown) { return true; }
            if (lStateID == STATE_RunToIdle_RDown) { return true; }
            if (lStateID == STATE_WalkToIdle_LDown) { return true; }
            if (lStateID == STATE_WalkToIdle_RDown) { return true; }

            int lTransitionID = mMotionLayer._AnimatorTransitionID;
            if (lTransitionID == TRANS_MoveTree_RunToIdle_LDown) { return true; }
            if (lTransitionID == TRANS_MoveTree_RunToIdle_RDown) { return true; }
            if (lTransitionID == TRANS_MoveTree_WalkToIdle_LDown) { return true; }
            if (lTransitionID == TRANS_MoveTree_WalkToIdle_RDown) { return true; }

            return false;
        }

        /// <summary>
        /// Tests if we're in one of the pivoting states
        /// </summary>
        /// <returns></returns>
        private bool IsIdlePivoting()
        {
            if (!_UseTapToPivot) { return false; }

            int lStateID = mMotionLayer._AnimatorStateID;
            if (lStateID == STATE_IdleTurn180L) { return true; }
            if (lStateID == STATE_IdleTurn90L) { return true; }
            if (lStateID == STATE_IdleTurn20L) { return true; }
            if (lStateID == STATE_IdleTurn20R) { return true; }
            if (lStateID == STATE_IdleTurn90R) { return true; }
            if (lStateID == STATE_IdleTurn180R) { return true; }

            int lTransitionID = mMotionLayer._AnimatorTransitionID;
            if (lTransitionID == TRANS_EntryState_IdleTurn180L) { return true; }
            if (lTransitionID == TRANS_EntryState_IdleTurn90L) { return true; }
            if (lTransitionID == TRANS_EntryState_IdleTurn20L) { return true; }
            if (lTransitionID == TRANS_EntryState_IdleTurn20R) { return true; }
            if (lTransitionID == TRANS_EntryState_IdleTurn90R) { return true; }
            if (lTransitionID == TRANS_EntryState_IdleTurn180R) { return true; }

            return false;
        }

        #region Editor Functions

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

#if UNITY_EDITOR

        /// <summary>
        /// Creates input settings in the Unity Input Manager
        /// </summary>
        public override void CreateInputManagerSettings()
        {
            if (!InputManagerHelper.IsDefined(_ActionAlias))
            {
                InputManagerEntry lEntry = new InputManagerEntry();
                lEntry.Name = _ActionAlias;
                lEntry.PositiveButton = "left shift";
                lEntry.Gravity = 1000;
                lEntry.Dead = 0.001f;
                lEntry.Sensitivity = 1000;
                lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                lEntry.Axis = 0;
                lEntry.JoyNum = 0;

                InputManagerHelper.AddEntry(lEntry, true);

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX

                lEntry = new InputManagerEntry();
                lEntry.Name = _ActionAlias;
                lEntry.PositiveButton = "";
                lEntry.Gravity = 1;
                lEntry.Dead = 0.3f;
                lEntry.Sensitivity = 1;
                lEntry.Type = InputManagerEntryType.JOYSTICK_AXIS;
                lEntry.Axis = 5;
                lEntry.JoyNum = 0;

                InputManagerHelper.AddEntry(lEntry, true);

#else

                lEntry = new InputManagerEntry();
                lEntry.Name = _ActionAlias;
                lEntry.PositiveButton = "";
                lEntry.Gravity = 1;
                lEntry.Dead = 0.3f;
                lEntry.Sensitivity = 1;
                lEntry.Type = InputManagerEntryType.JOYSTICK_AXIS;
                lEntry.Axis = 9;
                lEntry.JoyNum = 0;

                InputManagerHelper.AddEntry(lEntry, true);

#endif
            }
        }
        
        /// <summary>
        /// Allow the motion to render it's own GUI
        /// </summary>
        public override bool OnInspectorGUI()
        {
            bool lIsDirty = false;

            if (EditorHelper.BoolField("Default to Run", "Determines if the default is to run or walk.", DefaultToRun, mMotionController))
            {
                lIsDirty = true;
                DefaultToRun = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.TextField("Action Alias", "Action alias that triggers a run or walk (which ever is opposite the default).", ActionAlias, mMotionController))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Walk Speed", "Speed (units per second) to move when walking. Set to 0 to use root-motion.", WalkSpeed, mMotionController))
            {
                lIsDirty = true;
                WalkSpeed = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField("Run Speed", "Speed (units per second) to move when running. Set to 0 to use root-motion.", RunSpeed, mMotionController))
            {
                lIsDirty = true;
                RunSpeed = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Rotate With Camera", "Determines if we rotate to match the camera.", RotateWithCamera, mMotionController))
            {
                lIsDirty = true;
                RotateWithCamera = EditorHelper.FieldBoolValue;
            }

            if (RotateWithCamera)
            {
                if (EditorHelper.TextField("Rotate Action Alias", "Action alias determines if rotation is activated. This typically matches the input source's View Activator.", RotateActionAlias, mMotionController))
                {
                    lIsDirty = true;
                    RotateActionAlias = EditorHelper.FieldStringValue;
                }
            }

            if (EditorHelper.FloatField("Rotation Speed", "Degrees per second to rotate the actor ('0' means instant rotation).", RotationSpeed, mMotionController))
            {
                lIsDirty = true;
                RotationSpeed = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Use Start Transitions", "Determines if we'll use the start transitions when coming from idle", UseStartTransitions, mMotionController))
            {
                lIsDirty = true;
                UseStartTransitions = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Use Stop Transitions", "Determines if we'll use the stop transitions when stopping movement", UseStopTransitions, mMotionController))
            {
                lIsDirty = true;
                UseStopTransitions = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Use Tap to Pivot", "Determines if taping a direction while idle will pivot the character without moving them.", UseTapToPivot, mMotionController))
            {
                lIsDirty = true;
                UseTapToPivot = EditorHelper.FieldBoolValue;
            }

            if (UseTapToPivot)
            {
                EditorGUILayout.BeginHorizontal();

                if (EditorHelper.FloatField("Min Angle", "Sets the minimum angle between the input direction and character direction where we'll do a pivot.", MinPivotAngle, mMotionController))
                {
                    lIsDirty = true;
                    MinPivotAngle = EditorHelper.FieldFloatValue;
                }

                GUILayout.Space(10f);

                EditorGUILayout.LabelField(new GUIContent("Delay", "Delay in seconds before we test if we're NOT pivoting, but moving. In my tests, the average tap took 0.12 to 0.15 seconds."), GUILayout.Width(40f));
                if (EditorHelper.FloatField(TapToPivotDelay, "Delay", mMotionController, 40f))
                {
                    lIsDirty = true;
                    TapToPivotDelay = EditorHelper.FieldFloatValue;
                }

                GUILayout.FlexibleSpace();

                EditorGUILayout.EndHorizontal();
            }

            if (EditorHelper.IntField("Smoothing Samples", "The more samples the smoother movement is, but the less responsive.", SmoothingSamples, mMotionController))
            {
                lIsDirty = true;
                SmoothingSamples = EditorHelper.FieldIntValue;
            }

            return lIsDirty;
        }

#endif

        #endregion

        #region Auto-Generated
        // ************************************ START AUTO GENERATED ************************************

        /// <summary>
        /// These declarations go inside the class so you can test for which state
        /// and transitions are active. Testing hash values is much faster than strings.
        /// </summary>
        public static int STATE_MoveTree = -1;
        public static int STATE_IdleToWalk90L = -1;
        public static int STATE_IdleToWalk90R = -1;
        public static int STATE_IdleToWalk180R = -1;
        public static int STATE_IdleToWalk180L = -1;
        public static int STATE_IdlePose = -1;
        public static int STATE_IdleToRun90L = -1;
        public static int STATE_IdleToRun180L = -1;
        public static int STATE_IdleToRun90R = -1;
        public static int STATE_IdleToRun180R = -1;
        public static int STATE_IdleToRun = -1;
        public static int STATE_RunPivot180R_LDown = -1;
        public static int STATE_WalkPivot180L = -1;
        public static int STATE_RunToIdle_LDown = -1;
        public static int STATE_WalkToIdle_LDown = -1;
        public static int STATE_WalkToIdle_RDown = -1;
        public static int STATE_RunToIdle_RDown = -1;
        public static int STATE_IdleTurn20R = -1;
        public static int STATE_IdleTurn90R = -1;
        public static int STATE_IdleTurn180R = -1;
        public static int STATE_IdleTurn20L = -1;
        public static int STATE_IdleTurn90L = -1;
        public static int STATE_IdleTurn180L = -1;
        public static int STATE_IdleTurnEndPose = -1;
        public static int TRANS_AnyState_IdleToWalk90L = -1;
        public static int TRANS_EntryState_IdleToWalk90L = -1;
        public static int TRANS_AnyState_IdleToWalk90R = -1;
        public static int TRANS_EntryState_IdleToWalk90R = -1;
        public static int TRANS_AnyState_IdleToWalk180R = -1;
        public static int TRANS_EntryState_IdleToWalk180R = -1;
        public static int TRANS_AnyState_MoveTree = -1;
        public static int TRANS_EntryState_MoveTree = -1;
        public static int TRANS_AnyState_IdleToWalk180L = -1;
        public static int TRANS_EntryState_IdleToWalk180L = -1;
        public static int TRANS_AnyState_IdleToRun180L = -1;
        public static int TRANS_EntryState_IdleToRun180L = -1;
        public static int TRANS_AnyState_IdleToRun90L = -1;
        public static int TRANS_EntryState_IdleToRun90L = -1;
        public static int TRANS_AnyState_IdleToRun90R = -1;
        public static int TRANS_EntryState_IdleToRun90R = -1;
        public static int TRANS_AnyState_IdleToRun180R = -1;
        public static int TRANS_EntryState_IdleToRun180R = -1;
        public static int TRANS_AnyState_IdleToRun = -1;
        public static int TRANS_EntryState_IdleToRun = -1;
        public static int TRANS_AnyState_IdleTurn180L = -1;
        public static int TRANS_EntryState_IdleTurn180L = -1;
        public static int TRANS_AnyState_IdleTurn90L = -1;
        public static int TRANS_EntryState_IdleTurn90L = -1;
        public static int TRANS_AnyState_IdleTurn20L = -1;
        public static int TRANS_EntryState_IdleTurn20L = -1;
        public static int TRANS_AnyState_IdleTurn20R = -1;
        public static int TRANS_EntryState_IdleTurn20R = -1;
        public static int TRANS_AnyState_IdleTurn90R = -1;
        public static int TRANS_EntryState_IdleTurn90R = -1;
        public static int TRANS_AnyState_IdleTurn180R = -1;
        public static int TRANS_EntryState_IdleTurn180R = -1;
        public static int TRANS_MoveTree_RunPivot180R_LDown = -1;
        public static int TRANS_MoveTree_WalkPivot180L = -1;
        public static int TRANS_MoveTree_RunToIdle_LDown = -1;
        public static int TRANS_MoveTree_WalkToIdle_LDown = -1;
        public static int TRANS_MoveTree_RunToIdle_RDown = -1;
        public static int TRANS_MoveTree_WalkToIdle_RDown = -1;
        public static int TRANS_IdleToWalk90L_MoveTree = -1;
        public static int TRANS_IdleToWalk90L_IdlePose = -1;
        public static int TRANS_IdleToWalk90R_MoveTree = -1;
        public static int TRANS_IdleToWalk90R_IdlePose = -1;
        public static int TRANS_IdleToWalk180R_MoveTree = -1;
        public static int TRANS_IdleToWalk180R_IdlePose = -1;
        public static int TRANS_IdleToWalk180L_MoveTree = -1;
        public static int TRANS_IdleToWalk180L_IdlePose = -1;
        public static int TRANS_IdleToRun90L_MoveTree = -1;
        public static int TRANS_IdleToRun90L_IdlePose = -1;
        public static int TRANS_IdleToRun180L_MoveTree = -1;
        public static int TRANS_IdleToRun180L_IdlePose = -1;
        public static int TRANS_IdleToRun90R_MoveTree = -1;
        public static int TRANS_IdleToRun90R_IdlePose = -1;
        public static int TRANS_IdleToRun180R_MoveTree = -1;
        public static int TRANS_IdleToRun180R_IdlePose = -1;
        public static int TRANS_IdleToRun_MoveTree = -1;
        public static int TRANS_IdleToRun_IdlePose = -1;
        public static int TRANS_RunPivot180R_LDown_MoveTree = -1;
        public static int TRANS_WalkPivot180L_MoveTree = -1;
        public static int TRANS_RunToIdle_LDown_IdlePose = -1;
        public static int TRANS_RunToIdle_LDown_MoveTree = -1;
        public static int TRANS_WalkToIdle_LDown_MoveTree = -1;
        public static int TRANS_WalkToIdle_LDown_IdlePose = -1;
        public static int TRANS_WalkToIdle_RDown_MoveTree = -1;
        public static int TRANS_WalkToIdle_RDown_IdlePose = -1;
        public static int TRANS_RunToIdle_RDown_MoveTree = -1;
        public static int TRANS_RunToIdle_RDown_IdlePose = -1;
        public static int TRANS_IdleTurn20R_IdleTurnEndPose = -1;
        public static int TRANS_IdleTurn90R_IdleTurnEndPose = -1;
        public static int TRANS_IdleTurn180R_IdleTurnEndPose = -1;
        public static int TRANS_IdleTurn20L_IdleTurnEndPose = -1;
        public static int TRANS_IdleTurn90L_IdleTurnEndPose = -1;
        public static int TRANS_IdleTurn180L_IdleTurnEndPose = -1;
        public static int TRANS_IdleTurnEndPose_MoveTree = -1;

        /// <summary>
        /// Determines if we're using auto-generated code
        /// </summary>
        public override bool HasAutoGeneratedCode
        {
            get { return true; }
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsInMotionState
        {
            get
            {
                int lStateID = mMotionLayer._AnimatorStateID;
                int lTransitionID = mMotionLayer._AnimatorTransitionID;

                if (lTransitionID == 0)
                {
                    if (lStateID == STATE_MoveTree) { return true; }
                    if (lStateID == STATE_IdleToWalk90L) { return true; }
                    if (lStateID == STATE_IdleToWalk90R) { return true; }
                    if (lStateID == STATE_IdleToWalk180R) { return true; }
                    if (lStateID == STATE_IdleToWalk180L) { return true; }
                    if (lStateID == STATE_IdlePose) { return true; }
                    if (lStateID == STATE_IdleToRun90L) { return true; }
                    if (lStateID == STATE_IdleToRun180L) { return true; }
                    if (lStateID == STATE_IdleToRun90R) { return true; }
                    if (lStateID == STATE_IdleToRun180R) { return true; }
                    if (lStateID == STATE_IdleToRun) { return true; }
                    if (lStateID == STATE_RunPivot180R_LDown) { return true; }
                    if (lStateID == STATE_WalkPivot180L) { return true; }
                    if (lStateID == STATE_RunToIdle_LDown) { return true; }
                    if (lStateID == STATE_WalkToIdle_LDown) { return true; }
                    if (lStateID == STATE_WalkToIdle_RDown) { return true; }
                    if (lStateID == STATE_RunToIdle_RDown) { return true; }
                    if (lStateID == STATE_IdleTurn20R) { return true; }
                    if (lStateID == STATE_IdleTurn90R) { return true; }
                    if (lStateID == STATE_IdleTurn180R) { return true; }
                    if (lStateID == STATE_IdleTurn20L) { return true; }
                    if (lStateID == STATE_IdleTurn90L) { return true; }
                    if (lStateID == STATE_IdleTurn180L) { return true; }
                    if (lStateID == STATE_IdleTurnEndPose) { return true; }
                }

                if (lTransitionID == TRANS_AnyState_IdleToWalk90L) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToWalk90L) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToWalk90R) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToWalk90R) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToWalk180R) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToWalk180R) { return true; }
                if (lTransitionID == TRANS_AnyState_MoveTree) { return true; }
                if (lTransitionID == TRANS_EntryState_MoveTree) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToWalk180L) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToWalk180L) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToRun180L) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToRun180L) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToRun90L) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToRun90L) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToRun90R) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToRun90R) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToRun180R) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToRun180R) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToRun) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToRun) { return true; }
                if (lTransitionID == TRANS_AnyState_MoveTree) { return true; }
                if (lTransitionID == TRANS_EntryState_MoveTree) { return true; }
                if (lTransitionID == TRANS_AnyState_MoveTree) { return true; }
                if (lTransitionID == TRANS_EntryState_MoveTree) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleTurn180L) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleTurn180L) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleTurn90L) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleTurn90L) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleTurn20L) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleTurn20L) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleTurn20R) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleTurn20R) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleTurn90R) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleTurn90R) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleTurn180R) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleTurn180R) { return true; }
                if (lTransitionID == TRANS_MoveTree_RunPivot180R_LDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_RunPivot180R_LDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_WalkPivot180L) { return true; }
                if (lTransitionID == TRANS_MoveTree_WalkPivot180L) { return true; }
                if (lTransitionID == TRANS_MoveTree_RunToIdle_LDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_WalkToIdle_LDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_RunToIdle_RDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_WalkToIdle_RDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_RunToIdle_RDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_RunToIdle_LDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_WalkToIdle_RDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_WalkToIdle_LDown) { return true; }
                if (lTransitionID == TRANS_IdleToWalk90L_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToWalk90L_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToWalk90R_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToWalk90R_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToWalk180R_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToWalk180R_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToWalk180L_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToWalk180L_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToRun90L_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToRun90L_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToRun180L_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToRun180L_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToRun90R_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToRun90R_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToRun180R_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToRun180R_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToRun_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToRun_IdlePose) { return true; }
                if (lTransitionID == TRANS_RunPivot180R_LDown_MoveTree) { return true; }
                if (lTransitionID == TRANS_WalkPivot180L_MoveTree) { return true; }
                if (lTransitionID == TRANS_RunToIdle_LDown_IdlePose) { return true; }
                if (lTransitionID == TRANS_RunToIdle_LDown_MoveTree) { return true; }
                if (lTransitionID == TRANS_WalkToIdle_LDown_MoveTree) { return true; }
                if (lTransitionID == TRANS_WalkToIdle_LDown_IdlePose) { return true; }
                if (lTransitionID == TRANS_WalkToIdle_RDown_MoveTree) { return true; }
                if (lTransitionID == TRANS_WalkToIdle_RDown_IdlePose) { return true; }
                if (lTransitionID == TRANS_RunToIdle_RDown_MoveTree) { return true; }
                if (lTransitionID == TRANS_RunToIdle_RDown_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleTurn20R_IdleTurnEndPose) { return true; }
                if (lTransitionID == TRANS_IdleTurn90R_IdleTurnEndPose) { return true; }
                if (lTransitionID == TRANS_IdleTurn180R_IdleTurnEndPose) { return true; }
                if (lTransitionID == TRANS_IdleTurn20L_IdleTurnEndPose) { return true; }
                if (lTransitionID == TRANS_IdleTurn90L_IdleTurnEndPose) { return true; }
                if (lTransitionID == TRANS_IdleTurn180L_IdleTurnEndPose) { return true; }
                if (lTransitionID == TRANS_IdleTurnEndPose_MoveTree) { return true; }
                return false;
            }
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsMotionState(int rStateID)
        {
            if (rStateID == STATE_MoveTree) { return true; }
            if (rStateID == STATE_IdleToWalk90L) { return true; }
            if (rStateID == STATE_IdleToWalk90R) { return true; }
            if (rStateID == STATE_IdleToWalk180R) { return true; }
            if (rStateID == STATE_IdleToWalk180L) { return true; }
            if (rStateID == STATE_IdlePose) { return true; }
            if (rStateID == STATE_IdleToRun90L) { return true; }
            if (rStateID == STATE_IdleToRun180L) { return true; }
            if (rStateID == STATE_IdleToRun90R) { return true; }
            if (rStateID == STATE_IdleToRun180R) { return true; }
            if (rStateID == STATE_IdleToRun) { return true; }
            if (rStateID == STATE_RunPivot180R_LDown) { return true; }
            if (rStateID == STATE_WalkPivot180L) { return true; }
            if (rStateID == STATE_RunToIdle_LDown) { return true; }
            if (rStateID == STATE_WalkToIdle_LDown) { return true; }
            if (rStateID == STATE_WalkToIdle_RDown) { return true; }
            if (rStateID == STATE_RunToIdle_RDown) { return true; }
            if (rStateID == STATE_IdleTurn20R) { return true; }
            if (rStateID == STATE_IdleTurn90R) { return true; }
            if (rStateID == STATE_IdleTurn180R) { return true; }
            if (rStateID == STATE_IdleTurn20L) { return true; }
            if (rStateID == STATE_IdleTurn90L) { return true; }
            if (rStateID == STATE_IdleTurn180L) { return true; }
            if (rStateID == STATE_IdleTurnEndPose) { return true; }
            return false;
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsMotionState(int rStateID, int rTransitionID)
        {
            if (rTransitionID == 0)
            {
                if (rStateID == STATE_MoveTree) { return true; }
                if (rStateID == STATE_IdleToWalk90L) { return true; }
                if (rStateID == STATE_IdleToWalk90R) { return true; }
                if (rStateID == STATE_IdleToWalk180R) { return true; }
                if (rStateID == STATE_IdleToWalk180L) { return true; }
                if (rStateID == STATE_IdlePose) { return true; }
                if (rStateID == STATE_IdleToRun90L) { return true; }
                if (rStateID == STATE_IdleToRun180L) { return true; }
                if (rStateID == STATE_IdleToRun90R) { return true; }
                if (rStateID == STATE_IdleToRun180R) { return true; }
                if (rStateID == STATE_IdleToRun) { return true; }
                if (rStateID == STATE_RunPivot180R_LDown) { return true; }
                if (rStateID == STATE_WalkPivot180L) { return true; }
                if (rStateID == STATE_RunToIdle_LDown) { return true; }
                if (rStateID == STATE_WalkToIdle_LDown) { return true; }
                if (rStateID == STATE_WalkToIdle_RDown) { return true; }
                if (rStateID == STATE_RunToIdle_RDown) { return true; }
                if (rStateID == STATE_IdleTurn20R) { return true; }
                if (rStateID == STATE_IdleTurn90R) { return true; }
                if (rStateID == STATE_IdleTurn180R) { return true; }
                if (rStateID == STATE_IdleTurn20L) { return true; }
                if (rStateID == STATE_IdleTurn90L) { return true; }
                if (rStateID == STATE_IdleTurn180L) { return true; }
                if (rStateID == STATE_IdleTurnEndPose) { return true; }
            }

            if (rTransitionID == TRANS_AnyState_IdleToWalk90L) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToWalk90L) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToWalk90R) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToWalk90R) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToWalk180R) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToWalk180R) { return true; }
            if (rTransitionID == TRANS_AnyState_MoveTree) { return true; }
            if (rTransitionID == TRANS_EntryState_MoveTree) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToWalk180L) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToWalk180L) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToRun180L) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToRun180L) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToRun90L) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToRun90L) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToRun90R) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToRun90R) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToRun180R) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToRun180R) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToRun) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToRun) { return true; }
            if (rTransitionID == TRANS_AnyState_MoveTree) { return true; }
            if (rTransitionID == TRANS_EntryState_MoveTree) { return true; }
            if (rTransitionID == TRANS_AnyState_MoveTree) { return true; }
            if (rTransitionID == TRANS_EntryState_MoveTree) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleTurn180L) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleTurn180L) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleTurn90L) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleTurn90L) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleTurn20L) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleTurn20L) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleTurn20R) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleTurn20R) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleTurn90R) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleTurn90R) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleTurn180R) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleTurn180R) { return true; }
            if (rTransitionID == TRANS_MoveTree_RunPivot180R_LDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_RunPivot180R_LDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_WalkPivot180L) { return true; }
            if (rTransitionID == TRANS_MoveTree_WalkPivot180L) { return true; }
            if (rTransitionID == TRANS_MoveTree_RunToIdle_LDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_WalkToIdle_LDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_RunToIdle_RDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_WalkToIdle_RDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_RunToIdle_RDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_RunToIdle_LDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_WalkToIdle_RDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_WalkToIdle_LDown) { return true; }
            if (rTransitionID == TRANS_IdleToWalk90L_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToWalk90L_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToWalk90R_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToWalk90R_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToWalk180R_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToWalk180R_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToWalk180L_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToWalk180L_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToRun90L_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToRun90L_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToRun180L_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToRun180L_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToRun90R_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToRun90R_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToRun180R_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToRun180R_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToRun_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToRun_IdlePose) { return true; }
            if (rTransitionID == TRANS_RunPivot180R_LDown_MoveTree) { return true; }
            if (rTransitionID == TRANS_WalkPivot180L_MoveTree) { return true; }
            if (rTransitionID == TRANS_RunToIdle_LDown_IdlePose) { return true; }
            if (rTransitionID == TRANS_RunToIdle_LDown_MoveTree) { return true; }
            if (rTransitionID == TRANS_WalkToIdle_LDown_MoveTree) { return true; }
            if (rTransitionID == TRANS_WalkToIdle_LDown_IdlePose) { return true; }
            if (rTransitionID == TRANS_WalkToIdle_RDown_MoveTree) { return true; }
            if (rTransitionID == TRANS_WalkToIdle_RDown_IdlePose) { return true; }
            if (rTransitionID == TRANS_RunToIdle_RDown_MoveTree) { return true; }
            if (rTransitionID == TRANS_RunToIdle_RDown_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleTurn20R_IdleTurnEndPose) { return true; }
            if (rTransitionID == TRANS_IdleTurn90R_IdleTurnEndPose) { return true; }
            if (rTransitionID == TRANS_IdleTurn180R_IdleTurnEndPose) { return true; }
            if (rTransitionID == TRANS_IdleTurn20L_IdleTurnEndPose) { return true; }
            if (rTransitionID == TRANS_IdleTurn90L_IdleTurnEndPose) { return true; }
            if (rTransitionID == TRANS_IdleTurn180L_IdleTurnEndPose) { return true; }
            if (rTransitionID == TRANS_IdleTurnEndPose_MoveTree) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            TRANS_AnyState_IdleToWalk90L = mMotionController.AddAnimatorName("AnyState -> Base Layer.WalkRunPivot v2-SM.IdleToWalk90L");
            TRANS_EntryState_IdleToWalk90L = mMotionController.AddAnimatorName("Entry -> Base Layer.WalkRunPivot v2-SM.IdleToWalk90L");
            TRANS_AnyState_IdleToWalk90R = mMotionController.AddAnimatorName("AnyState -> Base Layer.WalkRunPivot v2-SM.IdleToWalk90R");
            TRANS_EntryState_IdleToWalk90R = mMotionController.AddAnimatorName("Entry -> Base Layer.WalkRunPivot v2-SM.IdleToWalk90R");
            TRANS_AnyState_IdleToWalk180R = mMotionController.AddAnimatorName("AnyState -> Base Layer.WalkRunPivot v2-SM.IdleToWalk180R");
            TRANS_EntryState_IdleToWalk180R = mMotionController.AddAnimatorName("Entry -> Base Layer.WalkRunPivot v2-SM.IdleToWalk180R");
            TRANS_AnyState_MoveTree = mMotionController.AddAnimatorName("AnyState -> Base Layer.WalkRunPivot v2-SM.Move Tree");
            TRANS_EntryState_MoveTree = mMotionController.AddAnimatorName("Entry -> Base Layer.WalkRunPivot v2-SM.Move Tree");
            TRANS_AnyState_IdleToWalk180L = mMotionController.AddAnimatorName("AnyState -> Base Layer.WalkRunPivot v2-SM.IdleToWalk180L");
            TRANS_EntryState_IdleToWalk180L = mMotionController.AddAnimatorName("Entry -> Base Layer.WalkRunPivot v2-SM.IdleToWalk180L");
            TRANS_AnyState_IdleToRun180L = mMotionController.AddAnimatorName("AnyState -> Base Layer.WalkRunPivot v2-SM.IdleToRun180L");
            TRANS_EntryState_IdleToRun180L = mMotionController.AddAnimatorName("Entry -> Base Layer.WalkRunPivot v2-SM.IdleToRun180L");
            TRANS_AnyState_IdleToRun90L = mMotionController.AddAnimatorName("AnyState -> Base Layer.WalkRunPivot v2-SM.IdleToRun90L");
            TRANS_EntryState_IdleToRun90L = mMotionController.AddAnimatorName("Entry -> Base Layer.WalkRunPivot v2-SM.IdleToRun90L");
            TRANS_AnyState_IdleToRun90R = mMotionController.AddAnimatorName("AnyState -> Base Layer.WalkRunPivot v2-SM.IdleToRun90R");
            TRANS_EntryState_IdleToRun90R = mMotionController.AddAnimatorName("Entry -> Base Layer.WalkRunPivot v2-SM.IdleToRun90R");
            TRANS_AnyState_IdleToRun180R = mMotionController.AddAnimatorName("AnyState -> Base Layer.WalkRunPivot v2-SM.IdleToRun180R");
            TRANS_EntryState_IdleToRun180R = mMotionController.AddAnimatorName("Entry -> Base Layer.WalkRunPivot v2-SM.IdleToRun180R");
            TRANS_AnyState_IdleToRun = mMotionController.AddAnimatorName("AnyState -> Base Layer.WalkRunPivot v2-SM.IdleToRun");
            TRANS_EntryState_IdleToRun = mMotionController.AddAnimatorName("Entry -> Base Layer.WalkRunPivot v2-SM.IdleToRun");
            TRANS_AnyState_MoveTree = mMotionController.AddAnimatorName("AnyState -> Base Layer.WalkRunPivot v2-SM.Move Tree");
            TRANS_EntryState_MoveTree = mMotionController.AddAnimatorName("Entry -> Base Layer.WalkRunPivot v2-SM.Move Tree");
            TRANS_AnyState_MoveTree = mMotionController.AddAnimatorName("AnyState -> Base Layer.WalkRunPivot v2-SM.Move Tree");
            TRANS_EntryState_MoveTree = mMotionController.AddAnimatorName("Entry -> Base Layer.WalkRunPivot v2-SM.Move Tree");
            TRANS_AnyState_IdleTurn180L = mMotionController.AddAnimatorName("AnyState -> Base Layer.WalkRunPivot v2-SM.IdleTurn180L");
            TRANS_EntryState_IdleTurn180L = mMotionController.AddAnimatorName("Entry -> Base Layer.WalkRunPivot v2-SM.IdleTurn180L");
            TRANS_AnyState_IdleTurn90L = mMotionController.AddAnimatorName("AnyState -> Base Layer.WalkRunPivot v2-SM.IdleTurn90L");
            TRANS_EntryState_IdleTurn90L = mMotionController.AddAnimatorName("Entry -> Base Layer.WalkRunPivot v2-SM.IdleTurn90L");
            TRANS_AnyState_IdleTurn20L = mMotionController.AddAnimatorName("AnyState -> Base Layer.WalkRunPivot v2-SM.IdleTurn20L");
            TRANS_EntryState_IdleTurn20L = mMotionController.AddAnimatorName("Entry -> Base Layer.WalkRunPivot v2-SM.IdleTurn20L");
            TRANS_AnyState_IdleTurn20R = mMotionController.AddAnimatorName("AnyState -> Base Layer.WalkRunPivot v2-SM.IdleTurn20R");
            TRANS_EntryState_IdleTurn20R = mMotionController.AddAnimatorName("Entry -> Base Layer.WalkRunPivot v2-SM.IdleTurn20R");
            TRANS_AnyState_IdleTurn90R = mMotionController.AddAnimatorName("AnyState -> Base Layer.WalkRunPivot v2-SM.IdleTurn90R");
            TRANS_EntryState_IdleTurn90R = mMotionController.AddAnimatorName("Entry -> Base Layer.WalkRunPivot v2-SM.IdleTurn90R");
            TRANS_AnyState_IdleTurn180R = mMotionController.AddAnimatorName("AnyState -> Base Layer.WalkRunPivot v2-SM.IdleTurn180R");
            TRANS_EntryState_IdleTurn180R = mMotionController.AddAnimatorName("Entry -> Base Layer.WalkRunPivot v2-SM.IdleTurn180R");
            STATE_MoveTree = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.Move Tree");
            TRANS_MoveTree_RunPivot180R_LDown = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.Move Tree -> Base Layer.WalkRunPivot v2-SM.RunPivot180R_LDown");
            TRANS_MoveTree_RunPivot180R_LDown = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.Move Tree -> Base Layer.WalkRunPivot v2-SM.RunPivot180R_LDown");
            TRANS_MoveTree_WalkPivot180L = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.Move Tree -> Base Layer.WalkRunPivot v2-SM.WalkPivot180L");
            TRANS_MoveTree_WalkPivot180L = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.Move Tree -> Base Layer.WalkRunPivot v2-SM.WalkPivot180L");
            TRANS_MoveTree_RunToIdle_LDown = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.Move Tree -> Base Layer.WalkRunPivot v2-SM.RunToIdle_LDown");
            TRANS_MoveTree_WalkToIdle_LDown = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.Move Tree -> Base Layer.WalkRunPivot v2-SM.WalkToIdle_LDown");
            TRANS_MoveTree_RunToIdle_RDown = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.Move Tree -> Base Layer.WalkRunPivot v2-SM.RunToIdle_RDown");
            TRANS_MoveTree_WalkToIdle_RDown = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.Move Tree -> Base Layer.WalkRunPivot v2-SM.WalkToIdle_RDown");
            TRANS_MoveTree_RunToIdle_RDown = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.Move Tree -> Base Layer.WalkRunPivot v2-SM.RunToIdle_RDown");
            TRANS_MoveTree_RunToIdle_LDown = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.Move Tree -> Base Layer.WalkRunPivot v2-SM.RunToIdle_LDown");
            TRANS_MoveTree_WalkToIdle_RDown = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.Move Tree -> Base Layer.WalkRunPivot v2-SM.WalkToIdle_RDown");
            TRANS_MoveTree_WalkToIdle_LDown = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.Move Tree -> Base Layer.WalkRunPivot v2-SM.WalkToIdle_LDown");
            STATE_IdleToWalk90L = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToWalk90L");
            TRANS_IdleToWalk90L_MoveTree = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToWalk90L -> Base Layer.WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToWalk90L_IdlePose = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToWalk90L -> Base Layer.WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToWalk90R = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToWalk90R");
            TRANS_IdleToWalk90R_MoveTree = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToWalk90R -> Base Layer.WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToWalk90R_IdlePose = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToWalk90R -> Base Layer.WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToWalk180R = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToWalk180R");
            TRANS_IdleToWalk180R_MoveTree = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToWalk180R -> Base Layer.WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToWalk180R_IdlePose = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToWalk180R -> Base Layer.WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToWalk180L = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToWalk180L");
            TRANS_IdleToWalk180L_MoveTree = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToWalk180L -> Base Layer.WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToWalk180L_IdlePose = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToWalk180L -> Base Layer.WalkRunPivot v2-SM.IdlePose");
            STATE_IdlePose = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToRun90L = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToRun90L");
            TRANS_IdleToRun90L_MoveTree = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToRun90L -> Base Layer.WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToRun90L_IdlePose = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToRun90L -> Base Layer.WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToRun180L = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToRun180L");
            TRANS_IdleToRun180L_MoveTree = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToRun180L -> Base Layer.WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToRun180L_IdlePose = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToRun180L -> Base Layer.WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToRun90R = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToRun90R");
            TRANS_IdleToRun90R_MoveTree = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToRun90R -> Base Layer.WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToRun90R_IdlePose = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToRun90R -> Base Layer.WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToRun180R = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToRun180R");
            TRANS_IdleToRun180R_MoveTree = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToRun180R -> Base Layer.WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToRun180R_IdlePose = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToRun180R -> Base Layer.WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToRun = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToRun");
            TRANS_IdleToRun_MoveTree = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToRun -> Base Layer.WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToRun_IdlePose = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleToRun -> Base Layer.WalkRunPivot v2-SM.IdlePose");
            STATE_RunPivot180R_LDown = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.RunPivot180R_LDown");
            TRANS_RunPivot180R_LDown_MoveTree = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.RunPivot180R_LDown -> Base Layer.WalkRunPivot v2-SM.Move Tree");
            STATE_WalkPivot180L = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.WalkPivot180L");
            TRANS_WalkPivot180L_MoveTree = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.WalkPivot180L -> Base Layer.WalkRunPivot v2-SM.Move Tree");
            STATE_RunToIdle_LDown = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.RunToIdle_LDown");
            TRANS_RunToIdle_LDown_IdlePose = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.RunToIdle_LDown -> Base Layer.WalkRunPivot v2-SM.IdlePose");
            TRANS_RunToIdle_LDown_MoveTree = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.RunToIdle_LDown -> Base Layer.WalkRunPivot v2-SM.Move Tree");
            STATE_WalkToIdle_LDown = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.WalkToIdle_LDown");
            TRANS_WalkToIdle_LDown_MoveTree = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.WalkToIdle_LDown -> Base Layer.WalkRunPivot v2-SM.Move Tree");
            TRANS_WalkToIdle_LDown_IdlePose = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.WalkToIdle_LDown -> Base Layer.WalkRunPivot v2-SM.IdlePose");
            STATE_WalkToIdle_RDown = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.WalkToIdle_RDown");
            TRANS_WalkToIdle_RDown_MoveTree = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.WalkToIdle_RDown -> Base Layer.WalkRunPivot v2-SM.Move Tree");
            TRANS_WalkToIdle_RDown_IdlePose = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.WalkToIdle_RDown -> Base Layer.WalkRunPivot v2-SM.IdlePose");
            STATE_RunToIdle_RDown = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.RunToIdle_RDown");
            TRANS_RunToIdle_RDown_MoveTree = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.RunToIdle_RDown -> Base Layer.WalkRunPivot v2-SM.Move Tree");
            TRANS_RunToIdle_RDown_IdlePose = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.RunToIdle_RDown -> Base Layer.WalkRunPivot v2-SM.IdlePose");
            STATE_IdleTurn20R = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleTurn20R");
            TRANS_IdleTurn20R_IdleTurnEndPose = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleTurn20R -> Base Layer.WalkRunPivot v2-SM.IdleTurnEndPose");
            STATE_IdleTurn90R = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleTurn90R");
            TRANS_IdleTurn90R_IdleTurnEndPose = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleTurn90R -> Base Layer.WalkRunPivot v2-SM.IdleTurnEndPose");
            STATE_IdleTurn180R = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleTurn180R");
            TRANS_IdleTurn180R_IdleTurnEndPose = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleTurn180R -> Base Layer.WalkRunPivot v2-SM.IdleTurnEndPose");
            STATE_IdleTurn20L = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleTurn20L");
            TRANS_IdleTurn20L_IdleTurnEndPose = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleTurn20L -> Base Layer.WalkRunPivot v2-SM.IdleTurnEndPose");
            STATE_IdleTurn90L = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleTurn90L");
            TRANS_IdleTurn90L_IdleTurnEndPose = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleTurn90L -> Base Layer.WalkRunPivot v2-SM.IdleTurnEndPose");
            STATE_IdleTurn180L = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleTurn180L");
            TRANS_IdleTurn180L_IdleTurnEndPose = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleTurn180L -> Base Layer.WalkRunPivot v2-SM.IdleTurnEndPose");
            STATE_IdleTurnEndPose = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleTurnEndPose");
            TRANS_IdleTurnEndPose_MoveTree = mMotionController.AddAnimatorName("Base Layer.WalkRunPivot v2-SM.IdleTurnEndPose -> Base Layer.WalkRunPivot v2-SM.Move Tree");
        }

#if UNITY_EDITOR

        private AnimationClip m14306 = null;
        private AnimationClip m19088 = null;
        private AnimationClip m14266 = null;
        private AnimationClip m21162 = null;
        private AnimationClip m21164 = null;
        private AnimationClip m21168 = null;
        private AnimationClip m21166 = null;
        private AnimationClip m18166 = null;
        private AnimationClip m18170 = null;
        private AnimationClip m18168 = null;
        private AnimationClip m18172 = null;
        private AnimationClip m14264 = null;
        private AnimationClip m18032 = null;
        private AnimationClip m21172 = null;
        private AnimationClip m17466 = null;
        private AnimationClip m21176 = null;
        private AnimationClip m21178 = null;
        private AnimationClip m19758 = null;
        private AnimationClip m14316 = null;
        private AnimationClip m14320 = null;
        private AnimationClip m14314 = null;
        private AnimationClip m14318 = null;

        /// <summary>
        /// Creates the animator substate machine for this motion.
        /// </summary>
        protected override void CreateStateMachine()
        {
            // Grab the root sm for the layer
            UnityEditor.Animations.AnimatorStateMachine lRootStateMachine = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
            UnityEditor.Animations.AnimatorStateMachine lSM_21500 = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
            UnityEditor.Animations.AnimatorStateMachine lRootSubStateMachine = null;

            // If we find the sm with our name, remove it
            for (int i = 0; i < lRootStateMachine.stateMachines.Length; i++)
            {
                // Look for a sm with the matching name
                if (lRootStateMachine.stateMachines[i].stateMachine.name == _EditorAnimatorSMName)
                {
                    lRootSubStateMachine = lRootStateMachine.stateMachines[i].stateMachine;

                    // Allow the user to stop before we remove the sm
                    if (!UnityEditor.EditorUtility.DisplayDialog("Motion Controller", _EditorAnimatorSMName + " already exists. Delete and recreate it?", "Yes", "No"))
                    {
                        return;
                    }

                    // Remove the sm
                    //lRootStateMachine.RemoveStateMachine(lRootStateMachine.stateMachines[i].stateMachine);
                    break;
                }
            }

            UnityEditor.Animations.AnimatorStateMachine lSM_21552 = lRootSubStateMachine;
            if (lSM_21552 != null)
            {
                for (int i = lSM_21552.entryTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_21552.RemoveEntryTransition(lSM_21552.entryTransitions[i]);
                }

                for (int i = lSM_21552.anyStateTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_21552.RemoveAnyStateTransition(lSM_21552.anyStateTransitions[i]);
                }

                for (int i = lSM_21552.states.Length - 1; i >= 0; i--)
                {
                    lSM_21552.RemoveState(lSM_21552.states[i].state);
                }

                for (int i = lSM_21552.stateMachines.Length - 1; i >= 0; i--)
                {
                    lSM_21552.RemoveStateMachine(lSM_21552.stateMachines[i].stateMachine);
                }
            }
            else
            {
                lSM_21552 = lSM_21500.AddStateMachine(_EditorAnimatorSMName, new Vector3(624, -756, 0));
            }

            UnityEditor.Animations.AnimatorState lS_21808 = lSM_21552.AddState("Move Tree", new Vector3(240, 372, 0));
            lS_21808.speed = 1f;

            UnityEditor.Animations.BlendTree lM_15644 = CreateBlendTree("Move Blend Tree", _EditorAnimatorController, mMotionLayer.AnimatorLayerIndex);
            lM_15644.blendType = UnityEditor.Animations.BlendTreeType.Simple1D;
            lM_15644.blendParameter = "InputMagnitude";
            lM_15644.blendParameterY = "InputX";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_15644.useAutomaticThresholds = false;
#endif
            lM_15644.AddChild(m14306, 0f);
            lM_15644.AddChild(m19088, 0.5f);
            lM_15644.AddChild(m14266, 1f);
            lS_21808.motion = lM_15644;

            UnityEditor.Animations.AnimatorState lS_21802 = lSM_21552.AddState("IdleToWalk90L", new Vector3(-180, 204, 0));
            lS_21802.speed = 1.3f;
            lS_21802.motion = m21162;

            UnityEditor.Animations.AnimatorState lS_21804 = lSM_21552.AddState("IdleToWalk90R", new Vector3(-180, 264, 0));
            lS_21804.speed = 1.3f;
            lS_21804.motion = m21164;

            UnityEditor.Animations.AnimatorState lS_21806 = lSM_21552.AddState("IdleToWalk180R", new Vector3(-180, 324, 0));
            lS_21806.speed = 1.3f;
            lS_21806.motion = m21168;

            UnityEditor.Animations.AnimatorState lS_21810 = lSM_21552.AddState("IdleToWalk180L", new Vector3(-180, 144, 0));
            lS_21810.speed = 1.3f;
            lS_21810.motion = m21166;

            UnityEditor.Animations.AnimatorState lS_22668 = lSM_21552.AddState("IdlePose", new Vector3(132, 216, 0));
            lS_22668.speed = 1f;
            lS_22668.motion = m14306;

            UnityEditor.Animations.AnimatorState lS_21814 = lSM_21552.AddState("IdleToRun90L", new Vector3(-168, 492, 0));
            lS_21814.speed = 1.5f;
            lS_21814.motion = m18166;

            UnityEditor.Animations.AnimatorState lS_21812 = lSM_21552.AddState("IdleToRun180L", new Vector3(-168, 432, 0));
            lS_21812.speed = 1.3f;
            lS_21812.motion = m18170;

            UnityEditor.Animations.AnimatorState lS_21816 = lSM_21552.AddState("IdleToRun90R", new Vector3(-168, 612, 0));
            lS_21816.speed = 1.5f;
            lS_21816.motion = m18168;

            UnityEditor.Animations.AnimatorState lS_21818 = lSM_21552.AddState("IdleToRun180R", new Vector3(-168, 672, 0));
            lS_21818.speed = 1.3f;
            lS_21818.motion = m18172;

            UnityEditor.Animations.AnimatorState lS_21820 = lSM_21552.AddState("IdleToRun", new Vector3(-168, 552, 0));
            lS_21820.speed = 2f;
            lS_21820.motion = m14264;

            UnityEditor.Animations.AnimatorState lS_22670 = lSM_21552.AddState("RunPivot180R_LDown", new Vector3(144, 564, 0));
            lS_22670.speed = 1.2f;
            lS_22670.motion = m18032;

            UnityEditor.Animations.AnimatorState lS_22672 = lSM_21552.AddState("WalkPivot180L", new Vector3(360, 564, 0));
            lS_22672.speed = 1.5f;
            lS_22672.motion = m21172;

            UnityEditor.Animations.AnimatorState lS_22674 = lSM_21552.AddState("RunToIdle_LDown", new Vector3(576, 336, 0));
            lS_22674.speed = 1f;
            lS_22674.motion = m17466;

            UnityEditor.Animations.AnimatorState lS_22676 = lSM_21552.AddState("WalkToIdle_LDown", new Vector3(576, 492, 0));
            lS_22676.speed = 1f;
            lS_22676.motion = m21176;

            UnityEditor.Animations.AnimatorState lS_22678 = lSM_21552.AddState("WalkToIdle_RDown", new Vector3(576, 420, 0));
            lS_22678.speed = 1f;
            lS_22678.motion = m21178;

            UnityEditor.Animations.AnimatorState lS_22680 = lSM_21552.AddState("RunToIdle_RDown", new Vector3(576, 264, 0));
            lS_22680.speed = 1f;
            lS_22680.motion = m19758;

            UnityEditor.Animations.AnimatorState lS_21828 = lSM_21552.AddState("IdleTurn20R", new Vector3(-720, 408, 0));
            lS_21828.speed = 1f;
            lS_21828.motion = m14316;

            UnityEditor.Animations.AnimatorState lS_21830 = lSM_21552.AddState("IdleTurn90R", new Vector3(-720, 468, 0));
            lS_21830.speed = 1.6f;
            lS_21830.motion = m14316;

            UnityEditor.Animations.AnimatorState lS_21832 = lSM_21552.AddState("IdleTurn180R", new Vector3(-720, 528, 0));
            lS_21832.speed = 1.4f;
            lS_21832.motion = m14320;

            UnityEditor.Animations.AnimatorState lS_21826 = lSM_21552.AddState("IdleTurn20L", new Vector3(-720, 348, 0));
            lS_21826.speed = 1f;
            lS_21826.motion = m14314;

            UnityEditor.Animations.AnimatorState lS_21824 = lSM_21552.AddState("IdleTurn90L", new Vector3(-720, 288, 0));
            lS_21824.speed = 1.6f;
            lS_21824.motion = m14314;

            UnityEditor.Animations.AnimatorState lS_21822 = lSM_21552.AddState("IdleTurn180L", new Vector3(-720, 228, 0));
            lS_21822.speed = 1.4f;
            lS_21822.motion = m14318;

            UnityEditor.Animations.AnimatorState lS_22682 = lSM_21552.AddState("IdleTurnEndPose", new Vector3(-984, 372, 0));
            lS_22682.speed = 1f;
            lS_22682.motion = m14306;

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_21660 = lRootStateMachine.AddAnyStateTransition(lS_21802);
            lT_21660.hasExitTime = false;
            lT_21660.hasFixedDuration = true;
            lT_21660.exitTime = 0.9f;
            lT_21660.duration = 0.1f;
            lT_21660.offset = 0f;
            lT_21660.mute = false;
            lT_21660.solo = false;
            lT_21660.canTransitionToSelf = true;
            lT_21660.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_21660.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L0MotionPhase");
            lT_21660.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");
            lT_21660.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -20f, "InputAngleFromAvatar");
            lT_21660.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -160f, "InputAngleFromAvatar");
            lT_21660.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_21662 = lRootStateMachine.AddAnyStateTransition(lS_21804);
            lT_21662.hasExitTime = false;
            lT_21662.hasFixedDuration = true;
            lT_21662.exitTime = 0.9f;
            lT_21662.duration = 0.1f;
            lT_21662.offset = 0f;
            lT_21662.mute = false;
            lT_21662.solo = false;
            lT_21662.canTransitionToSelf = true;
            lT_21662.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_21662.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L0MotionPhase");
            lT_21662.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");
            lT_21662.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 20f, "InputAngleFromAvatar");
            lT_21662.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 160f, "InputAngleFromAvatar");
            lT_21662.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_21664 = lRootStateMachine.AddAnyStateTransition(lS_21806);
            lT_21664.hasExitTime = false;
            lT_21664.hasFixedDuration = true;
            lT_21664.exitTime = 0.9f;
            lT_21664.duration = 0.1f;
            lT_21664.offset = 0f;
            lT_21664.mute = false;
            lT_21664.solo = false;
            lT_21664.canTransitionToSelf = true;
            lT_21664.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_21664.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L0MotionPhase");
            lT_21664.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");
            lT_21664.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 160f, "InputAngleFromAvatar");
            lT_21664.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_21666 = lRootStateMachine.AddAnyStateTransition(lS_21808);
            lT_21666.hasExitTime = false;
            lT_21666.hasFixedDuration = true;
            lT_21666.exitTime = 0.9f;
            lT_21666.duration = 0.1f;
            lT_21666.offset = 0f;
            lT_21666.mute = false;
            lT_21666.solo = false;
            lT_21666.canTransitionToSelf = true;
            lT_21666.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_21666.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L0MotionPhase");
            lT_21666.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -20f, "InputAngleFromAvatar");
            lT_21666.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 20f, "InputAngleFromAvatar");
            lT_21666.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_21668 = lRootStateMachine.AddAnyStateTransition(lS_21810);
            lT_21668.hasExitTime = false;
            lT_21668.hasFixedDuration = true;
            lT_21668.exitTime = 0.9f;
            lT_21668.duration = 0.1f;
            lT_21668.offset = 0f;
            lT_21668.mute = false;
            lT_21668.solo = false;
            lT_21668.canTransitionToSelf = true;
            lT_21668.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_21668.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L0MotionPhase");
            lT_21668.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");
            lT_21668.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -160f, "InputAngleFromAvatar");
            lT_21668.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_21670 = lRootStateMachine.AddAnyStateTransition(lS_21812);
            lT_21670.hasExitTime = false;
            lT_21670.hasFixedDuration = true;
            lT_21670.exitTime = 0.9f;
            lT_21670.duration = 0.1f;
            lT_21670.offset = 0f;
            lT_21670.mute = false;
            lT_21670.solo = false;
            lT_21670.canTransitionToSelf = true;
            lT_21670.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_21670.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L0MotionPhase");
            lT_21670.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");
            lT_21670.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -160f, "InputAngleFromAvatar");
            lT_21670.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_21672 = lRootStateMachine.AddAnyStateTransition(lS_21814);
            lT_21672.hasExitTime = false;
            lT_21672.hasFixedDuration = true;
            lT_21672.exitTime = 0.9f;
            lT_21672.duration = 0.1f;
            lT_21672.offset = 0f;
            lT_21672.mute = false;
            lT_21672.solo = false;
            lT_21672.canTransitionToSelf = true;
            lT_21672.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_21672.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L0MotionPhase");
            lT_21672.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");
            lT_21672.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -20f, "InputAngleFromAvatar");
            lT_21672.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -160f, "InputAngleFromAvatar");
            lT_21672.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_21674 = lRootStateMachine.AddAnyStateTransition(lS_21816);
            lT_21674.hasExitTime = false;
            lT_21674.hasFixedDuration = true;
            lT_21674.exitTime = 0.9f;
            lT_21674.duration = 0.1f;
            lT_21674.offset = 0f;
            lT_21674.mute = false;
            lT_21674.solo = false;
            lT_21674.canTransitionToSelf = true;
            lT_21674.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_21674.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L0MotionPhase");
            lT_21674.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");
            lT_21674.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 20f, "InputAngleFromAvatar");
            lT_21674.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 160f, "InputAngleFromAvatar");
            lT_21674.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_21676 = lRootStateMachine.AddAnyStateTransition(lS_21818);
            lT_21676.hasExitTime = false;
            lT_21676.hasFixedDuration = true;
            lT_21676.exitTime = 0.9f;
            lT_21676.duration = 0.1f;
            lT_21676.offset = 0f;
            lT_21676.mute = false;
            lT_21676.solo = false;
            lT_21676.canTransitionToSelf = true;
            lT_21676.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_21676.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L0MotionPhase");
            lT_21676.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");
            lT_21676.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 160f, "InputAngleFromAvatar");
            lT_21676.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_21678 = lRootStateMachine.AddAnyStateTransition(lS_21820);
            lT_21678.hasExitTime = false;
            lT_21678.hasFixedDuration = true;
            lT_21678.exitTime = 0.9f;
            lT_21678.duration = 0.1f;
            lT_21678.offset = 0f;
            lT_21678.mute = false;
            lT_21678.solo = false;
            lT_21678.canTransitionToSelf = true;
            lT_21678.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_21678.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L0MotionPhase");
            lT_21678.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");
            lT_21678.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -20f, "InputAngleFromAvatar");
            lT_21678.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 20f, "InputAngleFromAvatar");
            lT_21678.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_21680 = lRootStateMachine.AddAnyStateTransition(lS_21808);
            lT_21680.hasExitTime = false;
            lT_21680.hasFixedDuration = true;
            lT_21680.exitTime = 0.9f;
            lT_21680.duration = 0.1f;
            lT_21680.offset = 0.5f;
            lT_21680.mute = false;
            lT_21680.solo = false;
            lT_21680.canTransitionToSelf = true;
            lT_21680.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_21680.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L0MotionPhase");
            lT_21680.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 2f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_21682 = lRootStateMachine.AddAnyStateTransition(lS_21808);
            lT_21682.hasExitTime = false;
            lT_21682.hasFixedDuration = true;
            lT_21682.exitTime = 0.9f;
            lT_21682.duration = 0.1f;
            lT_21682.offset = 0f;
            lT_21682.mute = false;
            lT_21682.solo = false;
            lT_21682.canTransitionToSelf = true;
            lT_21682.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_21682.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L0MotionPhase");
            lT_21682.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_21684 = lRootStateMachine.AddAnyStateTransition(lS_21822);
            lT_21684.hasExitTime = false;
            lT_21684.hasFixedDuration = true;
            lT_21684.exitTime = 0.9f;
            lT_21684.duration = 0.05f;
            lT_21684.offset = 0.2228713f;
            lT_21684.mute = false;
            lT_21684.solo = false;
            lT_21684.canTransitionToSelf = true;
            lT_21684.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_21684.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27135f, "L0MotionPhase");
            lT_21684.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -135f, "InputAngleFromAvatar");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_21686 = lRootStateMachine.AddAnyStateTransition(lS_21824);
            lT_21686.hasExitTime = false;
            lT_21686.hasFixedDuration = true;
            lT_21686.exitTime = 0.9f;
            lT_21686.duration = 0.05f;
            lT_21686.offset = 0.1442637f;
            lT_21686.mute = false;
            lT_21686.solo = false;
            lT_21686.canTransitionToSelf = true;
            lT_21686.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_21686.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27135f, "L0MotionPhase");
            lT_21686.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -45f, "InputAngleFromAvatar");
            lT_21686.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -135f, "InputAngleFromAvatar");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_21688 = lRootStateMachine.AddAnyStateTransition(lS_21826);
            lT_21688.hasExitTime = false;
            lT_21688.hasFixedDuration = true;
            lT_21688.exitTime = 0.9f;
            lT_21688.duration = 0.05f;
            lT_21688.offset = 0.1442637f;
            lT_21688.mute = false;
            lT_21688.solo = false;
            lT_21688.canTransitionToSelf = true;
            lT_21688.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_21688.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27135f, "L0MotionPhase");
            lT_21688.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0f, "InputAngleFromAvatar");
            lT_21688.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -45f, "InputAngleFromAvatar");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_21690 = lRootStateMachine.AddAnyStateTransition(lS_21828);
            lT_21690.hasExitTime = false;
            lT_21690.hasFixedDuration = true;
            lT_21690.exitTime = 0.9f;
            lT_21690.duration = 0.05f;
            lT_21690.offset = 0.2277291f;
            lT_21690.mute = false;
            lT_21690.solo = false;
            lT_21690.canTransitionToSelf = true;
            lT_21690.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_21690.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27135f, "L0MotionPhase");
            lT_21690.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0f, "InputAngleFromAvatar");
            lT_21690.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 45f, "InputAngleFromAvatar");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_21692 = lRootStateMachine.AddAnyStateTransition(lS_21830);
            lT_21692.hasExitTime = false;
            lT_21692.hasFixedDuration = true;
            lT_21692.exitTime = 0.8999999f;
            lT_21692.duration = 0.05000001f;
            lT_21692.offset = 0.2277291f;
            lT_21692.mute = false;
            lT_21692.solo = false;
            lT_21692.canTransitionToSelf = true;
            lT_21692.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_21692.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27135f, "L0MotionPhase");
            lT_21692.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 45f, "InputAngleFromAvatar");
            lT_21692.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 135f, "InputAngleFromAvatar");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_21694 = lRootStateMachine.AddAnyStateTransition(lS_21832);
            lT_21694.hasExitTime = false;
            lT_21694.hasFixedDuration = true;
            lT_21694.exitTime = 0.9f;
            lT_21694.duration = 0.05f;
            lT_21694.offset = 0.2689505f;
            lT_21694.mute = false;
            lT_21694.solo = false;
            lT_21694.canTransitionToSelf = true;
            lT_21694.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_21694.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27135f, "L0MotionPhase");
            lT_21694.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 135f, "InputAngleFromAvatar");

            UnityEditor.Animations.AnimatorStateTransition lT_22684 = lS_21808.AddTransition(lS_22670);
            lT_22684.hasExitTime = false;
            lT_22684.hasFixedDuration = true;
            lT_22684.exitTime = 0.5481927f;
            lT_22684.duration = 0.1f;
            lT_22684.offset = 0f;
            lT_22684.mute = false;
            lT_22684.solo = false;
            lT_22684.canTransitionToSelf = true;
            lT_22684.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22684.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 160f, "InputAngleFromAvatar");
            lT_22684.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_22686 = lS_21808.AddTransition(lS_22670);
            lT_22686.hasExitTime = false;
            lT_22686.hasFixedDuration = true;
            lT_22686.exitTime = 0.5481927f;
            lT_22686.duration = 0.1f;
            lT_22686.offset = 0f;
            lT_22686.mute = false;
            lT_22686.solo = false;
            lT_22686.canTransitionToSelf = true;
            lT_22686.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22686.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -160f, "InputAngleFromAvatar");
            lT_22686.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_22688 = lS_21808.AddTransition(lS_22672);
            lT_22688.hasExitTime = false;
            lT_22688.hasFixedDuration = true;
            lT_22688.exitTime = 0.5481927f;
            lT_22688.duration = 0.1f;
            lT_22688.offset = 0f;
            lT_22688.mute = false;
            lT_22688.solo = false;
            lT_22688.canTransitionToSelf = true;
            lT_22688.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22688.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 160f, "InputAngleFromAvatar");
            lT_22688.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.2f, "InputMagnitude");
            lT_22688.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_22690 = lS_21808.AddTransition(lS_22672);
            lT_22690.hasExitTime = false;
            lT_22690.hasFixedDuration = true;
            lT_22690.exitTime = 0.5481927f;
            lT_22690.duration = 0.1f;
            lT_22690.offset = 0f;
            lT_22690.mute = false;
            lT_22690.solo = false;
            lT_22690.canTransitionToSelf = true;
            lT_22690.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22690.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -160f, "InputAngleFromAvatar");
            lT_22690.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.2f, "InputMagnitude");
            lT_22690.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_22692 = lS_21808.AddTransition(lS_22674);
            lT_22692.hasExitTime = true;
            lT_22692.hasFixedDuration = true;
            lT_22692.exitTime = 0.5f;
            lT_22692.duration = 0.2f;
            lT_22692.offset = 0.3595567f;
            lT_22692.mute = false;
            lT_22692.solo = false;
            lT_22692.canTransitionToSelf = true;
            lT_22692.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22692.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27131f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_22694 = lS_21808.AddTransition(lS_22676);
            lT_22694.hasExitTime = true;
            lT_22694.hasFixedDuration = true;
            lT_22694.exitTime = 0.5f;
            lT_22694.duration = 0.2f;
            lT_22694.offset = 0.5352634f;
            lT_22694.mute = false;
            lT_22694.solo = false;
            lT_22694.canTransitionToSelf = true;
            lT_22694.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22694.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27132f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_22696 = lS_21808.AddTransition(lS_22680);
            lT_22696.hasExitTime = true;
            lT_22696.hasFixedDuration = true;
            lT_22696.exitTime = 1f;
            lT_22696.duration = 0.2f;
            lT_22696.offset = 0f;
            lT_22696.mute = false;
            lT_22696.solo = false;
            lT_22696.canTransitionToSelf = true;
            lT_22696.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22696.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27131f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_22698 = lS_21808.AddTransition(lS_22678);
            lT_22698.hasExitTime = true;
            lT_22698.hasFixedDuration = true;
            lT_22698.exitTime = 1f;
            lT_22698.duration = 0.2f;
            lT_22698.offset = 0.4974566f;
            lT_22698.mute = false;
            lT_22698.solo = false;
            lT_22698.canTransitionToSelf = true;
            lT_22698.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22698.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27132f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_22700 = lS_21808.AddTransition(lS_22680);
            lT_22700.hasExitTime = true;
            lT_22700.hasFixedDuration = true;
            lT_22700.exitTime = 0.25f;
            lT_22700.duration = 0.2f;
            lT_22700.offset = 0.1060333f;
            lT_22700.mute = false;
            lT_22700.solo = false;
            lT_22700.canTransitionToSelf = true;
            lT_22700.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22700.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27131f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_22702 = lS_21808.AddTransition(lS_22674);
            lT_22702.hasExitTime = true;
            lT_22702.hasFixedDuration = true;
            lT_22702.exitTime = 0.75f;
            lT_22702.duration = 0.2f;
            lT_22702.offset = 0.4174516f;
            lT_22702.mute = false;
            lT_22702.solo = false;
            lT_22702.canTransitionToSelf = true;
            lT_22702.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22702.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27131f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_22704 = lS_21808.AddTransition(lS_22678);
            lT_22704.hasExitTime = true;
            lT_22704.hasFixedDuration = true;
            lT_22704.exitTime = 0.75f;
            lT_22704.duration = 0.2f;
            lT_22704.offset = 0.256667f;
            lT_22704.mute = false;
            lT_22704.solo = false;
            lT_22704.canTransitionToSelf = true;
            lT_22704.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22704.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27132f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_22706 = lS_21808.AddTransition(lS_22676);
            lT_22706.hasExitTime = true;
            lT_22706.hasFixedDuration = true;
            lT_22706.exitTime = 0.25f;
            lT_22706.duration = 0.2f;
            lT_22706.offset = 0.2689477f;
            lT_22706.mute = false;
            lT_22706.solo = false;
            lT_22706.canTransitionToSelf = true;
            lT_22706.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22706.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27132f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_22708 = lS_21802.AddTransition(lS_21808);
            lT_22708.hasExitTime = true;
            lT_22708.hasFixedDuration = true;
            lT_22708.exitTime = 0.75f;
            lT_22708.duration = 0.15f;
            lT_22708.offset = 0.0963606f;
            lT_22708.mute = false;
            lT_22708.solo = false;
            lT_22708.canTransitionToSelf = true;
            lT_22708.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22708.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_22710 = lS_21802.AddTransition(lS_22668);
            lT_22710.hasExitTime = true;
            lT_22710.hasFixedDuration = true;
            lT_22710.exitTime = 0.8404255f;
            lT_22710.duration = 0.25f;
            lT_22710.offset = 0f;
            lT_22710.mute = false;
            lT_22710.solo = false;
            lT_22710.canTransitionToSelf = true;
            lT_22710.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22710.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_22712 = lS_21804.AddTransition(lS_21808);
            lT_22712.hasExitTime = true;
            lT_22712.hasFixedDuration = true;
            lT_22712.exitTime = 0.75f;
            lT_22712.duration = 0.15f;
            lT_22712.offset = 0.6026077f;
            lT_22712.mute = false;
            lT_22712.solo = false;
            lT_22712.canTransitionToSelf = true;
            lT_22712.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22712.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_22714 = lS_21804.AddTransition(lS_22668);
            lT_22714.hasExitTime = true;
            lT_22714.hasFixedDuration = true;
            lT_22714.exitTime = 0.7916668f;
            lT_22714.duration = 0.25f;
            lT_22714.offset = 0f;
            lT_22714.mute = false;
            lT_22714.solo = false;
            lT_22714.canTransitionToSelf = true;
            lT_22714.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22714.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_22716 = lS_21806.AddTransition(lS_21808);
            lT_22716.hasExitTime = true;
            lT_22716.hasFixedDuration = true;
            lT_22716.exitTime = 0.8846154f;
            lT_22716.duration = 0.25f;
            lT_22716.offset = 0.8864383f;
            lT_22716.mute = false;
            lT_22716.solo = false;
            lT_22716.canTransitionToSelf = true;
            lT_22716.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22716.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_22718 = lS_21806.AddTransition(lS_22668);
            lT_22718.hasExitTime = true;
            lT_22718.hasFixedDuration = true;
            lT_22718.exitTime = 0.8584907f;
            lT_22718.duration = 0.25f;
            lT_22718.offset = 0f;
            lT_22718.mute = false;
            lT_22718.solo = false;
            lT_22718.canTransitionToSelf = true;
            lT_22718.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22718.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_22720 = lS_21810.AddTransition(lS_21808);
            lT_22720.hasExitTime = true;
            lT_22720.hasFixedDuration = true;
            lT_22720.exitTime = 0.9074074f;
            lT_22720.duration = 0.25f;
            lT_22720.offset = 0.3468954f;
            lT_22720.mute = false;
            lT_22720.solo = false;
            lT_22720.canTransitionToSelf = true;
            lT_22720.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22720.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_22722 = lS_21810.AddTransition(lS_22668);
            lT_22722.hasExitTime = true;
            lT_22722.hasFixedDuration = true;
            lT_22722.exitTime = 0.8584907f;
            lT_22722.duration = 0.25f;
            lT_22722.offset = 0f;
            lT_22722.mute = false;
            lT_22722.solo = false;
            lT_22722.canTransitionToSelf = true;
            lT_22722.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22722.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_22724 = lS_21814.AddTransition(lS_21808);
            lT_22724.hasExitTime = true;
            lT_22724.hasFixedDuration = true;
            lT_22724.exitTime = 0.7222224f;
            lT_22724.duration = 0.25f;
            lT_22724.offset = 0f;
            lT_22724.mute = false;
            lT_22724.solo = false;
            lT_22724.canTransitionToSelf = true;
            lT_22724.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_22726 = lS_21814.AddTransition(lS_22668);
            lT_22726.hasExitTime = true;
            lT_22726.hasFixedDuration = true;
            lT_22726.exitTime = 0.7794119f;
            lT_22726.duration = 0.25f;
            lT_22726.offset = 0f;
            lT_22726.mute = false;
            lT_22726.solo = false;
            lT_22726.canTransitionToSelf = true;
            lT_22726.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22726.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_22728 = lS_21812.AddTransition(lS_21808);
            lT_22728.hasExitTime = true;
            lT_22728.hasFixedDuration = true;
            lT_22728.exitTime = 0.7580653f;
            lT_22728.duration = 0.25f;
            lT_22728.offset = 0f;
            lT_22728.mute = false;
            lT_22728.solo = false;
            lT_22728.canTransitionToSelf = true;
            lT_22728.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_22730 = lS_21812.AddTransition(lS_22668);
            lT_22730.hasExitTime = true;
            lT_22730.hasFixedDuration = true;
            lT_22730.exitTime = 0.8125004f;
            lT_22730.duration = 0.25f;
            lT_22730.offset = 0f;
            lT_22730.mute = false;
            lT_22730.solo = false;
            lT_22730.canTransitionToSelf = true;
            lT_22730.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22730.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_22732 = lS_21816.AddTransition(lS_21808);
            lT_22732.hasExitTime = true;
            lT_22732.hasFixedDuration = true;
            lT_22732.exitTime = 0.7580646f;
            lT_22732.duration = 0.25f;
            lT_22732.offset = 0.5379788f;
            lT_22732.mute = false;
            lT_22732.solo = false;
            lT_22732.canTransitionToSelf = true;
            lT_22732.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_22734 = lS_21816.AddTransition(lS_22668);
            lT_22734.hasExitTime = true;
            lT_22734.hasFixedDuration = true;
            lT_22734.exitTime = 0.7794119f;
            lT_22734.duration = 0.25f;
            lT_22734.offset = 0f;
            lT_22734.mute = false;
            lT_22734.solo = false;
            lT_22734.canTransitionToSelf = true;
            lT_22734.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22734.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_22736 = lS_21818.AddTransition(lS_21808);
            lT_22736.hasExitTime = true;
            lT_22736.hasFixedDuration = true;
            lT_22736.exitTime = 0.8255816f;
            lT_22736.duration = 0.25f;
            lT_22736.offset = 0.5181294f;
            lT_22736.mute = false;
            lT_22736.solo = false;
            lT_22736.canTransitionToSelf = true;
            lT_22736.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_22738 = lS_21818.AddTransition(lS_22668);
            lT_22738.hasExitTime = true;
            lT_22738.hasFixedDuration = true;
            lT_22738.exitTime = 0.8125004f;
            lT_22738.duration = 0.25f;
            lT_22738.offset = 0f;
            lT_22738.mute = false;
            lT_22738.solo = false;
            lT_22738.canTransitionToSelf = true;
            lT_22738.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22738.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_22740 = lS_21820.AddTransition(lS_21808);
            lT_22740.hasExitTime = true;
            lT_22740.hasFixedDuration = true;
            lT_22740.exitTime = 0.6182807f;
            lT_22740.duration = 0.25f;
            lT_22740.offset = 0.02634108f;
            lT_22740.mute = false;
            lT_22740.solo = false;
            lT_22740.canTransitionToSelf = true;
            lT_22740.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_22742 = lS_21820.AddTransition(lS_22668);
            lT_22742.hasExitTime = true;
            lT_22742.hasFixedDuration = true;
            lT_22742.exitTime = 0.6250002f;
            lT_22742.duration = 0.25f;
            lT_22742.offset = 0f;
            lT_22742.mute = false;
            lT_22742.solo = false;
            lT_22742.canTransitionToSelf = true;
            lT_22742.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22742.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_22744 = lS_22670.AddTransition(lS_21808);
            lT_22744.hasExitTime = true;
            lT_22744.hasFixedDuration = true;
            lT_22744.exitTime = 0.8469388f;
            lT_22744.duration = 0.25f;
            lT_22744.offset = 0f;
            lT_22744.mute = false;
            lT_22744.solo = false;
            lT_22744.canTransitionToSelf = true;
            lT_22744.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_22746 = lS_22672.AddTransition(lS_21808);
            lT_22746.hasExitTime = true;
            lT_22746.hasFixedDuration = true;
            lT_22746.exitTime = 0.8636364f;
            lT_22746.duration = 0.25f;
            lT_22746.offset = 0.8593867f;
            lT_22746.mute = false;
            lT_22746.solo = false;
            lT_22746.canTransitionToSelf = true;
            lT_22746.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_22748 = lS_22674.AddTransition(lS_22668);
            lT_22748.hasExitTime = true;
            lT_22748.hasFixedDuration = true;
            lT_22748.exitTime = 0.7f;
            lT_22748.duration = 0.2f;
            lT_22748.offset = 0f;
            lT_22748.mute = false;
            lT_22748.solo = false;
            lT_22748.canTransitionToSelf = true;
            lT_22748.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_22750 = lS_22674.AddTransition(lS_21808);
            lT_22750.hasExitTime = false;
            lT_22750.hasFixedDuration = true;
            lT_22750.exitTime = 0.8684211f;
            lT_22750.duration = 0.25f;
            lT_22750.offset = 0f;
            lT_22750.mute = false;
            lT_22750.solo = false;
            lT_22750.canTransitionToSelf = true;
            lT_22750.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22750.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27133f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_22752 = lS_22676.AddTransition(lS_21808);
            lT_22752.hasExitTime = false;
            lT_22752.hasFixedDuration = true;
            lT_22752.exitTime = 0.75f;
            lT_22752.duration = 0.25f;
            lT_22752.offset = 0f;
            lT_22752.mute = false;
            lT_22752.solo = false;
            lT_22752.canTransitionToSelf = true;
            lT_22752.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22752.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27133f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_22754 = lS_22676.AddTransition(lS_22668);
            lT_22754.hasExitTime = true;
            lT_22754.hasFixedDuration = true;
            lT_22754.exitTime = 0.8f;
            lT_22754.duration = 0.2f;
            lT_22754.offset = 0f;
            lT_22754.mute = false;
            lT_22754.solo = false;
            lT_22754.canTransitionToSelf = true;
            lT_22754.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_22756 = lS_22678.AddTransition(lS_21808);
            lT_22756.hasExitTime = false;
            lT_22756.hasFixedDuration = true;
            lT_22756.exitTime = 0.75f;
            lT_22756.duration = 0.25f;
            lT_22756.offset = 0f;
            lT_22756.mute = false;
            lT_22756.solo = false;
            lT_22756.canTransitionToSelf = true;
            lT_22756.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22756.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27133f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_22758 = lS_22678.AddTransition(lS_22668);
            lT_22758.hasExitTime = true;
            lT_22758.hasFixedDuration = true;
            lT_22758.exitTime = 0.8f;
            lT_22758.duration = 0.2f;
            lT_22758.offset = 0f;
            lT_22758.mute = false;
            lT_22758.solo = false;
            lT_22758.canTransitionToSelf = true;
            lT_22758.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_22760 = lS_22680.AddTransition(lS_21808);
            lT_22760.hasExitTime = false;
            lT_22760.hasFixedDuration = true;
            lT_22760.exitTime = 0.8170732f;
            lT_22760.duration = 0.25f;
            lT_22760.offset = 0f;
            lT_22760.mute = false;
            lT_22760.solo = false;
            lT_22760.canTransitionToSelf = true;
            lT_22760.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22760.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27133f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_22762 = lS_22680.AddTransition(lS_22668);
            lT_22762.hasExitTime = true;
            lT_22762.hasFixedDuration = true;
            lT_22762.exitTime = 0.5021765f;
            lT_22762.duration = 0.1999999f;
            lT_22762.offset = 0.04457206f;
            lT_22762.mute = false;
            lT_22762.solo = false;
            lT_22762.canTransitionToSelf = true;
            lT_22762.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_22764 = lS_21828.AddTransition(lS_22682);
            lT_22764.hasExitTime = true;
            lT_22764.hasFixedDuration = true;
            lT_22764.exitTime = 0.3138752f;
            lT_22764.duration = 0.15f;
            lT_22764.offset = 0f;
            lT_22764.mute = false;
            lT_22764.solo = false;
            lT_22764.canTransitionToSelf = true;
            lT_22764.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_22766 = lS_21830.AddTransition(lS_22682);
            lT_22766.hasExitTime = true;
            lT_22766.hasFixedDuration = true;
            lT_22766.exitTime = 0.5643811f;
            lT_22766.duration = 0.15f;
            lT_22766.offset = 0f;
            lT_22766.mute = false;
            lT_22766.solo = false;
            lT_22766.canTransitionToSelf = true;
            lT_22766.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_22768 = lS_21832.AddTransition(lS_22682);
            lT_22768.hasExitTime = true;
            lT_22768.hasFixedDuration = true;
            lT_22768.exitTime = 0.7016318f;
            lT_22768.duration = 0.15f;
            lT_22768.offset = 0f;
            lT_22768.mute = false;
            lT_22768.solo = false;
            lT_22768.canTransitionToSelf = true;
            lT_22768.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_22770 = lS_21826.AddTransition(lS_22682);
            lT_22770.hasExitTime = true;
            lT_22770.hasFixedDuration = true;
            lT_22770.exitTime = 0.2468245f;
            lT_22770.duration = 0.15f;
            lT_22770.offset = 0f;
            lT_22770.mute = false;
            lT_22770.solo = false;
            lT_22770.canTransitionToSelf = true;
            lT_22770.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_22772 = lS_21824.AddTransition(lS_22682);
            lT_22772.hasExitTime = true;
            lT_22772.hasFixedDuration = true;
            lT_22772.exitTime = 0.5180793f;
            lT_22772.duration = 0.15f;
            lT_22772.offset = 0f;
            lT_22772.mute = false;
            lT_22772.solo = false;
            lT_22772.canTransitionToSelf = true;
            lT_22772.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_22774 = lS_21822.AddTransition(lS_22682);
            lT_22774.hasExitTime = true;
            lT_22774.hasFixedDuration = true;
            lT_22774.exitTime = 0.6774405f;
            lT_22774.duration = 0.15f;
            lT_22774.offset = 0f;
            lT_22774.mute = false;
            lT_22774.solo = false;
            lT_22774.canTransitionToSelf = true;
            lT_22774.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_22776 = lS_22682.AddTransition(lS_21808);
            lT_22776.hasExitTime = false;
            lT_22776.hasFixedDuration = true;
            lT_22776.exitTime = 0f;
            lT_22776.duration = 0.1f;
            lT_22776.offset = 0f;
            lT_22776.mute = false;
            lT_22776.solo = false;
            lT_22776.canTransitionToSelf = true;
            lT_22776.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22776.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");

        }

        /// <summary>
        /// Gathers the animations so we can use them when creating the sub-state machine.
        /// </summary>
        public override void FindAnimations()
        {
            m14306 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose");
            m19088 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_WalkFWD_v2.fbx/WalkForward.anim", "WalkForward");
            m14266 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunForward_v2.fbx/RunForward.anim", "RunForward");
            m21162 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/IdleToWalk90L.anim", "IdleToWalk90L");
            m21164 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/IdleToWalk90R.anim", "IdleToWalk90R");
            m21168 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/IdleToWalk180R.anim", "IdleToWalk180R");
            m21166 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/IdleToWalk180L.anim", "IdleToWalk180L");
            m18166 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_Idle2Run_v2.fbx/IdleToRun90L.anim", "IdleToRun90L");
            m18170 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_Idle2Run_v2.fbx/IdleToRun180L.anim", "IdleToRun180L");
            m18168 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_Idle2Run_v2.fbx/IdleToRun90R.anim", "IdleToRun90R");
            m18172 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_Idle2Run_v2.fbx/IdleToRun180R.anim", "IdleToRun180R");
            m14264 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunForward_v2.fbx/IdleToRun.anim", "IdleToRun");
            m18032 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_PlantNTurn180_Run_R_1.fbx/RunPivot180R_LDown.anim", "RunPivot180R_LDown");
            m21172 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/WalkPivot180L.anim", "WalkPivot180L");
            m17466 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_PlantNTurn180_Run_R_2.fbx/RunToIdle_LDown.anim", "RunToIdle_LDown");
            m21176 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/WalkToIdle_LDown.anim", "WalkToIdle_LDown");
            m21178 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/WalkToIdle_RDown.anim", "WalkToIdle_RDown");
            m19758 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_HalfSteps2Idle_PasingLongStepTOIdle.fbx/RunToIdle_RDown.anim", "RunToIdle_RDown");
            m14316 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdleTurn90R.anim", "IdleTurn90R");
            m14320 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdleTurn180R.anim", "IdleTurn180R");
            m14314 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdleTurn90L.anim", "IdleTurn90L");
            m14318 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdleTurn180L.anim", "IdleTurn180L");

            // Add the remaining functionality
            base.FindAnimations();
        }

        /// <summary>
        /// Used to show the settings that allow us to generate the animator setup.
        /// </summary>
        public override void OnSettingsGUI()
        {
            UnityEditor.EditorGUILayout.IntField(new GUIContent("Phase ID", "Phase ID used to transition to the state."), PHASE_START);
            m14306 = CreateAnimationField("Move Tree.IdlePose", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose", m14306);
            m19088 = CreateAnimationField("Move Tree.WalkForward", "Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_WalkFWD_v2.fbx/WalkForward.anim", "WalkForward", m19088);
            m14266 = CreateAnimationField("Move Tree.RunForward", "Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunForward_v2.fbx/RunForward.anim", "RunForward", m14266);
            m21162 = CreateAnimationField("IdleToWalk90L", "Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/IdleToWalk90L.anim", "IdleToWalk90L", m21162);
            m21164 = CreateAnimationField("IdleToWalk90R", "Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/IdleToWalk90R.anim", "IdleToWalk90R", m21164);
            m21168 = CreateAnimationField("IdleToWalk180R", "Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/IdleToWalk180R.anim", "IdleToWalk180R", m21168);
            m21166 = CreateAnimationField("IdleToWalk180L", "Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/IdleToWalk180L.anim", "IdleToWalk180L", m21166);
            m18166 = CreateAnimationField("IdleToRun90L", "Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_Idle2Run_v2.fbx/IdleToRun90L.anim", "IdleToRun90L", m18166);
            m18170 = CreateAnimationField("IdleToRun180L", "Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_Idle2Run_v2.fbx/IdleToRun180L.anim", "IdleToRun180L", m18170);
            m18168 = CreateAnimationField("IdleToRun90R", "Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_Idle2Run_v2.fbx/IdleToRun90R.anim", "IdleToRun90R", m18168);
            m18172 = CreateAnimationField("IdleToRun180R", "Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_Idle2Run_v2.fbx/IdleToRun180R.anim", "IdleToRun180R", m18172);
            m14264 = CreateAnimationField("IdleToRun", "Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunForward_v2.fbx/IdleToRun.anim", "IdleToRun", m14264);
            m18032 = CreateAnimationField("RunPivot180R_LDown", "Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_PlantNTurn180_Run_R_1.fbx/RunPivot180R_LDown.anim", "RunPivot180R_LDown", m18032);
            m21172 = CreateAnimationField("WalkPivot180L", "Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/WalkPivot180L.anim", "WalkPivot180L", m21172);
            m17466 = CreateAnimationField("RunToIdle_LDown", "Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_PlantNTurn180_Run_R_2.fbx/RunToIdle_LDown.anim", "RunToIdle_LDown", m17466);
            m21176 = CreateAnimationField("WalkToIdle_LDown", "Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/WalkToIdle_LDown.anim", "WalkToIdle_LDown", m21176);
            m21178 = CreateAnimationField("WalkToIdle_RDown", "Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/WalkToIdle_RDown.anim", "WalkToIdle_RDown", m21178);
            m19758 = CreateAnimationField("RunToIdle_RDown", "Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_HalfSteps2Idle_PasingLongStepTOIdle.fbx/RunToIdle_RDown.anim", "RunToIdle_RDown", m19758);
            m14316 = CreateAnimationField("IdleTurn20R.IdleTurn90R", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdleTurn90R.anim", "IdleTurn90R", m14316);
            m14320 = CreateAnimationField("IdleTurn180R", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdleTurn180R.anim", "IdleTurn180R", m14320);
            m14314 = CreateAnimationField("IdleTurn20L.IdleTurn90L", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdleTurn90L.anim", "IdleTurn90L", m14314);
            m14318 = CreateAnimationField("IdleTurn180L", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdleTurn180L.anim", "IdleTurn180L", m14318);

            // Add the remaining functionality
            base.OnSettingsGUI();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}
