using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.Networking;

public class STTAPI : MonoBehaviour
{
    public static STTAPI instance;
    public WriteText writetext;

    private void Awake()
    {
        instance = this;
    }

    private IEnumerator PostRecordData(string url, byte[] data)
    {
        // 유니티에서 HTTP 양식으로 구성된 서버에 POST할 때 사용하는 함수
        WWWForm form = new WWWForm();

        UnityWebRequest www = UnityWebRequest.Post(url, form);

        www.SetRequestHeader("X-NCP-APIGW-API-KEY-ID", "qhat4s91jn");
        www.SetRequestHeader("X-NCP-APIGW-API-KEY", "oM48E0sHtOVQf0QOAIkSFrdjTjygqlLEjCSHPk9l");
        www.SetRequestHeader("Content-Type", "application/octet-stream");

        www.uploadHandler = new UploadHandlerRaw(data);

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ProtocolError ||
            www.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log(www.error);
        }
        else
        {
            if (www.result == UnityWebRequest.Result.Success)
            {
                //Debug.Log(System.Text.Encoding.UTF8.GetString(www.downloadHandler.data));
                writetext.GetText(System.Text.Encoding.UTF8.GetString(www.downloadHandler.data));
            }
        }

        www.Dispose();
    }

    public void SendAudioSample(AudioClip _clip)
    {
        byte[] bytearray = SavWav.GetWav(_clip, out var length);
        StartCoroutine(PostRecordData("https://naveropenapi.apigw.ntruss.com/recog/v1/stt?lang=Kor",
            bytearray));
    }
}
