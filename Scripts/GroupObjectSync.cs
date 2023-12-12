using System;
//using bSenpai.UdonProfiler;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using ArrayExtensions = GroupSync.Extensions.ArrayExtensions;

namespace GroupSync
{
    [RequireComponent(typeof(Rigidbody))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(100)]
    public class GroupObjectSync : GroupCustomSync
    {
        [Space(20)]

        [NonSerialized] public bool HasPickup; 
        [NonSerialized] public VRC_Pickup Pickup;
        [NonSerialized] public int RequestedOwner = -1;
        
        public float respawnHeight = -70;
    
        private Vector3 _startingPosition;
        private Quaternion _startingRotation;
    
        private int _localPlayer = -99;

        private bool _hasGravity;
        private bool _isKinematic;
        private Rigidbody _rb;

        private bool _hasBehaviourEnabler;
        private LocalBehaviourEnabler[] _behaviourEnablers = Array.Empty<LocalBehaviourEnabler>();
    
        [Range(0.1f, 2f)] public float updateSeconds = 0.16f;
        [Range(0.1f, 2f)] public float handUpdateSeconds = 0.33f;

        public bool closeGroupOnOwnerChange = true;

        public bool allowTheft;

        private bool _syncAnyways;
    
        public override void OnPickup()
        {
            SimulatePickup(Pickup.currentHand == VRC_Pickup.PickupHand.Left);
        }

        public override void OnDrop()
        {
            SimulateDrop();
        }

        /// <summary>
        /// Simulates the player dropping the object, but doesn't actually drop it.
        /// </summary>
        [PublicAPI]
        public void SimulateDrop()
        {
            _delayedHandSync = false;
            SetVariableInLocalGroup(nameof(handSync), 0);
            SyncObjectChanges(true);
            CallFunctionInLocalGroup(nameof(FB), false);
            UpdateGrabbed(false);
        }

        private bool _syncHand;
        private bool _delayedHandSync;
        private float _timeUntilHandSync;

        /// <summary>
        /// Simulates the object being picked up.
        /// If you don't want the grabber option, then call BecomeOwner instead.
        /// </summary>
        /// <param name="lHand">Left or right hand grabbing</param>
        [PublicAPI]
        public void SimulatePickup(bool lHand)
        {
            BecomeOwnerAndSync();

            _delayedHandSync = true;
            _timeUntilHandSync = 1.0f;
            _syncHand = lHand;
        }

        [PublicAPI]
        public void UpdateGrabbed(bool state)
        {
            SetVariableInLocalGroup(nameof(ih), state);
            CallFunctionInLocalGroup(nameof(PreSubPostLateUpdateCallback));
        }

        [PublicAPI]
        public bool IsGrabbed()
        {
            return ih;
        }

        [PublicAPI]
        public bool IsOwner()
        {
            return cu == _localPlayer;
        }
        [PublicAPI]
        public bool HasOwner()
        {
            return cu != -1;
        }

        [PublicAPI]
        public VRCPlayerApi GetOwner()
        {
            return VRCPlayerApi.GetPlayerById(cu);
        }

        [PublicAPI]
        public void LeaveCallback()
        {
            if (!forceGlobalSync)
                UnSync();
        }
    
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!forceGlobalSync || cu != _localPlayer || cu == -1)
                return;
        
