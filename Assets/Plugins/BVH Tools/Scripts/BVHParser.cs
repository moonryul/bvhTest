using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// This class uses no Unity data types and should be completely safe to use in another thread
public class BVHParser {
    public int frames = 0;
    public float frameTime = 1000f / 60f;
    public BVHBone bvhRootNode;
    private List<BVHBone> boneList;

    static private char[] charMap = null;
    private float[][] channels_bvhParser; // the first index ranges over the total number of channels, DOFs, in MOTION part of the bvh file.
                                          // the second index ranges over the total number of frames in the MOTION.
    private string bvhText;
    private int pos = 0;
    
    // Inner class BVHBone of BVHParser  
    public class BVHBone {
        public string name;
        public List<BVHBone> children;
        public float offsetX, offsetY, offsetZ; // for each bone/joint
        public int[] channelOrder;
        public int channelNumber;
        public BVHChannel[] channels_bvhBones;

        private BVHParser bp;

        // 0 = Xpos, 1 = Ypos, 2 = Zpos, 3 = Xrot, 4 = Yrot, 5 = Zrot

        //  a C# struct is associated with value-type semantic and a value-type is not required to have a constructor.
        // It calls the default parameterless constructor of the struct, which initializes all the members to their default value of the specified data type.
        public struct BVHChannel {
            public bool enabled; // the default value of a boolean is false
            public float[] values; // the frame values for each channel
        }

        public BVHBone(BVHParser parser, bool isRoot) {
            this.bp = parser; // this refers to an instance of BVHBone

            this.bp.boneList.Add(this); // because BVHBone constructor is called recursively, the bone referred to by "this"
                                        // will be added to this.bp.boneList, creating the total number of bones. 

            this.channels_bvhBones = new BVHChannel[6];
            this.channelOrder = new int[6] { 0, 1, 2, 5, 3, 4 };

            this.children = new List<BVHBone>();

            this.bp.skip();
            if (isRoot) {
                this.bp.assureExpect("ROOT");
            } else {
                this.bp.assureExpect("JOINT");
            }
            bp.assure("joint name", bp.getString(out name));
            bp.skip();
            bp.assureExpect("{");
            bp.skip();
            bp.assureExpect("OFFSET");
            bp.skip();
            bp.assure("offset X", bp.getFloat(out offsetX));
            bp.skip();
            bp.assure("offset Y", bp.getFloat(out offsetY));
            bp.skip();
            bp.assure("offset Z", bp.getFloat(out offsetZ));
            bp.skip();
            bp.assureExpect("CHANNELS");

            bp.skip();
            bp.assure("channel number", bp.getInt(out this.channelNumber));
            bp.assure("valid channel number", this.channelNumber >= 1 && this.channelNumber <= 6);

            for (int i = 0; i < this.channelNumber; i++) {
                bp.skip();
                int channelId;
                bp.assure("channel ID", bp.getChannel(out channelId));
                this.channelOrder[i] = channelId;
                this.channels_bvhBones[channelId].enabled = true; // // the default value of a boolean is false
            }

            char peek = ' ';
            do {
                float ignored;
                bp.skip();
                bp.assure("child joint", bp.peek(out peek));
                switch (peek) {
                    case 'J':
                        BVHBone child = new BVHBone(bp, false); // recursive call to BVHBone constructor with rootBoneTransform = false
                        // create the children nodes of the current bone, this.
                        this.children.Add(child);
                        break;
                    case 'E':
                        bp.assureExpect("End Site");
                        bp.skip();
                        bp.assureExpect("{");
                        bp.skip();
                        bp.assureExpect("OFFSET");
                        bp.skip();
                        bp.assure("end site offset X", bp.getFloat(out ignored));
                        bp.skip();
                        bp.assure("end site offset Y", bp.getFloat(out ignored));
                        bp.skip();
                        bp.assure("end site offset Z", bp.getFloat(out ignored));
                        bp.skip();
                        bp.assureExpect("}");
                        break;
                    case '}':
                        bp.assureExpect("}");
                        break;
                    default:
                        bp.assure("child joint", false);
                        break;
                }
            } while (peek != '}');
        }
    } // public class BVHBone

