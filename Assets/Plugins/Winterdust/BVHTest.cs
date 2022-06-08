using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Winterdust;
using System;

using System.IO;
public class BVHTest : MonoBehaviour
{
    // Start is called before the first frame update
    // https://winterdust.itch.io/bvhimporterexporter

    public string  bvhFileName =""; 
    BVH myBvh;
    void Start()
    {
      
     if (bvhFileName == "") { 
         Debug.Log(" bvhFileName should be set in the inspector");
         throw new InvalidOperationException("No  Bvh FileName is set.");

     }
     myBvh = new BVH(bvhFileName); 
     //GameObject skeletonGO = myBvh.makeDebugSkeleton();
     // This line creates an animated skeleton from the BVH instance, visualized as a stick figure.
     //  makeSkeleton() does the same thing, except it isn't visualized/animated by default.
    // AnimationClip clip = myBvh.makeAnimationClip();

    GameObject skeletonGO = myBvh.makeDebugSkeleton(); // => Create the rest pose
   // public GameObject makeDebugSkeleton(bool animate = true, string colorHex = "ffffff", float jointSize = 1f, int frame = -1, bool xray = false, bool includeBoneEnds = true, string skeletonGOName = "Skeleton", bool originLine = false)

    // GameObject skeletonGO = myBvh.makeSkeleton();
    //==> public GameObject makeSkeleton(int frame = -1, bool includeBoneEnds = true, string skeletonGOName = "Skeleton", bool animate = false) => does NOT create an animation clip

    // public GameObject makeDebugSkeleton(bool animate = true, string colorHex = "ffffff", float jointSize = 1f, int frame = -1, bool xray = false, bool includeBoneEnds = true, string skeletonGOName = "Skeleton", bool originLine = false)


// Modified BVH instances can be written back to a new .bvh file so you can import it into for example Blender and work on it further.


// AnimationClip clip = myBvh.makeAnimationClip();
//==>  This line just creates an AnimationClip. By default it has legacy set to true.
//   But you can turn that off (either later or directly in the method call).
//   ==> It means you can use the AnimationClip both in the Animation component (legacy) and the Animator component (Mecanim).

// It's possible to make an Animator-compatible AnimationClip, which you can use in Mecanim via an Animator Override Controller.
//  But I recommend using the legacy Animation component for simplicity.

// BvhImporterExporter is delivered as a .dll file, everything is well documented with detailed XMLDOC descriptions.
//  A .xml file is included, when placed next to the .dll the documentation can be seen from inside your script editor.

// Most of the heavy work (actually importing the .bvh file) can be executed from a different thread if you want to make a preloader for your game. 
// BvhImporterExporter has been optimized so it's blazingly fast, able to import thousands of animation frames in a very short time.
  } // Start()

    // Update is called once per frame
    void Update()
    {
        
    }
}

// GameObject skeletonGO = myBvh.makeSkeleton(); => 
	// public GameObject makeSkeleton(int frame = -1, bool includeBoneEnds = true, string skeletonGOName = "Skeleton", bool animate = false)
	// 	{
	// 		GameObject gameObject = new GameObject(skeletonGOName);
			
	// 		for (int i = 0; i < this.boneCount; i++)
	// 		{
	// 			if (this.allBones[i].parentBoneIndex == -1)
	// 			{
	// 				this.allBones[i].makeGO(ref frame, ref includeBoneEnds, ref this.allBones, i).transform.parent = gameObject.transform;
	// 			}
	// 		}
	// 		if (animate)
	// 		{
	// 			BVH.animateSkeleton(gameObject, this.makeAnimationClip(0, -1, false, "", WrapMode.Loop, true, false, false),   1f);
	// 			//=> 	public static Animation animateSkeleton(GameObject skeletonGO, AnimationClip clip, float blendTimeSec = 1f)
	// 			//=>    gameObject( ==skeletonGO ).AddComponent<Animation>();
	// 		}
	// 		return gameObject;
	// 	}

