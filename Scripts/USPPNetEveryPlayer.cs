﻿#define USPPNet_string
#define USPPNet_int
#define USPPNet_bool
#define USPPNet_float
#define USPPNet_Vector2
#define USPPNet_Vector3
#define USPPNet_Vector4
#define USPPNet_Quaternion

using USPPNet;

using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace GroupSync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class USPPNetEveryPlayer : UdonSharpBehaviour
    {
        public GroupManager groupManager;
        public GroupObjectSyncManager syncManager;
        public USPPNetEveryPlayerManager playerManager;

        [Space(10)]
        public bool neverCloseGroups;
    
        private USPPNetEveryPlayerManager _usppNetEveryPlayerManager;
        [UdonSynced] [HideInInspector] public bool owned;

        private string _currZone = "";

        private void USPPNET_request_new_group(string zone, int playerId)
        {
            if (!Networking.IsMaster)
                return;
        
            _usppNetEveryPlayerManager.groupManager.RemovePlayerFromGroups(playerId);
            var group = _usppNetEveryPlayerManager.groupManager.GetJoinableGroup(zone);
            _usppNetEveryPlayerManager.groupManager.AddPlayerToGroup(playerId, group);
        
            Debug.Log($"zone: {zone}, player: {playerId}");
        }
    
        private void USPPNET_request_remove_from_group(int playerId)
        {
            if (!Networking.IsMaster)
                return;
        
            _usppNetEveryPlayerManager.groupManager.RemovePlayerFromGroups(playerId);
        
            Debug.Log($"player: {playerId} removed from group");
        }

        private void USPPNET_request_leave_callback(int playerId)
        {
            if (Networking.LocalPlayer.playerId == playerId)
                do_leave_callback();
        }

        private void GenericSet(int group, string varName, int netId, object var)
        {
            if ((group == -1 || group != groupManager.local_group) && group != -2)
                return;
            if (!syncManager.syncedCustomObjects.TryGetValue(netId, out var data))
                return;
            var obj = (GroupCustomSync)data.Reference;

            obj.SetProgramVariable(varName, var);
        }
    
        private void USPPNET_CustomSet_int(int group, string varName, int netId, int var)
        {
            GenericSet(group, varName, netId, var);
        }
        private void USPPNET_CustomSet_string(int group, string varName, int netId, string var)
        {
            GenericSet(group, varName, netId, var);
        }
        private void USPPNET_CustomSet_bool(int group, string varName, int netId, bool var)
        {
            GenericSet(group, varName, netId, var);
        }
        private void USPPNET_CustomSet_float(int group, string varName, int netId, float var)
        {
            GenericSet(group, varName, netId, var);
        }
        private void USPPNET_CustomSet_Vector2(int group, string varName, int netId, Vector2 var)
        {
            GenericSet(group, varName, netId, var);
        }
        private void USPPNET_CustomSet_Vector3(int group, string varName, int netId, Vector3 var)
        {
            GenericSet(group, varName, netId, var);
        }
        private void USPPNET_CustomSet_Vector4(int group, string varName, int netId, Vector4 var)
        {
            GenericSet(group, varName, netId, var);
        }
        private void USPPNET_CustomSet_Quaternion(int group, string varName, int netId, Quaternion var)
        {
            GenericSet(group, varName, netId, var);
        }
        private void USPPNET_CustomSet_GroupCustomSync(int group, string varName, int netId, int networkId)
        {
            if (!syncManager.syncedCustomObjects.TryGetValue(networkId, out var data))
            {
                Debug.LogError($"couldn't get <color=red>{networkId}</color>?");
                return;
            }
            GenericSet(group, varName, netId, (GroupCustomSync)data.Reference);
        }

        private void USPPNET_CustomRPC(int group, string eventName, int netId)
        {
            if ((group == -1 || group != groupManager.local_group) && group != -2 )
                return;
            if (!syncManager.syncedCustomObjects.TryGetValue(netId, out var data))
                return;
            var obj = (GroupCustomSync)data.Reference;

            obj.SendCustomEvent(eventName);
        }


        // ReSharper disable Unity.PerformanceAnalysis
        public void SetRemoteVar(int group, string varName, int netId, object var, bool setLocally = true)
        {
            if (!Utilities.IsValid(var))
                return;
            
            var argType = var.GetType();
        
            if (argType == typeof(int))
                USPPNET_CustomSet_int(group, varName, netId, (int)var);
            else if (argType == typeof(string))
                USPPNET_CustomSet_string(group, varName, netId, (string)var);
            else if (argType == typeof(bool))
                USPPNET_CustomSet_bool(group, varName, netId, (bool)var);
            else if (argType == typeof(float))
                USPPNET_CustomSet_float(group, varName, netId, (float)var);
            else if (argType == typeof(Vector2))
                USPPNET_CustomSet_Vector2(group, varName, netId, (Vector2)var);
            else if (argType == typeof(Vector3))
                USPPNET_CustomSet_Vector3(group, varName, netId, (Vector3)var);
            else if (argType == typeof(Vector4))
                USPPNET_CustomSet_Vector4(group, varName, netId, (Vector4)var);
            else if (argType == typeof(Quaternion))
                USPPNET_CustomSet_Quaternion(group, varName, netId, (Quaternion)var);
            else if (argType == typeof(UdonSharpBehaviour))
                USPPNET_CustomSet_GroupCustomSync(group, varName, netId, ((GroupCustomSync)var).networkId);
            else
                Debug.LogError($"Variable type {argType} Not supported in SetRemoteVar");
        
            if (setLocally)
                GenericSet(group, varName, netId, var);
        }

        public void RemoteFunctionCall(int group, string eventName, int netId, bool callLocally = true)
        {
            USPPNET_CustomRPC(group, eventName, netId);
        
            if (!callLocally) return;
            if (!syncManager.syncedCustomObjects.TryGetValue(netId, out var data))
                return;
        
            var obj = (GroupCustomSync)data.Reference;

            obj.SendCustomEvent(eventName);
        }

        // Master 
        private void USPPNET_close_group_joins(int group)
        {
            if (!Networking.IsMaster)
                return;

            groupManager.DisableJoinGroup(group);
            Debug.Log($"Group: {group} joins closed.");
        }

        // Client
        public void close_group_joinings(int group)
        {
            if (neverCloseGroups)
                return;
        
            if (!Networking.IsMaster)
            {
                USPPNET_close_group_joins(group);
                RequestSerialization();
            }
            else
                groupManager.DisableJoinGroup(group);
        }

        public void request_new_group(string zone)
        {
            if (_currZone == zone)
                return;
            var playerId = Networking.LocalPlayer.playerId;
            request_remove_from_group(playerId);
            _currZone = zone;
        
            if (Networking.IsMaster)
            {
                _usppNetEveryPlayerManager.groupManager.RemovePlayerFromGroups(playerId);
                var group = _usppNetEveryPlayerManager.groupManager.GetJoinableGroup(zone);
                _usppNetEveryPlayerManager.groupManager.AddPlayerToGroup(playerId, group);
                return;
            }
        
            USPPNET_request_new_group(zone, playerId);
            RequestSerialization();
        }

        private void do_leave_callback()
        {
            groupManager.local_group = -1;
            _currZone = "";
        
            // This needs to be called locally, and this doesn't actually get called on group leave,
            // Just on requesting a new one, which might as well be the same thing.
            for (var index = 0; index < _usppNetEveryPlayerManager.groupManager.leaveGroupCallbacks.Count; index++)
            {
                var caller = _usppNetEveryPlayerManager.groupManager.leaveGroupCallbacks[index];
                if ((UdonSharpBehaviour)caller.Reference != null)
                    ((UdonSharpBehaviour)caller.Reference).SendCustomEvent("LeaveCallback");
            }
        }

        /// <summary>
        /// Probably shouldn't call this outside of here
        /// </summary>
        public void request_leave_callback(int playerId)
        {
            if (Networking.LocalPlayer.playerId == playerId)
            {
                do_leave_callback();
            }
            else
            {
                USPPNET_request_leave_callback(playerId);
                RequestSerialization();
            }
        }

        public void request_remove_from_group(int playerId)
        {
            request_leave_callback(playerId);
        
            if (Networking.IsMaster)
            {
                _usppNetEveryPlayerManager.groupManager.RemovePlayerFromGroups(playerId);
                Debug.Log($"player: {playerId} removed from group");
                return;
            }
        
            USPPNET_request_remove_from_group(playerId);
            RequestSerialization();
        }

        private float _serializationTimer;
        private bool _requestedSerialization;
        public new void RequestSerialization()
        {
            _requestedSerialization = true;
        }
    
        public void Bootstrap()
        {
            _usppNetEveryPlayerManager = transform.parent.GetComponent<USPPNetEveryPlayerManager>();
        }

        public void FixedUpdate()
        {
            if (!Networking.IsOwner(gameObject))
                return;
            _serializationTimer += Time.deltaTime;
            if (_requestedSerialization && _serializationTimer > 0.16)
            {
                _serializationTimer = 0;
                _requestedSerialization = false;
                base.RequestSerialization();
            }
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (!Networking.IsMaster)
                return;
        
            if (playerManager.local_object == this)
                return;
        
            if (Networking.IsOwner(gameObject))
                owned = false;
        }

        public override void OnDeserialization()
        {
            // USPPNet OnDeserialization
        }
    
        private long _tLastSerial = 0;
        public long byteDisplay;
        public long byteCounter;
        public override void OnPostSerialization(VRC.Udon.Common.SerializationResult result)
        {
            if ((DateTime.Now.Ticks - _tLastSerial) / TimeSpan.TicksPerMillisecond > 1000)
            {
                _tLastSerial = DateTime.Now.Ticks;
                byteDisplay = byteCounter;
                byteCounter = result.byteCount;
            }
            else
            {
                byteCounter += result.byteCount;
            }
            // USPPNet OnPostSerialization
        }

        // USPPNet Init
    }
}