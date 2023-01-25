using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;

public class Replay : MonoBehaviour
{
    string[,] seperatedInput; // stores output data file
    Dictionary<string, GameObject> allModels; // holds all models that move
    Dictionary<string, GameObject> activeModels; // holds all models visible in scene
    public GameObject car1; // 1_infiniti
    public GameObject car2; // 2_AudiS5
    public GameObject car3; // 3_Ford
    public GameObject car4; // 4_VW
    public GameObject car5; // 5_Skoda
    public GameObject playerModel; // playerSphere
    int index; // line to read
    float nextTimeToExecute; // time recorded at current index
    Boolean paused; 
    Boolean timeSkip; // if index suddenly changes
    int alerts; //whether or not to create alert bars: 2 = need to decide, 1 = show alerts, 2 = no alerts
    float simulatedTime; // time of replay system
    float playbackSpeed; 
    public string inputFileName;
    public float startTime;
    GameObject nextNearLaneAlert;
    GameObject nextFarLaneAlert;

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
        index = indexFromTime(startTime);
        Debug.Log("FOUND INDEX = " + index);
        if (index == 0) simulatedTime = 0;
        else simulatedTime = startTime;
        allModels = new Dictionary<string, GameObject>();
        activeModels = new Dictionary<string, GameObject>();
        paused = false;
        playbackSpeed = 1;
        alerts = 2;
        nextTimeToExecute = 0;
        if (index == 0) timeSkip = false;
        else timeSkip = true;
        nextNearLaneAlert = null;
        nextFarLaneAlert = null;
        Preload();
    }

    void Update()
    {   
        if ((index < seperatedInput.GetLength(0)) && (index >= 0)) {
            nextTimeToExecute = float.Parse(seperatedInput[index,1]);
            Debug.Log("timesince= " + Time.time + "\tnextTime = " + nextTimeToExecute + "\tsimTime = " + simulatedTime + "\tplaySpeed = " + playbackSpeed + "\tindex = " + index + "\tpaused = " + paused);
            // Do something if next time read is less than current time (when playbackspeed is positive)
            // or when previous time read is greater than current time (when playbackspeed is negative)
            while ((paused == false) && ((nextTimeToExecute <= simulatedTime && playbackSpeed > 0) || (nextTimeToExecute >= simulatedTime && playbackSpeed < 0))) {
                executeIndex(index);
            }
            if (paused == false) simulatedTime += playbackSpeed * Time.deltaTime;
        }

        // p pauses and unpauses replay
        if (Input.GetKeyDown(KeyCode.P) && (playbackSpeed != 0)) { // does playbackspeed matter?
            if (paused == false) paused = true;
            else paused = false;
        }

        // right arrow key increases playback speed by 1
        if (Input.GetKeyDown(KeyCode.RightArrow)) {
            if (playbackSpeed == 0) paused = false;
            if (playbackSpeed == -1) paused = true;
            playbackSpeed += 1;
        }
        
        // left arrow key decreases playback speed by 1
        // if (Input.GetKeyDown(KeyCode.LeftArrow) && playbackSpeed >= 2) playbackSpeed -= 1;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            if (playbackSpeed == 0) paused = false;
            if (playbackSpeed == 1) paused = true;
            playbackSpeed -= 1;
        }

        // period key to time step forwards
        if (paused == true && Input.GetKeyDown(KeyCode.Period)) {
            if (playbackSpeed == -1) {
                index += 2;
                playbackSpeed = 1;
            }
            if (playbackSpeed == 0) {
                index++;
                playbackSpeed = 1;
            }
            executeIndex(index);
            simulatedTime = nextTimeToExecute;
        }

        // comma key to time step backwards
        if (paused == true && Input.GetKeyDown(KeyCode.Comma)) {
            if (playbackSpeed == 1) {
                index -= 2;
                playbackSpeed = -1;
            }
            if (playbackSpeed == 0) {
                index--;
                playbackSpeed = 1;
            }
            executeIndex(index);
            simulatedTime = nextTimeToExecute;
        }

        // show trial number somewhere
        // NEXT: Skipping back in time
        // show indicator bars
        // show data points (position, ...)
        // Error: should do nothing when index = 0 or index = last line -> just do nothing at last index
    }

    // executes a line of output
    void executeIndex(int i) {
        // if time is skipped, clear the scene
        if (timeSkip == true) {
            foreach(KeyValuePair<string, GameObject> item in activeModels) {
                item.Value.SetActive(false);
            }
            activeModels.Clear();
            timeSkip = false;
        }

        // check if we need to move
        if (seperatedInput[index,0].Equals("M")) {
            string modelID = seperatedInput[index,2];
            moveModel(modelID, index);
        }
        // check if we need to create
        // "create" by enabling model
        if (seperatedInput[index,0].Equals("create")) {
            string modelID = seperatedInput[index,2];
            if (playbackSpeed > 0) enableModel(modelID);
            else disableModel(modelID, index, true);
        }
        // check if we need to delete/remove
        // "remove" by disabling model
        if (seperatedInput[index,0].Equals("remove")){
            string modelID = seperatedInput[index,2];
            if (playbackSpeed > 0) disableModel(modelID, index, true);
            else enableModel(modelID);
        }

        if (playbackSpeed > 0) index++;
        if (playbackSpeed < 0) index--; 
        nextTimeToExecute = float.Parse(seperatedInput[index,1]);
        // Notes lines are always at t=0, which breaks replay
        if (seperatedInput[index, 0].Equals("Notes")) {
            if (playbackSpeed > 0) index++;
            if (playbackSpeed < 0) index--; 
            nextTimeToExecute = float.Parse(seperatedInput[index,1]);
        }
    }

    // runs entire recording to fill allModels
    void Preload() {
        for (int i=0; i<seperatedInput.GetLength(0); i++) {
            // check if we need to create
            if (seperatedInput[i,0].Equals("create")) {
                string modelID = seperatedInput[i,2];
                createNewModel(modelID, i);
            }
            if (seperatedInput[i,0].Equals("M")) {
                string modelID = seperatedInput[i,2];
                moveModel(modelID, i);
            }
            if (seperatedInput[i,0].Equals("remove")){
                string modelID = seperatedInput[i,2];
                disableModel(modelID, i, false);
            }
        }
    } 

    // returns index of seperatedInput where time is exactly or right before given input time
    // returns 0 if invalid
    int indexFromTime(float t) {
        int closestIndex = 0;
        if ((float.Parse(seperatedInput[seperatedInput.GetLength(0)-1,1]) <= t) || t <= 0) return 0;
        for (int i=0; i<seperatedInput.GetLength(0); i++) {
            if((float.Parse(seperatedInput[i,1]) <= t)) closestIndex = i;
            else return closestIndex;
        }
        return closestIndex;
    }

    // creates new model at index i
    // only used by Start()
    void createNewModel(string modelID, int i) {
        if (alerts == 2 && !modelID.Equals("H0")) {
            Debug.Log(seperatedInput[i,5]);
            if (string.Equals(seperatedInput[i,5],"noAlert") || string.Equals(seperatedInput[i,5],"alert")) alerts = 1;
            else alerts = 0;
        }
        string modelName = seperatedInput[i,3];
        float xPos = float.Parse(seperatedInput[i,4]);
        float yPos;
        float zPos;
        if (alerts != 1) {
            yPos = float.Parse(seperatedInput[i,5]);
            zPos = float.Parse(seperatedInput[i,6]);
        }
        else {
            yPos = float.Parse(seperatedInput[i,6]);
            zPos = float.Parse(seperatedInput[i,7]);
        }
        GameObject newObject;
        if (modelID.Equals("H0")) newObject = Instantiate(playerModel, new Vector3(xPos, yPos, zPos), Quaternion.identity);
        else {
            float xr;
            float yr;
            float zr;
            if (alerts != 1) {
                xr = float.Parse(seperatedInput[i,8]);
                yr = float.Parse(seperatedInput[i,9]);
                zr = float.Parse(seperatedInput[i,10]);
            }
            else {
                xr = float.Parse(seperatedInput[i,9]);
                yr = float.Parse(seperatedInput[i,10]);
                zr = float.Parse(seperatedInput[i,11]);
            }
            GameObject findModel;
            if (modelName.Equals("1_Infiniti")) findModel = car1;
            else if (modelName.Equals("2_AudiS5")) findModel = car2;
            else if (modelName.Equals("3_Ford")) findModel = car3;
            else if (modelName.Equals("4_VW")) findModel = car4;
            else findModel = car5;
            newObject = Instantiate(findModel, new Vector3(xPos, yPos, zPos), Quaternion.Euler(xr, yr, zr));
        }
        if (alerts == 1) {
            // 1.3 means far lane
            if (float.Parse(seperatedInput[i,8]) == 1.3) {
                if (nextFarLaneAlert != null) {
                    float distanceBetween = nextFarLaneAlert.transform.position.x - yPos;
                    Debug.Log("db = " + distanceBetween + "\tyPos = " + yPos + "\t transformx = " + nextNearLaneAlert.transform.position.x);
                    GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    bar.transform.parent = nextFarLaneAlert.transform;
                    bar.transform.position = bar.transform.parent.position;
                    bar.transform.localScale += new Vector3(distanceBetween, 0, 0);
                    bar.transform.position += new Vector3((distanceBetween/2), 0, 0);
                }
                if (seperatedInput[i,5].Equals("alert")) nextFarLaneAlert = newObject;
                else nextFarLaneAlert = null;
            }
            else {
                if (nextNearLaneAlert != null) {
                    float distanceBetween = yPos - nextNearLaneAlert.transform.position.x;
                    Debug.Log("db = " + distanceBetween + "\tyPos = " + yPos + "\t transformx = " + nextNearLaneAlert.transform.position.x);
                    
                    GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    bar.transform.parent = nextNearLaneAlert.transform;
                    bar.transform.position = bar.transform.parent.position;
                    bar.transform.localScale += new Vector3(distanceBetween, 0, 0);
                    bar.transform.position -= new Vector3((distanceBetween/2), 0, 0);
                }
                if (seperatedInput[i,5].Equals("alert")) nextNearLaneAlert = newObject;
                else nextNearLaneAlert = null;
            }
        }
        allModels.Add(modelID, newObject);
        Debug.Log("new car added = " + modelID);
    }

    // move a model at index i
    void moveModel(string modelID, int i) {
        GameObject findModel = allModels[modelID];
        // if model is not active, then activate it first
        if (!findModel.activeSelf){
            enableModel(modelID);
        }

        float xPos = float.Parse(seperatedInput[i,3]);
        float yPos = float.Parse(seperatedInput[i,4]);
        float zPos = float.Parse(seperatedInput[i,5]);

        findModel.transform.position = new Vector3(xPos, yPos, zPos);
        if (modelID.Equals("H0")) {
            float xr = float.Parse(seperatedInput[i,6]);
            float yr = float.Parse(seperatedInput[i,7]);
            float zr = float.Parse(seperatedInput[i,8]);
            findModel.transform.rotation = Quaternion.Euler(xr, yr, zr);
        }
    }

    // "delete" a model at index i by disabling it
    void disableModel(string modelID, int i, Boolean removeFromActive) {
        allModels[modelID].SetActive(false);
        if (removeFromActive == true) activeModels.Remove(modelID);
    }

    // "creates" a model at index i by enabling it
    void enableModel (string modelID) {
        GameObject findModel = allModels[modelID];
        findModel.SetActive(true);
        activeModels.Add(modelID, findModel);
    }
}