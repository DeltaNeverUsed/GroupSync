#define USPPNet_string
#define USPPNet_short
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
    [DefaultExecutionOrder(1000)]
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

        private void USPPNET_request_new_group(string zone, short playerId)
        {
            if (!Networking.IsMaster)
                return;
        
            _usppNetEveryPlayerManager.groupManager.RemovePlayerFromGroups(playerId);
            var group = _usppNetEveryPlayerManager.groupManager.GetJoinableGroup(zone);
            _usppNetEveryPlayerManager.groupManager.AddPlayerToGroup(playerId, group);
        
            Debug.Log($"zone: {zone}, player: {playerId}");
        }
    
        private void USPPNET_request_remove_from_group(short playerId)
        {
            if (!Networking.IsMaster)
                return;
        
            _usppNetEveryPlayerManager.groupManager.RemovePlayerFromGroups(playerId);
        
            Debug.Log($"player: {playerId} removed from group");
        }

        private void USPPNET_request_leave_callback(short playerId)
        {
            if (Networking.LocalPlayer.playerId == playerId)
                do_leave_callback();
        }

        private void GenericSet(short group, string varName, int netId, object var)
        {
            if ((group == -1 || group != groupManager.local_group) && group != -2)
                return;
            if (!syncManager.syncedCustomObjects.TryGetValue(netId, out var data))
                return;
            var obj = (GroupCustomSync)data.Reference;

            if (!Utilities.IsValid(obj))
            {
                syncManager.syncedCustomObjects.Remove(data);
                return;
            }

            obj.SetProgramVariable(varName, var);
        }
    
        private void USPPNET_CustomSet_int(short group, string varName, int netId, int var)
        {
            GenericSet(group, varName, netId, var);
        }
        private void USPPNET_CustomSet_string(short group, string varName, int netId, string var)
        {
            GenericSet(group, varName, netId, var);
        }
        private void USPPNET_CustomSet_bool(short group, string varName, int netId, bool var)
        {
            GenericSet(group, varName, netId, var);
        }
        private void USPPNET_CustomSet_float(short group, string varName, int netId, float var)
        {
            GenericSet(group, varName, netId, var);
        }
        private void USPPNET_CustomSet_Vector2(short group, string varName, int netId, Vector2 var)
        {
            GenericSet(group, varName, netId, var);
        }
        private void USPPNET_CustomSet_Vector3(short group, string varName, int netId, Vector3 var)
        {
            GenericSet(group, varName, netId, var);
        }
        private void USPPNET_CustomSet_Vector4(short group, string varName, int netId, Vector4 var)
        {
            GenericSet(group, varName, netId, var);
        }
        private void USPPNET_CustomSet_Quaternion(short group, string varName, int netId, Quaternion var)
        {
            GenericSet(group, varName, netId, var);
        }
        private void USPPNET_CustomSet_GroupCustomSync(short group, string varName, int netId, int networkId)
        {
            if (!syncManager.syncedCustomObjects.TryGetValue(networkId, out var data))
            {
                Debug.LogError($"couldn't get <color=red>{networkId}</color>?");
                return;
            }
            GenericSet(group, varName, netId, (GroupCustomSync)data.Reference);
        }

        private void USPPNET_CustomRPC(short group, string eventName, int netId)
        {
            if ((group == -1 || group != groupManager.local_group) && group != -2 )
                return;
            if (!syncManager.syncedCustomObjects.TryGetValue(netId, out var data))
                return;
            var obj = (GroupCustomSync)data.Reference;
            
            if (!Utilities.IsValid(obj))
            {
                syncManager.syncedCustomObjects.Remove(data);
                return;
            }

            obj.SendCustomEvent(eventName);
        }


        // ReSharper disable Unity.PerformanceAnalysis
        public void SetRemoteVar(short group, string varName, int netId, object var, bool setLocally = true)
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

        public void RemoteFunctionCall(short group, string eventName, int netId, bool callLocally = true)
        {
            USPPNET_CustomRPC(group, eventName, netId);
        
            if (!callLocally) return;
            if (!syncManager.syncedCustomObjects.TryGetValue(netId, out var data))
                return;
        
            var obj = (GroupCustomSync)data.Reference;
            
            if (!Utilities.IsValid(obj))
            {
                syncManager.syncedCustomObjects.Remove(data);
                return;
            }

            obj.SendCustomEvent(eventName);
        }

        // Master 
        private void USPPNET_close_group_joins(short group)
        {
            if (!Networking.IsMaster)
                return;

            groupManager.DisableJoinGroup(group);
            Debug.Log($"Group: {group} joins closed.");
        }

        // Client
        public void close_group_joinings(short group)
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
            var playerId = (short)Networking.LocalPlayer.playerId;
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
        public void request_leave_callback(short playerId)
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

        public void request_remove_from_group(short playerId)
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

        private bool _requestedSerialization;
        public new void RequestSerialization()
        {
            if (_requestedSerialization)
                return;
            if (!Networking.IsOwner(gameObject))
                return;
            
            SendCustomEventDelayedSeconds(nameof(ResetSerialization), 0.01666f);
            _requestedSerialization = true;
            base.RequestSerialization();
        }
        
        public void ResetSerialization()
        {
            _requestedSerialization = false;
        }

        private void INTSetOwner(int targetOwner)
        {
            if (targetOwner != Networking.LocalPlayer.playerId)
                return;
            
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            owned = true;

            _usppNetEveryPlayerManager.local_object = this;
            
            base.RequestSerialization();
        }
        
        public void USPPNET_SetOwner(int targetOwner)
        {
            INTSetOwner(targetOwner);
        }

        public void SetOwner(VRCPlayerApi player)
        {
            if (!Networking.IsOwner(gameObject))
            {
                Debug.LogError("Setter wasn't owner!");
                return;
            }
            
            owned = true;
            var playerId = player.playerId;
            
            USPPNET_SetOwner(playerId);
            INTSetOwner(playerId);
            RequestSerialization();
        }

        public void Bootstrap()
        {
            _usppNetEveryPlayerManager = GetComponentInParent<USPPNetEveryPlayerManager>();

        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (!Networking.IsMaster)
                return;
            if (playerManager.local_object == this)
                return;
        
            if (Networking.IsOwner(gameObject))
                owned = false;
            
            base.RequestSerialization();
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