    private bool peek(out char c) {
        c = ' ';
        if (this.pos >= this.bvhText.Length) { // this refers to an instance BVHParser
            return false;
        }
        c = this.bvhText[this.pos];
        return true;
    }

    private bool expect(string text) {
        foreach (char c in text) {
            if (pos >= bvhText.Length || (c != bvhText[pos] && bvhText[pos] < 256 && c != charMap[bvhText[pos]])) {
                return false;
            }
            pos++;
        }
        return true;
    }

    private bool getString(out string text) {
        text = "";
        while (pos < bvhText.Length && bvhText[pos] != '\n' && bvhText[pos] != '\r') {
            text += bvhText[pos++];
        }
        text = text.Trim();

        return (text.Length != 0);
    }

    private bool getChannel(out int channel) {
        channel = -1;
        if (pos + 1 >= bvhText.Length) {
            return false;
        }
        switch (bvhText[pos]) {
            case 'x':
            case 'X':
                channel = 0;
                break;
            case 'y':
            case 'Y':
                channel = 1;
                break;
            case 'z':
            case 'Z':
                channel = 2;
                break;
            default:
                return false;
        }
        pos++;
        switch (bvhText[pos]) {
            case 'p':
            case 'P':
                pos++;
                return expect("osition");
            case 'r':
            case 'R':
                pos++;
                channel += 3;
                return expect("otation");
            default:
                return false;
        }
    }

    private bool getInt(out int v) {
        bool negate = false;
        bool digitFound = false;
        v = 0;

        // Read sign
        if (pos < bvhText.Length && bvhText[pos] == '-') {
            negate = true;
            pos++;
        } else if (pos < bvhText.Length && bvhText[pos] == '+') {
            pos++;
        }

        // Read digits
        while (pos < bvhText.Length && bvhText[pos] >= '0' && bvhText[pos] <= '9') {
            v = v * 10 + (int)(bvhText[pos++] - '0');
            digitFound = true;
        }

        // Finalize
        if (negate) {
            v *= -1;
        }
        if (!digitFound) {
            v = -1;
        }
        return digitFound;
    }

    // Accuracy looks okay
    private bool getFloat(out float v) {
        bool negate = false;
        bool digitFound = false;
        //int i = 0; MJ: i is not used.
        v = 0f;

        // Read sign
        if (pos < bvhText.Length && bvhText[pos] == '-') {
            negate = true;
            pos++;
        } else if (pos < bvhText.Length && bvhText[pos] == '+') {
            pos++;
        }

        // Read digits before decimal point
        while (pos < bvhText.Length && bvhText[pos] >= '0' && bvhText[pos] <= '9') {
            v = v * 10 + (float)(bvhText[pos++] - '0');
            digitFound = true;
        }

        // Read decimal point
        if (pos < bvhText.Length && (bvhText[pos] == '.' || bvhText[pos] == ',')) {
            pos++;

            // Read digits after decimal
            float fac = 0.1f;
            //while (pos < bvhText.Length && bvhText[pos] >= '0' && bvhText[pos] <= '9' && i < 128) {
            while (pos < bvhText.Length && bvhText[pos] >= '0' && bvhText[pos] <= '9') { // MJ: removed i not used
                v += fac * (float)(bvhText[pos++] - '0'); 
                // A char is an integral type. It is NOT a character, it is a number! 'a' is just shorthand for a number.
                // So adding two character results in a number.
                fac *= 0.1f;
                //digitFound = true; // Checking if a digit is found is postponed later
            }
        }

        //MJ: check if the char at bvhText[pos] is "E", so that the remaining digits represent the exponent of the "E" notation (scientific notation)
        // bvhText is a string that contains the whole data of the bvh file, without the end of file character (?)
        // first check if there is no character in bvhText[pos]; this is the case when pos == bvhText.Length, whbich means that we have read all characters from the bvhText string.
        // If that happens at this point, it means the number parsed is float, not a scientific notation.
        if ( pos == bvhText.Length ) {
            // do nothing here
            digitFound = true;
        }
        else { // pos <  bvhText.Length 
        // there are more characters at pos and thereafter in bvhText
          if ( bvhText[pos] == 'E' || bvhText[pos] == 'e') { //   // get the exponent after 'e' or 'E'
          
            pos++;
            int exp; 
            bool intFound;
            intFound = getInt( out exp);
            if (!intFound) { // the integer representing the exponent is not found. Error
                digitFound = false; // if the exponent is not found, the current number is invalid, the digit is not found.
            }
            else { // comput the value using the exponent

              v = v * Mathf.Pow(10f, (float)exp); // v = v * 10^{exp}; (float)exp casts exp to float
              digitFound = true;

            }
          } // if ( bvhText[pos] == 'E' || bvhText[pos] == 'e')
          else { // not  ( bvhText[pos] == 'E' || bvhText[pos] == 'e') ==> The character other than 'E' or 'e' found after the float number. This is an error
            
              digitFound = true;
          }
        
        } // // pos <  bvhText.Length 

        // Finalize
      
        if (!digitFound) { // the digit not found, so assing flaot.NaN to v
            v = float.NaN;
        }
        else { 
          if (negate) {
            v *= -1f;
          }
        }
        
        return digitFound;
    }

