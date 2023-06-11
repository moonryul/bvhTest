using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{

        
    IEnumerator MainCo()
    {
        yield return SubCo1();
        Debug.Log("Sub1 is completed and MainCo running");

    }
    IEnumerator SubCo1()
    {
        yield return SubCo2();
    }
    IEnumerator SubCo2()
    {
        for (int i=0; i<5; i++)
        {
            yield return new WaitForSeconds(5);
            Debug.Log("WaiteForSeconds(5)  is completed");
                      
        }

        yield  break; 
        // what happens when this statement is not used? 
        //=> the  effect is the same whether you use yield break or not at this point.
        // It means that the default behavior at the end of a coroutine function is yield break.

    }
 
    Coroutine co;
    private void OnGUI()
    {
        if (GUILayout.Button("Start coroutine"))
            co = StartCoroutine(MainCo());
        if (GUILayout.Button("Stop coroutine"))
            StopCoroutine(co);
    }

  

    
    // Start is called before the first frame update
    void Start()
    {
         co = StartCoroutine(MainCo());
         Debug.Log("The MainCo  completed");

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
