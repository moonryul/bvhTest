using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System;



public class RealTime_MicGet : MonoBehaviour
{
    private AudioSource audioSource; // The audio source to play the recorded audio from mic
    private AudioClip micAudioClip; // the Audio clip for the Microphone

    private int lastSamplePos = 0;
    private float[] samplesPerDeltaTime = null;
    private List<float> readSampleList = null;
    private float[]  read_samples = null;

    private float[]  write_samples = null;
    private int channels = 0;
    private int sampleRate = 22050;
    private int maxRecordDuration = 100; // seconds

   
    private int   samplesNumForCurrentFrame;

    private int readUpdateId = 0;
    private int previousReadUpdateId = -1;

    private float WRITE_FLUSH_TIME = 0.5f;
    public float READ_FLUSH_TIME = 0.5f;

   // public float READ_FLUSH_TIME = 1.0f;
    private float writeFlushTimer = 0.0f;
    private float readFlushTimer = 0.0f;
    
    public string micName ="마이크 (2- USBFC1)"; //MJ: this mic is selected as the default mic for zoom

    void Awake()
    {
      {
    int deviceCount = Microphone.devices.Length;
    for (int i = 0; i < deviceCount; i++)
    {
        Debug.Log("Microphone " + i + ": " + Microphone.devices[i]);
    }
}
    }
    void Start()
    {
      
        //writeSamples = new List<float>(1024);

        this.channels = 1;
        
        this.readSampleList  = new  List<float>();
        
        this.audioSource = this.gameObject.GetComponent<AudioSource>();

        //this.micName = "마이크 (Logi C270 HD WebCam)";
       
        //this.micAudioClip = Microphone.Start("마이크 (Logi C270 HD WebCam)", true, this.maxRecordDuration, this.sampleRate);

        this.micAudioClip = Microphone.Start(this.micName, true, this.maxRecordDuration, this.sampleRate);
        ////loops every this.maxRecordDuration second, capturing the audio and overwriting the previous clip
        //https://csharp.hotexamples.com/examples/-/Microphone/GetPosition/php-microphone-getposition-method-examples.html
              
        //this.channels = micAudioClip.channels;//mono or stereo, for me it's 1 (k)

    //    this.samplesNumPerDeltaTime = (int)(this.channels * this.fixedDelta * this.sampleRate);
    //    this.samplesPerDeltaTime = new float[this.samplesNumPerDeltaTime]; // amount of audio samples for the fixed DeltaTime

    // // Stop recording
     //       microphone.Stop();
    }

    // Update is called once per frame
   // void FixedUpdate() //MJ: called at the fixed frame interval
   void Update()
    {
        ReadMic();
        PlayMic();


        // MJ: for Debugging purpose:
        //this.audioSource.clip = this.micAudioClip;
        
      
        //if (!this.audioSource.isPlaying){
        //     Debug.Log("Play the audio obtain at the current frame");
        //     this.audioSource.Play();
        // }
    }

    
    private void ReadMic()
    {
        this.writeFlushTimer += Time.deltaTime;
        //this.writeFlushTimer += Time.fixedDeltaTime;


        int pos = Microphone.GetPosition(this.micName);  
        //int pos = Microphone.GetPosition( null);      
        // https://csharp.hotexamples.com/examples/-/Microphone/GetPosition/php-microphone-getposition-method-examples.html
           // MJ: Get the last position of the audio samples buffer that are recorded so far
           // This position will go back to the start the buffer when the maximum length of the buffer is reached.
           //  The maximum length of the buffer is defined by this.maxRecordDuration, this.sampleRate.

        int samplesNumToRead = pos - this.lastSamplePos;

        //this.samplesNumForCurrentFrame = (int)(this.channels * this.fixedDelta * this.sampleRate);
        //this.samplesPerDeltaTime = new float[samplesNumToRead]; // amount of audio samples for the fixed DeltaTime


        //Debug.LogFormat("lastSamplePos:{0}, pos:{1}, SamplesNumToRead:{2} ", this.lastSamplePos, pos, samplesNumToRead);


        if (samplesNumToRead > 0)
        {

           //AudioClip.samples tells how many samples there are in the audioclip.
           // AudioClip.GetData fills an array (passed as a parameter) with the samples that makes up the audioclip.
            
			read_samples = new float[samplesNumToRead  * channels];
            this.micAudioClip.GetData( read_samples, this.lastSamplePos); // Read the recorded audio buffer starting from this.lastSamplePos
                                                                           // to the end of the recorded audio buffer 
            // After  this.micAudioClip.GetData( read_samples, this.lastSamplePos), the audio recording buffer can be overwritten
            // when wrapping to the first position, because all audio samples recorded are immediately read off.

            this.readSampleList.AddRange(	read_samples );//readSampleList gonna be converted to an audio clip and be played (k)

            this.lastSamplePos = pos;



        }   // if (SamplesNumToRead > 0)
        // If    samplesNumToRead = pos - this.lastSamplePo is 0, then do nothing.
    }    //    private void ReadMic()


    private void PlayMic()
    {

        this.readFlushTimer += Time.deltaTime;
        //this.readFlushTimer += Time.fixedDeltaTime;

        if (this.readFlushTimer > READ_FLUSH_TIME) //1.0f (k); Try to play the audio when it is accumulated long enough, at least 1 second in our experiment
        {
            if (this.readUpdateId != this.previousReadUpdateId && this.readSampleList != null && this.readSampleList.Count > 0)
            {
                Debug.Log("Read happened");
                this.previousReadUpdateId = this.readUpdateId;  // -1 => 0


                this.audioSource.clip = AudioClip.Create("Real_time", this.readSampleList.Count, this.channels, this.sampleRate, false);
               
                this.audioSource.spatialBlend = 0;//2D sound

                this.audioSource.clip.SetData(this.readSampleList.ToArray(), 0);

                Debug.Log( $"this.audioSource.isPlaying={this.audioSource.isPlaying}");

                if (!this.audioSource.isPlaying)
                {
                    Debug.Log("Play! Play! Play! Play!");
                    this.audioSource.Play();
                }

                this.readSampleList.Clear();
                this.readUpdateId++;
            }

            this.readFlushTimer = 0.0f;
        }

        else 
        {
            //  Debug.Log($"readFlushTimer ({this.readFlushTimer}) > READ_FLUSH_TIME ({READ_FLUSH_TIME})");
            //  Debug.Log("no Play! no Play! NO Play! no Play!");

        }
    }
}