    private void skip() {
        while (pos < bvhText.Length && (bvhText[pos] == ' ' || bvhText[pos] == '\t' || bvhText[pos] == '\n' || bvhText[pos] == '\r')) {
            pos++;
        }
    }

    private void skipInLine() { // skip empty characters in the given line
        while (pos < bvhText.Length && (bvhText[pos] == ' ' || bvhText[pos] == '\t')) { // '\b" = tab control character
            pos++;
        }
    }

    private void newline() {
        bool foundNewline = false;
        skipInLine();
        while (pos < bvhText.Length && (bvhText[pos] == '\n' || bvhText[pos] == '\r')) {
            foundNewline = true;
            pos++;
        }
        assure("newline", foundNewline);
    }

    private void assure(string what, bool result) {
        if (!result) {
            string errorRegion = "";
            for (int i = Math.Max(0, pos - 15); i < Math.Min(bvhText.Length, pos + 15); i++) {
                if (i == pos - 1) {
                    errorRegion += ">>>";
                }
                errorRegion += bvhText[i];
                if (i == pos + 1) {
                    errorRegion += "<<<";
                }
            }
            throw new ArgumentException("Failed to parse BVH data at position " + pos + ". Expected " + what + " around here: " + errorRegion);
        }
    }

    private void assureExpect(string text) {
        assure(text, expect(text));
    }

    /*private void tryCustomFloats(string[] floats) {
        float total = 0f;
        foreach (string f in floats) {
            pos = 0;
            bvhText = f;
            float v;
            getFloat(out v);
            total += v;
        }
        Debug.Log("Custom: " + total);
    }

    private void tryStandardFloats(string[] floats) {
        IFormatProvider fp = CultureInfo.InvariantCulture;
        float total = 0f;
        foreach (string f in floats) {
            float v = float.Parse(f, fp);
            total += v;
        }
        Debug.Log("Standard: " + total);
    }

    private void tryCustomInts(string[] ints) {
        int total = 0;
        foreach (string i in ints) {
            pos = 0;
            bvhText = i;
            int v;
            getInt(out v);
            total += v;
        }
        Debug.Log("Custom: " + total);
    }

    private void tryStandardInts(string[] ints) {
        IFormatProvider fp = CultureInfo.InvariantCulture;
        int total = 0;
        foreach (string i in ints) {
            int v = int.Parse(i, fp);
            total += v;
        }
        Debug.Log("Standard: " + total);
    }

    public void benchmark () {
        string[] floats = new string[105018];
        string[] ints = new string[105018];
        for (int i = 0; i < floats.Length; i++) {
            floats[i] = UnityEngine.Random.Range(-180f, 180f).ToString();
        }
        for (int i = 0; i < ints.Length; i++) {
            ints[i] = ((int)Mathf.Round(UnityEngine.Random.Range(-180f, 18000f))).ToString();
        }
        tryCustomFloats(floats);
        tryStandardFloats(floats);
        tryCustomInts(ints);
        tryStandardInts(ints);
    }*/

