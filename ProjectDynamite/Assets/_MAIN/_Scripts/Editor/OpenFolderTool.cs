using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

public class OpenFolderTool
{
    [OnOpenAsset]
    public static bool OnOpenAsset(int instanceId)
    {
        Event es = Event.current;

        if (es == null || !es.shift)
        {
            return false;
        }

        Object obj = EditorUtility.InstanceIDToObject(instanceId);
        string path = AssetDatabase.GetAssetPath(obj);

        if (AssetDatabase.IsValidFolder(path))
        {
            EditorUtility.RevealInFinder(path);
        }

        return true;
    }
}