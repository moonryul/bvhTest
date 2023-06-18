using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SetRECDevice : MonoBehaviour
{
    [SerializeField] private AudioClip _clip; 
    // The audio clip onto which the audio from the mic will be recorded; to be created by Microphone.Start()
    
    [SerializeField] private int lengthSec = 100;
    [SerializeField] private bool loop = true;
    [SerializeField] private ToggleGroup _toggleGroup_micList; //MJ: set this in inspector
    [SerializeField] private AudioSource _source; //MJ: We need to set AudioSource component to record from a mic; Set in the inspector
    // AudioSource component is added to "AudioSource" gameObject.
    [SerializeField] private int channels; //MJ: mono audio by default
    [SerializeField] private int _sampleRate = 44100;

    private float[] p_cutSamples; // This will be used in avatarController.

    private SaveAudio _csrAPI;
    private Text _buttonText;
    

    private void Awake()
    {
        this._csrAPI = this.gameObject.GetComponent<SaveAudio>(); //MJ: this.gameObect == "RecordButton" object under Canvas
        this._buttonText = this.gameObject.GetComponentInChildren<Text>(); //MJ: Text component attached to Text gameObject under RecordButton
    }

    private void Start()
    {

        //MJ: this._source = new GameObject("Mic").AddComponent<AudioSource>();
       
         //public static AudioClip Start(string deviceName, bool loop, int lengthSec, int frequency);
        //this._clip = Microphone.Start(selectToggle.name, this.loop, this.lengthSec, this._sampleRate); // 녹음 시작; MJ: Microphone.Start() creates an audio clip
        this._clip = Microphone.Start(Microphone.devices[0], this.loop, this.lengthSec, this._sampleRate);
        this.channels = this._clip.channels; //MJ: Get and Set the mic audio clip's number of channels.
    }

    private Toggle selectToggle
    {
        get { return this._toggleGroup_micList.ActiveToggles().FirstOrDefault(); } 
        // C#의 구문(LINQ)
        // 활성화된 토글을 검색하면서, 가장 처음으로 활성화된 토글을 반환한다.
        // First와 FirstOrDefault의 차이는, 반환 객체의 존재 유무이다. 
        // First는 반환객체가 없으면 오류가 발생하고, FirstOrDefault는 반환이 없어도 됨.
    }

    public void ShowSelectToggleName()  //MJ: SetRECDevice.ShowSelectToggleName is bound to RecordButton under Canvas gameObject 
    {
        if (!Microphone.IsRecording(selectToggle.name)) // 녹음 시작
        {
            this._buttonText.text = "녹음 종료";
            DeviceManager.instance.SetAllToggleInteracable(false); // 녹음을 시작하면 Toggle을 조작할 수 없도록 모든 토글을 비활성화하는 함수를 호출
            // //public static AudioClip Start(string deviceName, bool loop, int lengthSec, int frequency);
            // this._clip = Microphone.Start(selectToggle.name, this.loop, this.lengthSec, this._sampleRate); // 녹음 시작; MJ: Microphone.Start() creates an audio clip
            // this.channels = this._clip.channels; //MJ: Get and Set the mic audio clip's number of channels.
        }
        else // 녹음 종료
        {
            this._buttonText.text = "녹음 시작";
            DeviceManager.instance.SetAllToggleInteracable(true); // 녹음을 정지하면 다시 Toggle을 조작할 수 있도록 모든 토글을 활성화하는 함수를 호출

            int t_sIndex = Microphone.GetPosition(Microphone.devices[0]); // 현재까지 녹음된 sample의 index를 반환

            Microphone.End(selectToggle.name); // 녹음 종료
            
            float[] samples = new float[this._clip.samples]; // 원래 _clip만큼의 sample 수만큼 배열 공간 할당
            this._clip.GetData(samples, 0); // clip의 sample을 가져와서 저장

            this.p_cutSamples = new float[t_sIndex]; // 녹음된 index까지만큼의 sample 수만큼 배열 공간 할당
            Array.Copy(samples, this.p_cutSamples, this.p_cutSamples.Length - 1); // 복사 
            AudioClip _clip2 = AudioClip.Create("rec", this.p_cutSamples.Length, 1, 44100, false); // 새로운 AudioClip을 만듬
            _clip2.SetData(this.p_cutSamples, 0); // _clip2에 cutSample의 데이터 할당

            this._csrAPI.SaveAudioClip(_clip2);
        }
    }

    public float[] GetCutSamples()
    {
        return this.p_cutSamples;
    }
    public int GetSampleRate()
    {
        return this._sampleRate;
    }
}
