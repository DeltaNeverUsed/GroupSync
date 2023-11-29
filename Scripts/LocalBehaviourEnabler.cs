using System;
using GroupSync.Extensions;
using UdonSharp;
using VRC.SDKBase;

namespace GroupSync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LocalBehaviourEnabler : UdonSharpBehaviour
    {
        public GroupObjectSync objectSync;
        
        public UdonSharpBehaviour[] excluded = Array.Empty<UdonSharpBehaviour>();
        private UdonSharpBehaviour[] _compList = Array.Empty<UdonSharpBehaviour>();
        

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

        private bool _started;
        public void StartWithGroupSync()
        {
            if (_started)
                return;
            _started = true;
            
            if (!Utilities.IsValid(objectSync))
                objectSync = GetComponent<GroupObjectSync>();
            if (Utilities.IsValid(objectSync))
                AddExclusion(objectSync);

            AddExclusion(this);
            var objectComps = GetComponents<UdonSharpBehaviour>();
            foreach (var comp in objectComps)
            {
                if (!excluded.Contains(comp))
                    _compList = _compList.Add(comp);
            }
        }

        public void EnableComps()
        {
            foreach (var comp in _compList)
            {
                //this.Log("<color=green>Enabling</color>: " + comp.GetUdonTypeName() + " on: " + gameObject.name);
                comp.enabled = true;
            }
        }
        public void DisableComps()
        {
            foreach (var comp in _compList)
            {
                //this.Log("<color=red>Disabling</color>: " + comp.GetUdonTypeName() + " on: " + gameObject.name);
                comp.enabled = false;
            }
        }
    }
}
