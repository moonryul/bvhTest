using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WriteText : MonoBehaviour
{
    private Text _text;

    private void Awake()
    {
        this._text = GetComponent<Text>();
    }

    public void GetText(string text)
    {
        string a = text.Substring(9, text.Length - 11);
        _text.text += a;
        Debug.Log("Success Reading!");
    }
}
