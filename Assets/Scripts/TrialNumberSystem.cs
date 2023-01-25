using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;

public class TrialNumberSystem : MonoBehaviour
{
    public string codedFileName;
    Boolean showTrialNum; // true if coded file name given
    string[,] seperatedCoded; // stores coded data file
    float[] startingTrialTimes; // stores time when each trial starts

    // Start is called before the first frame update
    void Start()
    {
         // read entire output coded file if entered
          string projectLocation = Application.dataPath;
        if (codedFileName != null) {
            showTrialNum = true;
            string inputFile = Path.Combine(projectLocation, "../Output/" + codedFileName+".csv");
            string[] lines = File.ReadAllLines(inputFile);
            seperatedCoded = new string[lines.Length,lines[0].Length];
            for (int i=0; i<lines.Length; i++) {
                string[] data = lines[i].Split("\t");
                for (int j=0; j<data.Length; j++) {
                    seperatedCoded[i,j] = data[j];
                }
            }

            // find start time of each trial
            int trialNum = 0;
            Debug.Log("lengh = " + seperatedCoded.Length);
            Debug.Log("x = " + seperatedCoded[seperatedCoded.Length-1,1]);
            startingTrialTimes = new float[Int32.Parse(seperatedCoded[seperatedCoded.Length,1])];
            for (int i=1; i<seperatedCoded.Length; i++) {
                int lineTrialNum = Int32.Parse(seperatedCoded[i,1]);
                float lineStartTime = float.Parse(seperatedCoded[i,9]);
                if (lineTrialNum != trialNum) trialNum=lineTrialNum;
                startingTrialTimes[trialNum] = lineStartTime;

            }
            
            for (int i=0; i<startingTrialTimes.Length; i++) {
                Debug.Log("Trial " + i + " = " + startingTrialTimes[i]);
            }
        }
        else showTrialNum = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
