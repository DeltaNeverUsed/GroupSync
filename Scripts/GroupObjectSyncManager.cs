using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Data;

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
    }
}