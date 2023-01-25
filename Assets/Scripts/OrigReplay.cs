using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;

public class OrigReplay : MonoBehaviour
{
    string[,] seperatedInput; // stores output data file
    Dictionary<string, GameObject> allModels; // holds all models that move
    public GameObject car1; // 1_infiniti
    public GameObject car2; // 2_AudiS5
    public GameObject car3; // 3_Ford
    public GameObject car4; // 4_VW
    public GameObject car5; // 5_Skoda
    public GameObject playerModel;
    int index;
    Boolean paused;
    float totalPauseDelay;
    float currentPauseDelay;
    float time_PausedAt;
    float activeTime;
    float playbackSpeed;
    public string inputFileName;
    void Start()
    {
        // read entire output data file
        string projectLocation = Application.dataPath;
        string inputFile = Path.Combine(projectLocation, "../Output/" + inputFileName+".txt");
        string[] lines = File.ReadAllLines(inputFile);
        // split everything into an array
        seperatedInput = new string[lines.Length,lines[0].Length];
        for (int i=0; i<lines.Length; i++) {
            string[] data = lines[i].Split(' ');
            for (int j=0; j<data.Length; j++) {
                seperatedInput[i,j] = data[j];
            }
        }
        // initialize variables
        index = 2;
        allModels = new Dictionary<string, GameObject>();
        paused = false;
        totalPauseDelay = 0;
        activeTime = 0;
        playbackSpeed = 1;
    }

    void Update()
    {   
        if (index < seperatedInput.GetLength(0)) {
            float nextTimeRecorded = float.Parse(seperatedInput[index,1]);
            Debug.Log("timesince= " + Time.time + "\t nextTime = " + nextTimeRecorded + "\t totalpausedelay = " + totalPauseDelay + "\t timepausedat = " + time_PausedAt + "activeTime = " + activeTime);
            // Do something if next time read is less than current time
            while ((paused == false) && (nextTimeRecorded <= activeTime - totalPauseDelay)) {
                // check if we need to move
                if (seperatedInput[index,0].Equals("M")) {
                    string modelID = seperatedInput[index,2];
                    moveModel(modelID, index);
                }
                // check if we need to create
                else if (seperatedInput[index,0].Equals("create")) {
                    string modelID = seperatedInput[index,2];
                    createNewModel(modelID, index);
                }
                 else if (seperatedInput[index,0].Equals("remove")){
                    removeModel(index);
                }
                index++;
                nextTimeRecorded = float.Parse(seperatedInput[index,1]);
            }
            activeTime += playbackSpeed * Time.deltaTime;
        }

        // p pauses and unpauses replay
        if (Input.GetKeyDown(KeyCode.P)) {
            if (paused == false) {
                paused = true;
                time_PausedAt = Time.time;
            }
            else {
                paused = false;
                totalPauseDelay += currentPauseDelay;
            }
        }

        if (paused == true) currentPauseDelay = Time.time - time_PausedAt;

        // right arrow key increases playback speed by 1
        if (Input.GetKeyDown(KeyCode.RightArrow)) playbackSpeed += 1;
        
        // left arrow key decreases playback speed by 1
        if (Input.GetKeyDown(KeyCode.LeftArrow) && playbackSpeed >= 2) playbackSpeed -= 1;

        // NEXT: Skipping forward/back in time
        // time steps
        // show data points (position, ...)
    }


    void createNewModel(string modelID, int i) {
        string modelName = seperatedInput[i,3];
        float xPos = float.Parse(seperatedInput[i,4]);
        float yPos = float.Parse(seperatedInput[i,5]);
        float zPos = float.Parse(seperatedInput[i,6]);
        GameObject newObject;
        if (modelID.Equals("H0")) newObject = Instantiate(playerModel, new Vector3(xPos, yPos, zPos), Quaternion.identity);
        else {
            float xr = float.Parse(seperatedInput[i,8]);
            float yr = float.Parse(seperatedInput[i,9]);
            float zr = float.Parse(seperatedInput[i,10]);
            GameObject findModel;
            if (modelName.Equals("1_Infiniti")) findModel = car1;
            else if (modelName.Equals("2_AudiS5")) findModel = car2;
            else if (modelName.Equals("3_Ford")) findModel = car3;
            else if (modelName.Equals("4_VW")) findModel = car4;
            else findModel = car5;
            newObject = Instantiate(findModel, new Vector3(xPos, yPos, zPos), Quaternion.Euler(xr, yr, zr));
        }
        
        allModels.Add(modelID, newObject);
    }

    // "delete" a model by disabling it
    void removeModel(int i) {
        string modelID = seperatedInput[i,2];
        allModels[modelID].SetActive(false);
    }

    void moveModel(string modelID, int i) {
        GameObject model = allModels[modelID];
        float xPos = float.Parse(seperatedInput[i,3]);
        float yPos = float.Parse(seperatedInput[i,4]);
        float zPos = float.Parse(seperatedInput[i,5]);

        model.transform.position = new Vector3(xPos, yPos, zPos);
        if (modelID.Equals("H0")) {
            float xr = float.Parse(seperatedInput[i,6]);
            float yr = float.Parse(seperatedInput[i,7]);
            float zr = float.Parse(seperatedInput[i,8]);
            model.transform.rotation = Quaternion.Euler(xr, yr, zr);
        }
    }
}







