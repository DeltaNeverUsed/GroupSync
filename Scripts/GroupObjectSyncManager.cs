using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(GroupObjectSyncManager), true)]
public class GroupObjectSyncManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Assign new IDs"))
        {
            var gcs = Resources.FindObjectsOfTypeAll<GroupCustomSync>();
            foreach (var obj in gcs)
                obj.networkId = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }
        
        DrawDefaultInspector();
    }
}

#endif

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GroupObjectSyncManager : UdonSharpBehaviour
{
    [HideInInspector] public DataDictionary syncedCustomObjects = new DataDictionary();
    
    public void AddCustomObject(GroupCustomSync obj)
    {
        if (!syncedCustomObjects.ContainsKey(obj.networkId))
            syncedCustomObjects.Add(obj.networkId, obj);
        else
            Debug.LogError($"Duplicate ID on custom object: {obj.name}, ID is: {obj.networkId}");
    }
}