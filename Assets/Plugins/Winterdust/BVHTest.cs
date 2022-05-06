using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Winterdust;
public class BVHTest : MonoBehaviour
{
    // Start is called before the first frame update

    BVH myBvh;
    void Start()
    {
      
     myBvh = new BVH("D:/Dropbox/metaverse/bvhTest/Assets/bvhFile.bvh"); 
     GameObject skeletonGO = myBvh.makeDebugSkeleton();
     // This line creates an animated skeleton from the BVH instance, visualized as a stick figure.
     //  makeSkeleton() does the same thing, except it isn't visualized/animated by default.
    // AnimationClip clip = myBvh.makeAnimationClip();

//     This line just creates an AnimationClip. By default it has legacy set to true but you can turn that off (either later or directly in the method call).
// That means you can use the AnimationClip both in the Animation component (legacy) and the Animator component (Mecanim).
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
