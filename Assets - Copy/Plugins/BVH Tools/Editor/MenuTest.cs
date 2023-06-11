// This script creates a new menu item Examples>Create Prefab in the main menu.
// Use it to create Prefab(s) from the selected GameObject(s).
// Prefab(s) are placed in the "Prefabs" folder.
using System.IO;
using UnityEngine;
using UnityEditor;

public class MenuTest: MonoBehaviour
{
    // Creates a new menu item 'My Menu > Create Prefab' in the main menu.
    //https://forum.unity.com/threads/save-mesh-as-prefab-with-mesh-renderer-material.619465/
    // https://docs.unity3d.com/ScriptReference/PrefabUtility.html
    //https://forum.unity.com/threads/material-is-lost-when-saving-a-prefab-from-script.906902/
    //
    //    The key is to use
    //Object prefab = PrefabUtility.CreatePrefab(localPath, obj);
    //    PrefabUtility.ReplacePrefab(obj, prefab, ReplacePrefabOptions.ConnectToPrefab);

    [MenuItem("MyMenu/Create Prefab")]
    static void CreatePrefab()
    {
        // Keep track of the currently selected GameObject(s)
        GameObject[] objectArray = Selection.gameObjects;

        // Loop through every GameObject in the array above
        foreach (GameObject gameObject in objectArray)
        {
            // Create folder Prefabs and set the path as within the Prefabs folder,
            // and name it as the GameObject's name with the .Prefab format
            if (!Directory.Exists("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            string localPath = "Assets/Prefabs/" + gameObject.name + ".prefab";

            // Make sure the file name is unique, in case an existing Prefab has the same name.
            localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

            // Create the new Prefab and log whether Prefab was saved successfully.
            bool prefabSuccess;
            PrefabUtility.SaveAsPrefabAsset(gameObject, localPath, out prefabSuccess);
            if (prefabSuccess == true)
                Debug.Log("Prefab was saved successfully");
            else
                Debug.Log("Prefab failed to save" + prefabSuccess);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    // Disable the menu item if no selection is in place.
    [MenuItem("MyMenu/Create Prefab", true)]
    static bool ValidateCreatePrefab()
    {
        return Selection.activeGameObject != null && !EditorUtility.IsPersistent(Selection.activeGameObject);
    }
}