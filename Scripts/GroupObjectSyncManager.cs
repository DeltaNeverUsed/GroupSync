using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace GroupSync
{
#if UNITY_EDITOR    
    [CustomEditor(typeof(GroupObjectSyncManager), true)]
    public class GroupObjectSyncManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Assign new IDs"))
            {
                var gcs = Resources.FindObjectsOfTypeAll<GroupCustomSync>();
                foreach (var obj in gcs)
                    obj.networkId = Random.Range(int.MinValue, int.MaxValue);
            }
        
            DrawDefaultInspector();
        }
    }
#endif

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GroupObjectSyncManager : UdonSharpBehaviour
    {
        [HideInInspector] public DataDictionary syncedCustomObjects = new DataDictionary();
        [HideInInspector] public DataList lateUpdateSubs;

        public int GetUnusedId()
        {
            var newId = Random.Range(int.MinValue, int.MaxValue);
            while (syncedCustomObjects.ContainsKey(newId) || newId == -1)
                newId = Random.Range(int.MinValue, int.MaxValue);
            return newId;
        }

        public int GetUnusedIdRange(int range)
        {
            var newId = 1000;
            var contains = true;
            while (contains)
            {
                newId = Random.Range(int.MinValue, int.MaxValue);
                contains = false;
                for (var i = 0; i < range; i++)
                {
                    if (!syncedCustomObjects.ContainsKey(newId + i) && newId + i != -1) continue;
                    contains = true;
                    break;
                }
            }
            return newId;
        }
    
        public void AddCustomObject(GroupCustomSync obj)
        {
            if (!syncedCustomObjects.ContainsKey(obj.networkId))
                syncedCustomObjects.Add(obj.networkId, obj);
            else
                Debug.LogError($"Duplicate ID on custom object: {obj.name}, ID is: {obj.networkId}");
        }
        public void RemoveCustomObject(GroupCustomSync obj)
        {
            if (!syncedCustomObjects.Remove(obj.networkId))
                Debug.LogError($"Couldn't find custom object: {obj.name}, ID is: {obj.networkId}");
        }
        
        public void SubPostLateUpdate(IUdonEventReceiver sync) {
            var tok = new DataToken(sync);
            if (lateUpdateSubs.Contains(tok))
                return;
            lateUpdateSubs.Add(tok);
        }
        public void UnSubPostLateUpdate(IUdonEventReceiver sync)
        {
            lateUpdateSubs.RemoveAll(new DataToken(sync));
        }

        public override void PostLateUpdate()
        {
            var subs = lateUpdateSubs.ToArray();
            foreach (var subRef in subs)
            {
                var sub = (IUdonEventReceiver)subRef.Reference;
                if (Utilities.IsValid(sub))
                    sub.SendCustomEvent("SubPostLateUpdate");
                else
                    lateUpdateSubs.RemoveAll(subRef);
            }
        }
    }
}