            _syncAnyways = true;
        }
    
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (player == null || !player.IsValid()) return;
            if (player.playerId == cu)
                UnSync();
        }

        [PublicAPI]
        public void UnSync()
        {
            if (!IsOwner() || !StartedNet)
                return;
            if (HasPickup)
                Pickup.Drop();
            SetVariableInLocalGroup(nameof(cu), -1, autoSerialize: false);
            SetVariableInLocalGroup(nameof(handSync), 0, false);
            UpdateGrabbed(false);
            CallFunctionInLocalGroup(nameof(UB), false);

            SetRbState();
        }

        private void TriggerEnablers(bool state)
        {
            foreach (var behaviourEnabler in _behaviourEnablers)
            {
                if (state)
                    behaviourEnabler.EnableComps();
                else
                    behaviourEnabler.DisableComps();
            }
        }

        //private Profiler _profiler;
        public override void Start()
        {
            if (StartedNet)
                return;

            //_profiler = this.GetComponentInHighestParent<RootInfo>().profiler;
            //_profiler.BeginSample("GOS: Start()");
        
            _rb = GetComponent<Rigidbody>();
            _isKinematic = _rb.isKinematic;
            _hasGravity = _rb.useGravity;
            
            _behaviourEnablers = GetComponents<LocalBehaviourEnabler>();
            var get2 = GetComponentsInChildren<LocalBehaviourEnabler>();
            if (_behaviourEnablers.Length == 0)
                _behaviourEnablers = get2;
            else if (get2.Length > 0)
                _behaviourEnablers = ArrayExtensions.Concat(_behaviourEnablers, get2);
            
            _hasBehaviourEnabler = _behaviourEnablers.Length > 0;
        
            _startingPosition = transform.position;
            _startingRotation = transform.rotation;
            if (StartNet())
            {
                SubLeaveGroupCallback();
                PreSubPostLateUpdateCallback();
            }
        
            Pickup = GetComponent<VRC_Pickup>();
            HasPickup = Utilities.IsValid(Pickup);

            _localPlayer = Networking.LocalPlayer.playerId;
        
            _lastPos = transform.position;
            _lastRot = transform.rotation;
            
            if (RequestedOwner != -1)
                SetVariableInLocalGroup(nameof(cu), RequestedOwner);

            if (_hasBehaviourEnabler)
            {
                foreach (var behaviourEnabler in _behaviourEnablers)
                    behaviourEnabler.StartWithGroupSync();
                
                TriggerEnablers(IsOwner());
            }
            
            //_profiler.EndSample();
        }

        [PublicAPI]
        public void ObjectReset()
        {
            //_profiler.BeginSample("GOS: ObjectReset()");
            
            transform.position = _startingPosition;
            transform.rotation = _startingRotation;
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            
            //_profiler.EndSample();
        }
    
        // Controls Syncing

        private bool _positionChanged;
        private bool _rotationChanged;

        // Keeping these sort to reduce network usage, Variable names and function names are sent with the data.
        // So to save bytes I'm keeping these sort.

        private int cu = -1; // C.U. Current Updater or the Owner of the Object

        // These are used as local offsets (relative to the grabber) if handSync is enabled.
        private Vector3 tp = Vector3.zero; // T.P. Target Position // This acts as a buffer
        private Quaternion tr = Quaternion.identity; // T.R. Target Rotation // This acts as a buffer

        private bool ih = false; // I.H. Is Held
    
        private Vector3 _tp_buffer2 = Vector3.zero;
        private Quaternion _tr_buffer2 = Quaternion.identity;
    
        private Vector3 _target_tp = Vector3.zero;
        private Quaternion _target_tr = Quaternion.identity;
    
        private Vector3 _last_tp = Vector3.zero;
        private Quaternion _last_tr = Quaternion.identity;

        private int handSync = 0;
    
        /// <summary>
        /// Updates Object for remote players
        /// </summary>
        /// <param name="force">Skips checks and forces an update.</param>
        [PublicAPI]
        public bool SyncObjectChanges(bool force)
        {
            //_profiler.BeginSample("GOS: SyncObjectChanges()");
            
            if (handSync != 0) // If we're hand syncing, we don't want to update this crap
            {
                //_profiler.EndSample();
                return true;
            }

            if (_syncAnyways)
            {
                BecomeOwner();
            
                force = true;
            }

            var anythingChanged = force || _positionChanged || _rotationChanged;
        
            if (anythingChanged)
                CallFunctionInLocalGroup(nameof(UB), false);
        
            if (force || _positionChanged) // Update Position
                SetVariableInLocalGroup(nameof(tp), transform.position, false);
            if (force || _rotationChanged) // Update Rotation
                SetVariableInLocalGroup(nameof(tr), transform.rotation, false);
        
            if (_syncAnyways)
            {
                CallFunctionInLocalGroup(nameof(TP), false);
                _syncAnyways = false;
            }

            //_profiler.EndSample();
            return anythingChanged;
        }
        /// <summary>
        /// Updates Object for remote players
        /// </summary>
        [PublicAPI]
        public void SyncObjectChangesHand()
        {
            //_profiler.BeginSample("GOS: SyncObjectChangesHand()");
            
            if (handSync == 0) // If we aren't hand syncing, we don't want to update this crap
            {
                //_profiler.EndSample();
                return;
            }

            var isLHand = Pickup.currentHand == VRC_Pickup.PickupHand.Left;

            var pos = Networking.LocalPlayer.GetBonePosition(isLHand
                ? HumanBodyBones.LeftHand
                : HumanBodyBones.RightHand);
            var rot = Quaternion.Inverse(Networking.LocalPlayer.GetBoneRotation(isLHand
                ? HumanBodyBones.LeftHand
                : HumanBodyBones.RightHand));
        
            var localPos = rot * (transform.position - pos);
            var localRot = rot * transform.rotation;
        
            CallFunctionInLocalGroup(nameof(UB), false);
            SetVariableInLocalGroup(nameof(tp), localPos, true);
            SetVariableInLocalGroup(nameof(tr), localRot, true);
            
            //_profiler.EndSample();
        }

        [PublicAPI]
        public void ForceSyncUpdateNextUpdate()
        {
            _syncAnyways = true;
        }

        [PublicAPI]
        public void BecomeOwnerAndSync()
        {
            BecomeOwner();
            SyncObjectChanges(true);
            CallFunctionInLocalGroup(nameof(FB), false);
        }

        /// <summary>
        /// Gets OwnerShip of the object
        /// </summary>
        [PublicAPI]
        public void BecomeOwner()
        {
            if (closeGroupOnOwnerChange)
                CloseCurrentGroup();
            UpdateBecomeOwner();
        }

        private void UpdateBecomeOwner()
        {
            if (!allowTheft && ih)
                return;
        
            CallFunctionInLocalGroup(nameof(PreSubPostLateUpdateCallback));
            SetVariableInLocalGroup(nameof(cu), _localPlayer);
            SetRbState();
        }

        private void SetRbState()
        {
            //_profiler.BeginSample("GOS: SetRbState()");
            var isOwner = (cu == _localPlayer || cu == -1);
            _rb.isKinematic = !isOwner || _isKinematic;
            _rb.useGravity = isOwner && _hasGravity;
            //_profiler.EndSample();
        }

        private int _timesChanged;

        private float _secSinceLastSt;
        public void UB() // U.B. Update Buffers
        {
            //_profiler.BeginSample("GOS: Update Buffers");
            _secSinceLastSt = 0;
        
            if (_timesChanged == 0) // I should probably make the buffering stuff configurable instead of this hard coded crap.
            {
                _tp_buffer2 = tp;
                _tr_buffer2 = tr;
            } else if (_timesChanged == 3)
            {
                FB();
            }
        
            _timesChanged = 0;

            SetRbState();
            PreSubPostLateUpdateCallback();
            
            //_profiler.EndSample();
        }

        public void FB() // F.B. forces the buffers to be rolled all the way down.
        {
            _lastPos = tp;
            _lastRot = tr;
        
            _last_tp = tp;
            _last_tr = tr;
        
            _target_tp = tp;
            _target_tr = tr;
        
            _tp_buffer2 = tp;
            _tr_buffer2 = tr;
        }

        public void TP()
        {
            FB();
        
            _lastPos = tp;
            _lastRot = tr;

            transform.position = tp;
            transform.rotation = tr;
        }

        private float _timer;
        private float _timerHand;

        private Vector3 _lastPos = Vector3.zero;
        private Quaternion _lastRot = Quaternion.identity;

        private void TimerReset()
        {
            _timer =  Mathf.Max(0, _timer - updateSeconds);
            _timerHand -= Mathf.Max(0, _timerHand - handUpdateSeconds);

            _last_tp = _target_tp;
            _last_tr = _target_tr;
        
            _target_tp = _tp_buffer2;
            _target_tr = _tr_buffer2;
        
            _tp_buffer2 = tp;
            _tr_buffer2 = tr;

            _timesChanged++;
        }

        public void OnCollisionEnter(Collision other)
        {
            if (IsOwner())
                PreSubPostLateUpdateCallback();
        }

        public bool canSleep;

        public void AllowSleep()
        {
            canSleep = true;
        }
        
        public void PreSubPostLateUpdateCallback()
        {
            if (isLateSubbed)
                return;

            canSleep = false;
            SendCustomEventDelayedSeconds(nameof(AllowSleep), 1f);
            
            _secSinceLastSt = 0;
            _timesChanged = 0;
            
            _timer = 0;
            _timerHand = 0;
            SubPostLateUpdateCallback();
        }

        public void PreUnSubPostLateUpdateCallback()
        {
            if (canSleep)
                UnSubPostLateUpdateCallback();
        }

        private bool _isLocalOwner;
        private bool _lastUpdate;

        public override void SubPostLateUpdate()
        {
            //_profiler.BeginSample("GOS: SubPostLateUpdate()");
            
            if (cu == -1)
            {
                PreUnSubPostLateUpdateCallback();
                //_profiler.EndSample();
                return;
            }

            var delta = Time.deltaTime;
            _timer += delta;
            _timerHand += delta;
            _secSinceLastSt += delta;
            
            var transform1 = transform;
        
            if (transform1.position.y < respawnHeight)
                ObjectReset();
            if (HasPickup)
                Pickup.pickupable = !ih;

            if (IsOwner())
            {
                if (!_isLocalOwner)
                {
                    _isLocalOwner = true;
                    if (_hasBehaviourEnabler)
                        TriggerEnablers(true);
                }
                if (_delayedHandSync)
                {
                    _timeUntilHandSync -= delta;
                    if (_timeUntilHandSync <= 0)
                    {
                        _delayedHandSync = false;
                        SetVariableInLocalGroup(nameof(handSync), _syncHand ? 1 : 2, true, false);
                        SyncObjectChangesHand();
                        CallFunctionInLocalGroup(nameof(FB), false);
                        UpdateGrabbed(true);
                    }
                }
                if (_timer >= updateSeconds || _syncAnyways)
                {
                    var position = transform1.position;
                    _positionChanged = _lastPos != position;
                    var rotation = transform1.rotation;
                    _rotationChanged = _lastRot != rotation;
                    var updated = SyncObjectChanges(false);
                    _timer = 0;
                    _lastPos = position;
                    _lastRot = rotation;
                    
                    if (!(updated || _lastUpdate))
                        PreUnSubPostLateUpdateCallback();
                    _lastUpdate = updated;
                }
                if (_timerHand >= handUpdateSeconds)
                {
                    SyncObjectChangesHand();
                    _timerHand = 0;
                }
            
            }
            else
            {
                if (_isLocalOwner)
                {
                    _isLocalOwner = false;
                    if (_hasBehaviourEnabler)
                        TriggerEnablers(false);
                }

                if (_secSinceLastSt > 2)
                {
                    PreUnSubPostLateUpdateCallback();
                    //_profiler.EndSample();
                    return;
                }

                if (handSync != 0)
                {
                    if (_timer > handUpdateSeconds)
                        TimerReset();
                    var holder = VRCPlayerApi.GetPlayerById(cu);
                    var isLHand = handSync == 1;
                
                    var pos = holder.GetBonePosition(isLHand
                        ? HumanBodyBones.LeftHand
                        : HumanBodyBones.RightHand);
                    var rot = holder.GetBoneRotation(isLHand
                        ? HumanBodyBones.LeftHand
                        : HumanBodyBones.RightHand);
                
                    var progress = Mathf.Clamp01(_timerHand / handUpdateSeconds);
                
                    var targetPos = Vector3.Lerp(_last_tp, _target_tp, progress);
                    var targetRot = Quaternion.Lerp(_last_tr, _target_tr, progress);

                    transform1.position = rot * targetPos + pos;
                    transform1.rotation = rot * targetRot;
                }
                else
                {
                    if (_timer > updateSeconds)
                        TimerReset();
                    if (_timesChanged > 5)
                        PreUnSubPostLateUpdateCallback();
                
                    var progress = Mathf.Clamp01(_timer / updateSeconds);
                
                    transform1.position = Vector3.Lerp(_last_tp, _target_tp, progress);
                    transform1.rotation = Quaternion.Lerp(_last_tr, _target_tr, progress);
                }
            }
            //_profiler.EndSample();
        }
    }
}