    private void parse(bool overrideFrameTime, float time) {
        // Prepare character table
        if (charMap == null) { // charMap is static
            charMap = new char[256];
            for (int i = 0; i < 256; i++) {
                if (i >= 'a' && i <= 'z') {
                    charMap[i] = (char)(i - 'a' + 'A');
                } else if (i == '\t' || i == '\n' || i == '\r') {
                    charMap[i] = ' ';
                } else {
                    charMap[i] = (char)i;
                }
            }
        }

        // Parse skeleton
        skip();
        assureExpect("HIERARCHY");
       // MJ: Create a hiearchy of the character
        this.boneList = new List<BVHBone>(); // this refers to an instance of  BVHParser

        this.bvhRootNode = new BVHBone(this, true); // true = the bone to parse is the root of the bvh hierarchy;
                                                    // It will call BVHBone() recursively for the childen bones.
        // new BVHBone() creates an instance of BVHBone and returns it
        // Parse meta data
        skip();
        assureExpect("MOTION");
        skip();
        assureExpect("FRAMES:");
        skip();
        assure("frame number", getInt(out this.frames)); // MJ: The number of frames in the motion data
        skip();
        assureExpect("FRAME TIME:");
        skip();
        assure("frame time", getFloat(out this.frameTime));

        if (overrideFrameTime) {
            this.frameTime = time;
        }

        // Prepare channels
        int totalChannels = 0; // the total degrees of freedom for the body.
        foreach (BVHBone bone in this.boneList) {
            
            totalChannels += bone.channelNumber; // add all the channel number  of each bone to totalChannels.
            // CHANNELS 6 Xposition Yposition Zposition Zrotation Xrotation Yrotation  
            // CHANNELS 3 Zrotation Xrotation Yrotation
        }
        int channel = 0; // channel = DOF

        this.channels_bvhParser = new float[totalChannels][];

        // Allocate the data structure to which the parsed data will be stored.
        foreach (BVHBone bone in this.boneList) { // this.boneList has the total number of bones

            for (int i = 0; i < bone.channelNumber; i++) {
                this.channels_bvhParser[channel] = new float[frames]; // channel ranges over the total number of channels in the MOTION data

                bone.channels_bvhBones[bone.channelOrder[i]].values =this.channels_bvhParser[channel++]; // chanel++ for each increment of i
                //  this.channelOrder = new int[6] { 0, 1, 2, 5, 3, 4 };

                 // This statement makes the left and right hand variables refer  to the same reference,
                 // so that assigning values to the right hand side makes the left hand side refer to those values.
                 // When displaying the motion data, bone.channels_bvhBones[bone.channelOrder[i]].values will be used,
                 // in loadAnimation() in BVHAnimationLoader.cs
                 

            }
        }
        
        // Parse frames
        for (int i = 0; i < this.frames; i++) { // get the DOF data for each frame line in the motion data part
            newline();
            for (channel = 0; channel < totalChannels; channel++) {
                skipInLine(); // skip empty characters in the current line, increasing pos

                assure("channel value", getFloat(out this.channels_bvhParser[channel][i])); // channel = DOF; i = each frame in the motion
               
            }
        }
    } // private void parse(bool overrideFrameTime, float time)

    public BVHParser(string bvhText) { //MJ:  bvhText is a string that contains the whole data of the bvh file, without the end of file character (?)
        this.bvhText = bvhText; // this refers to an instance of BVHParser

        this.parse(false, 0f); // bp.channels[][] and bone.channels[].values will store the all the DOF data in all the frames in bvhText;
        
    }

    public BVHParser(string bvhText, float time) {
        this.bvhText = bvhText;

        this.parse(true, time);
    }
} // public class BVHParser
