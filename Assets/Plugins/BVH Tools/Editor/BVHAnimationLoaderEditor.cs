using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BVHAnimationRetargetter))]   // This Editor object will refer to an instance of BVHAnimationLoader script
public class BVHAnimationLoaderEditor : Editor {
    public override void OnInspectorGUI() { // Implement this function to make a custom inspector.
        DrawDefaultInspector();

        BVHAnimationRetargetter bvhRetargetter = (BVHAnimationRetargetter) this.target;
        //    In Editor class: 
        //     The object being inspected.
        //    public UnityEngine.Object target { get; set; }
        //

        // if (GUILayout.Button("Load animation")) {
        //     bvhLoader.parseFile();
        //     bvhLoader.loadAnimation(); 
        //     Debug.Log("Loading animation done.");
        // }

        if (GUILayout.Button("Play animation")) {
            bvhRetargetter.playAnimation();
            Debug.Log("Playing animation.");
        }

        if (GUILayout.Button("Stop animation")) {
            Debug.Log("Stopping animation.");
            bvhRetargetter.stopAnimation();
        }

        
    }
}
