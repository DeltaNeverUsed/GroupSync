﻿using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[RequireComponent(typeof(Rigidbody))]
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GroupObjectSync : GroupCustomSync
{
    [Space(20)]

    [HideInInspector] public bool hasPickup; 
    [HideInInspector] public VRC_Pickup pickup;

    public float respawnHeight = -70;
    
    private bool _startingSync;
    private Vector3 _startingPosition;
    private Quaternion _startingRotation;
    
    private int _localPlayer = -1;
    private Transform _emptyTrans;

    private bool _useGrav;
    private Rigidbody _rb;
    
    [Range(0.1f, 2f)] public float updateSeconds = 0.16f;
    [Range(0.1f, 2f)] public float handUpdateSeconds = 0.33f;

    public bool closeGroupOnOwnerChange = true;

    public bool allowTheft;

    private bool _syncAnyways;

    public override void OnPickup()
    {
        SimulatePickup(pickup.currentHand == VRC_Pickup.PickupHand.Left);
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
        if (hasPickup)
            pickup.Drop();
        SetVariableInLocalGroup(nameof(cu), -1, autoSerialize: false);
        SetVariableInLocalGroup(nameof(handSync), 0, false);
        UpdateGrabbed(false);
        CallFunctionInLocalGroup(nameof(UB), false);
        
        _rb.useGravity = cu == _localPlayer && _useGrav;
    }

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _useGrav = _rb.useGravity;
        
        var etObject = GameObject.Find("GroupTransformProxy");
        if (etObject == null)
        {
            Debug.LogError("Catastrophic Error! The GroupTransformProxy couldn't be found. Did you import the Group Sync Prefab?");
            gameObject.SetActive(false);
            return;
        }
        _emptyTrans = etObject.transform;
        
        _startingPosition = transform.position;
        _startingRotation = transform.rotation;
        if (StartNet())
            SubLeaveGroupCallback();
        
        pickup = GetComponent<VRC_Pickup>();

        hasPickup = pickup != null;

        _localPlayer = Networking.LocalPlayer.playerId;
        
        _lastPos = transform.position;
        _lastRot = transform.rotation;
    }

    [PublicAPI]
    public void ObjectReset()
    {
        transform.position = _startingPosition;
        transform.rotation = _startingRotation;
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
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
    public void SyncObjectChanges(bool force)
    {
        if (handSync != 0) // If we're hand syncing, we don't want to update this crap
            return;

        if (_syncAnyways)
        {
            BecomeOwner();
            _syncAnyways = false;
            force = true;
        }
        
        if (force || _positionChanged || _rotationChanged)
            CallFunctionInLocalGroup(nameof(UB), false);
        
        if (force || _positionChanged) // Update Position
            SetVariableInLocalGroup(nameof(tp), transform.position, false, false);
        if (force || _rotationChanged) // Update Rotation
            SetVariableInLocalGroup(nameof(tr), transform.rotation, false, false);
    }
    /// <summary>
    /// Updates Object for remote players
    /// </summary>
    [PublicAPI]
    public void SyncObjectChangesHand()
    {
        if (handSync == 0) // If we aren't hand syncing, we don't want to update this crap
            return;

        _emptyTrans.position = Networking.LocalPlayer.GetBonePosition(pickup.currentHand == VRC_Pickup.PickupHand.Left
            ? HumanBodyBones.LeftHand
            : HumanBodyBones.RightHand);
        _emptyTrans.rotation = Networking.LocalPlayer.GetBoneRotation(pickup.currentHand == VRC_Pickup.PickupHand.Left
            ? HumanBodyBones.LeftHand
            : HumanBodyBones.RightHand);
        
        var localPos = _emptyTrans.InverseTransformPoint(transform.position);
        var localRot = Quaternion.Inverse(_emptyTrans.rotation) * transform.rotation;
        
        CallFunctionInLocalGroup(nameof(UB), false);
        SetVariableInLocalGroup(nameof(tp), localPos, false, false);
        SetVariableInLocalGroup(nameof(tr), localRot, false, false);
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
        
        SetVariableInLocalGroup(nameof(cu), _localPlayer);
        _rb.useGravity = (cu == _localPlayer || cu == -1) && _useGrav;
    }

    private int _timesChanged;

    private float _secSinceLastSt;
    public void UB() // U.B. Update Buffers
    {
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
        
        _rb.useGravity = (cu == _localPlayer || cu == -1) && _useGrav;
    }

    public void FB() // F.B. forces the buffers to be rolled all the way down.
    {
        _lastPos = transform.position;
        _lastRot = transform.rotation;
        
        _last_tp = tp;
        _last_tr = tr;
        
        _target_tp = tp;
        _target_tr = tr;
        
        _tp_buffer2 = tp;
        _tr_buffer2 = tr;
    }

    private float _timer;
    private float _timerHand;

    private Vector3 _lastPos = Vector3.zero;
    private Quaternion _lastRot = Quaternion.identity;

    private void TimerReset()
    {
        _timer = 0;
        _timerHand = 0;

        _last_tp = _target_tp;
        _last_tr = _target_tr;
        
        _target_tp = _tp_buffer2;
        _target_tr = _tr_buffer2;
        
        _tp_buffer2 = tp;
        _tr_buffer2 = tr;

        _timesChanged++;
    }

    private void Update()
    {
        if (cu == -1)
            return;

        var delta = Time.deltaTime;
        _timer += delta;
        _timerHand += delta;
        _secSinceLastSt += delta;
        
        if (transform.position.y < respawnHeight)
            ObjectReset();
        if (hasPickup)
            pickup.pickupable = !ih;
        
        var transform1 = transform;

        if (cu == _localPlayer)
        {
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
            if (_timer >= updateSeconds)
            {
                var position = transform1.position;
                _positionChanged = _lastPos != position;
                var rotation = transform1.rotation;
                _rotationChanged = _lastRot != rotation;
                SyncObjectChanges(false);
                _timer = 0;
                _lastPos = position;
                _lastRot = rotation;
            }
            if (_timerHand >= handUpdateSeconds)
            {
                SyncObjectChangesHand();
                _timerHand = 0;
            }
            
        }
        else if(_secSinceLastSt < 2)
        {
            if (handSync != 0)
            {
                if (_timer > handUpdateSeconds)
                    TimerReset();
                var holder = VRCPlayerApi.GetPlayerById(cu);
                _emptyTrans.position = holder.GetBonePosition(handSync == 1
                    ? HumanBodyBones.LeftHand
                    : HumanBodyBones.RightHand);
                _emptyTrans.rotation = holder.GetBoneRotation(handSync == 1
                    ? HumanBodyBones.LeftHand
                    : HumanBodyBones.RightHand);
                
                var progress = Mathf.Clamp01(_timerHand / handUpdateSeconds);
                
                var targetPos = Vector3.Lerp(_last_tp, _target_tp, progress);
                var targetRot = Quaternion.Lerp(_last_tr, _target_tr, progress);

                transform.position = _emptyTrans.TransformPoint(targetPos);
                transform.rotation = _emptyTrans.rotation * targetRot;
            }
            else
            {
                if (_timer > updateSeconds)
                    TimerReset();
                
                var progress = Mathf.Clamp01(_timer / updateSeconds);
                
                transform.position = Vector3.Lerp(_last_tp, _target_tp, progress);
                transform.rotation = Quaternion.Lerp(_last_tr, _target_tr, progress);
            }
        }
        else
        {
            _rb.Sleep();
        }
    }
}