//simpler pause logic
/*--------------------------------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;

public class Replay : MonoBehaviour
{
    string[,] seperatedInput; // stores output data file
    Dictionary<string, GameObject> allModels; // holds all models that move
    public GameObject car1; // 1_infiniti
    public GameObject car2; // 2_AudiS5
    public GameObject car3; // 3_Ford
    public GameObject car4; // 4_VW
    public GameObject car5; // 5_Skoda
    public GameObject playerModel;
    int index;
    Boolean paused;
    float activeTime;
    float playbackSpeed;
    public string inputFileName;
    void Start()
    {
        // read entire output data file
        string projectLocation = Application.dataPath;
        string inputFile = Path.Combine(projectLocation, "../Output/" + inputFileName+".txt");
        string[] lines = File.ReadAllLines(inputFile);
        // split everything into an array
        seperatedInput = new string[lines.Length,lines[0].Length];
        for (int i=0; i<lines.Length; i++) {
            string[] data = lines[i].Split(' ');
            for (int j=0; j<data.Length; j++) {
                seperatedInput[i,j] = data[j];
            }
        }
        // initialize variables
        index = 2;
        allModels = new Dictionary<string, GameObject>();
        paused = false;
        activeTime = 0;
        playbackSpeed = 1;
    }

    void Update()
    {   
        if (index < seperatedInput.GetLength(0)) {
            float nextTimeRecorded = float.Parse(seperatedInput[index,1]);
            Debug.Log("timesince= " + Time.time + "\tnextTime = " + nextTimeRecorded + "\tactiveTime = " + activeTime);
            // Do something if next time read is less than current time
            while ((paused == false) && (nextTimeRecorded <= activeTime)) {
                // check if we need to move
                if (seperatedInput[index,0].Equals("M")) {
                    string modelID = seperatedInput[index,2];
                    moveModel(modelID, index);
                }
                // check if we need to create
                else if (seperatedInput[index,0].Equals("create")) {
                    string modelID = seperatedInput[index,2];
                    createNewModel(modelID, index);
                }
                 else if (seperatedInput[index,0].Equals("remove")){
                    removeModel(index);
                }
                index++;
                nextTimeRecorded = float.Parse(seperatedInput[index,1]);
            }
            if (paused == false) activeTime += playbackSpeed * Time.deltaTime;
        }

        // p pauses and unpauses replay
        if (Input.GetKeyDown(KeyCode.P)) {
            if (paused == false) paused = true;
            else paused = false;
        }

        // right arrow key increases playback speed by 1
        if (Input.GetKeyDown(KeyCode.RightArrow)) playbackSpeed += 1;
        
        // left arrow key decreases playback speed by 1
        if (Input.GetKeyDown(KeyCode.LeftArrow) && playbackSpeed >= 2) playbackSpeed -= 1;

        // NEXT: Skipping forward/back in time
        // time steps
        // negative playbackSpeed
        // show data points (position, ...)
    }


    void createNewModel(string modelID, int i) {
        string modelName = seperatedInput[i,3];
        float xPos = float.Parse(seperatedInput[i,4]);
        float yPos = float.Parse(seperatedInput[i,5]);
        float zPos = float.Parse(seperatedInput[i,6]);
        GameObject newObject;
        if (modelID.Equals("H0")) newObject = Instantiate(playerModel, new Vector3(xPos, yPos, zPos), Quaternion.identity);
        else {
            float xr = float.Parse(seperatedInput[i,8]);
            float yr = float.Parse(seperatedInput[i,9]);
            float zr = float.Parse(seperatedInput[i,10]);
            GameObject findModel;
            if (modelName.Equals("1_Infiniti")) findModel = car1;
            else if (modelName.Equals("2_AudiS5")) findModel = car2;
            else if (modelName.Equals("3_Ford")) findModel = car3;
            else if (modelName.Equals("4_VW")) findModel = car4;
            else findModel = car5;
            newObject = Instantiate(findModel, new Vector3(xPos, yPos, zPos), Quaternion.Euler(xr, yr, zr));
        }
        
        allModels.Add(modelID, newObject);
    }

    // "delete" a model by disabling it
    void removeModel(int i) {
        string modelID = seperatedInput[i,2];
        allModels[modelID].SetActive(false);
    }

    void moveModel(string modelID, int i) {
        GameObject model = allModels[modelID];
        float xPos = float.Parse(seperatedInput[i,3]);
        float yPos = float.Parse(seperatedInput[i,4]);
        float zPos = float.Parse(seperatedInput[i,5]);

        model.transform.position = new Vector3(xPos, yPos, zPos);
        if (modelID.Equals("H0")) {
            float xr = float.Parse(seperatedInput[i,6]);
            float yr = float.Parse(seperatedInput[i,7]);
            float zr = float.Parse(seperatedInput[i,8]);
            model.transform.rotation = Quaternion.Euler(xr, yr, zr);
        }
    }
}*/