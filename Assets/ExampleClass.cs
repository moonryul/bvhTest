using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExampleClass : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
         string[] boneName = HumanTrait.BoneName;
        for (int i = 0; i < HumanTrait.BoneCount; ++i)
        {
            Debug.Log(boneName[i]);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
