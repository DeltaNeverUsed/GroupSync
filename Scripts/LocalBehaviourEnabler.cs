using System;
using GroupSync.Extensions;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace GroupSync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LocalBehaviourEnabler : UdonSharpBehaviour
    {
        public GroupObjectSync objectSync;
        
        public UdonSharpBehaviour[] excluded = { };
        private UdonSharpBehaviour[] _compList = { };
    
        

        public void AddExclusion(UdonSharpBehaviour behaviour)
        {
            excluded = excluded.Add(behaviour);
        }
    
        public void RemoveExclusion(UdonSharpBehaviour behaviour)
        {
            var index = -1;
            for (var i = 0; i < excluded.Length; i++)
            {
                if (excluded[i] != behaviour) continue;
                index = i;
                break;
            }
        
            if (index == -1)
                return;

            var newArrayLen = excluded.Length - 1;
            var tempArray = new UdonSharpBehaviour[newArrayLen];
            Array.Copy(excluded, 0, tempArray, 0, index);
            Array.Copy(excluded, index+1, tempArray, index, newArrayLen-index);
            excluded = tempArray;
        }

        private void Start()
        {
            if (!Utilities.IsValid(objectSync))
            {
                objectSync = GetComponent<GroupObjectSync>();
            }
            if (!Utilities.IsValid(objectSync))
            {
                Debug.LogError("Couldn't get GroupObjectSync!");
                enabled = false;
                return;
            }
            
            excluded = excluded.Add(objectSync);
            excluded = excluded.Add(this);
            var objectComps = GetComponents<UdonSharpBehaviour>();
            foreach (var comp in objectComps)
            {
                if (excluded.Contains(comp))
                    return;
                _compList = _compList.Add(comp);
            }
        }

        public void EnableComps()
        {
            foreach (var comp in _compList)
                comp.enabled = true;
        }
        public void DisableComps()
        {
            foreach (var comp in _compList)
                comp.enabled = false;
        }
    }
}
