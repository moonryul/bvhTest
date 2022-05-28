using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BVHAnimationLoader))]   // This Editor object will refer to an instance of BVHAnimationLoader script
public class BVHAnimationLoaderEditor : Editor {
    public override void OnInspectorGUI() { // Implement this function to make a custom inspector.
        DrawDefaultInspector();

        BVHAnimationLoader bvhLoader = (BVHAnimationLoader) this.target;
        //    In Editor class: 
        //     The object being inspected.
        //    public UnityEngine.Object target { get; set; }
        //

        if (GUILayout.Button("Load animation")) {
            bvhLoader.parseFile();
            bvhLoader.loadAnimation(); 
            Debug.Log("Loading animation done.");
        }

        if (GUILayout.Button("Play animation")) {
            bvhLoader.playAnimation();
            Debug.Log("Playing animation.");
        }

        if (GUILayout.Button("Stop animation")) {
            Debug.Log("Stopping animation.");
            bvhLoader.stopAnimation();
        }

        if (GUILayout.Button("Initialize renaming map with humanoid bone names")) {
            HumanBodyBones[] bones = (HumanBodyBones[])Enum.GetValues(typeof(HumanBodyBones));
            // bvhToUnityRenamingMapArray 
            // Hips = 0;.... LastBone = 55
            bvhLoader.bvhToUnityRenamingMapArray = new BVHAnimationLoader.FakeDictionary[bones.Length - 1];
            for (int i = 0; i < bones.Length - 1; i++) {
                if (bones[i] != HumanBodyBones.LastBone) {
                    bvhLoader.bvhToUnityRenamingMapArray[i].bvhName = "";
                    bvhLoader.bvhToUnityRenamingMapArray[i].targetName = bones[i].ToString();
                }
            }
        }
    }
}
