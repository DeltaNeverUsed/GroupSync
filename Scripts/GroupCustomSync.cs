using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace GroupSync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class GroupCustomSync : UdonSharpBehaviour
    {
        public int networkId = -1;
        public bool forceGlobalSync;
        [NonSerialized] public GroupObjectSyncManager gosm;
        [NonSerialized] public USPPNetEveryPlayerManager psm;
        [NonSerialized] public USPPNetEveryPlayer lpm;

        internal bool StartedNet;

        public virtual void Start()
        {
            StartNet();
        }

        public bool StartNet()
        {
            if (StartedNet)
                return true;
        
            if (networkId == -1)
            {
                Debug.LogError("networkId is -1 on " + gameObject.name +
                               "Please select to object to generate a new networkId, or create select one manually");
                return false;
            }        
            if (gosm == null)
            {
                gosm = GameObject.Find("GroupObjectSyncManager").GetComponent<GroupObjectSyncManager>();
                if (gosm == null)
                {
                    Debug.LogError("Couldn't find GroupObjectSyncManager");
                    return false;
                }
            }
            if (psm == null)
            {
                psm = GameObject.Find("EachPlayerUSPPNet").GetComponent<USPPNetEveryPlayerManager>();
                if (psm == null)
                {
                    Debug.LogError("Couldn't find USPPNetEveryPlayer");
                    return false;
                }
            }

            lpm = psm.local_object;
        
            gosm.AddCustomObject(this);
        
            StartedNet = true;
            return true;
        }

        public virtual void OnDestroy()
        {
            if (StartedNet)
            {
                gosm.RemoveCustomObject(this);
                gosm.UnSubPostLateUpdate(this);
            }
        }

        private bool _dontExists = true;
        private bool CheckLocalObject()
        {
            if (!StartedNet)
                return true;
            if (_dontExists)
            {
                if (!Utilities.IsValid(psm.local_object))
                    return true;
            }
            else
                return false;
            lpm = psm.local_object;
            _dontExists = false;
            return false;
        }
    
        public void SetVariableInAllGroups(string name, object value, bool setLocally = true, bool autoSerialize = true)
        {
            if (CheckLocalObject())
                return;
            SetVariable(-2, name, value, setLocally, autoSerialize);
        }
        public void CallFunctionInAllGroups(string name, bool callLocally = true, bool autoSerialize = true)
        {
            if (CheckLocalObject())
                return;
            CallFunction(-2, name, callLocally, autoSerialize);
        }

        public void SetVariableInLocalGroup(string name, object value, bool setLocally = true, bool autoSerialize = true)
        {
            if (CheckLocalObject())
                return;
            SetVariable(forceGlobalSync ? -2 : psm.groupManager.local_group, name, value, setLocally, autoSerialize);
        }
        public void CallFunctionInLocalGroup(string name, bool callLocally = true, bool autoSerialize = true)
        {
            if (CheckLocalObject())
                return;
            CallFunction(forceGlobalSync ? -2 : psm.groupManager.local_group, name, callLocally, autoSerialize);
        }

        private void CallFunction(int group, string name, bool callLocally = true, bool autoSerialize = true)
        {
            lpm.RemoteFunctionCall(group, name, networkId, callLocally);
            if (autoSerialize)
                lpm.RequestSerialization();
        }

        private void SetVariable(int group, string name, object value, bool setLocally = true, bool autoSerialize = true)
        {
            lpm.SetRemoteVar(group, name, networkId, value, setLocally);
            if (autoSerialize)
                lpm.RequestSerialization();
        }

        internal bool isLateSubbed;
        
        public void SubPostLateUpdateCallback()
        {
            if (isLateSubbed)
                return;
            isLateSubbed = true;
            gosm.SubPostLateUpdate(this);
        }
        public void UnSubPostLateUpdateCallback()
        {
            if (!isLateSubbed)
                return;
            isLateSubbed = false;
            gosm.UnSubPostLateUpdate(this);
        }
        
        public virtual void SubPostLateUpdate()
        {
            
        }

        /// <summary>
        /// The call back gets called whenever the group changed is request, and before the group is changed!
        /// </summary>
        protected void SubLeaveGroupCallback()
        {
            psm.groupManager.SubLeaveGroupCallback(this);
        }

        /// <summary>
        /// Locks the group so that new people can't join it.
        /// </summary>
        protected void CloseCurrentGroup()
        {
            if (CheckLocalObject())
                return;
            lpm.close_group_joinings(psm.groupManager.local_group);
        }
    
    }
}
