using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;

public class Replay : MonoBehaviour
{
    string[,]seperatedInput; // stores output data file
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
    GameObject nextNearLaneAlert; // car in near lane that alert starts at
    GameObject nextFarLaneAlert; // car in far lane that alert starts at
    private Color[] alert_colors; 
    private int alert_colors_index_near;
    private int alert_colors_index_far;
    public float skipToTime; // time to skip to while running
    private float storedSkipTime; // used to deetect change in skipToTime;

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
        Debug.Log("STARTING INDEX = " + index);
        if (index == 0) simulatedTime = 0;
        else simulatedTime = startTime;
        allModels = new Dictionary<string, GameObject>();
        activeModels = new Dictionary<string, GameObject>();
        paused = false;
        playbackSpeed = 1; // seconds of recorded data played per second in real time
        alerts = 2;
        nextTimeToExecute = 0;
        if (index == 0) timeSkip = false;
        else timeSkip = true;
        nextNearLaneAlert = null;
        nextFarLaneAlert = null;
        alert_colors = new Color[3];
        alert_colors[0] = new Color(1f, 0.5f, 0.4f, 0.7f); //Color orange;
        alert_colors[1] = new Color(0f, 0f, 1f, 0.7f); //Color blue
        alert_colors[2] = new Color(0.8f, 0.4f, 0.1f, 0.7f); //Color pink;
        alert_colors_index_near = 0;
        alert_colors_index_far = 0;
        skipToTime = 0;
        storedSkipTime = 0;
        Preload();
    }

    void Update()
    {   
        Debug.Log("timesince= " + Time.time + "\tnextTime = " + nextTimeToExecute + "\tsimTime = " + simulatedTime + "\tplaySpeed = " + playbackSpeed + "\tindex = " + index + "\tpaused = " + paused);
        if ((index < (seperatedInput.GetLength(0) - 2)) && (index >= 0)) {
            nextTimeToExecute = float.Parse(seperatedInput[index,1]);
            // Do something if next time read is less than current time (when playbackspeed is positive)
            // or when previous time read is greater than current time (when playbackspeed is negative)
            while ((paused == false) && ((nextTimeToExecute <= simulatedTime && playbackSpeed > 0) || (nextTimeToExecute >= simulatedTime && playbackSpeed < 0))) {
                executeIndex(index);
            }
            if (paused == false) simulatedTime += playbackSpeed * Time.deltaTime;
        }

        // p pauses and unpauses replay
        // executes all lines at current time
        if (Input.GetKeyDown(KeyCode.P) && (playbackSpeed != 0)) {
            if (paused == false) {
                float currentTime;
                if (playbackSpeed > 0) currentTime = float.Parse(seperatedInput[index-1,1]);
                else currentTime = float.Parse(seperatedInput[index+1,1]);
                executeTime(currentTime);
                paused = true;
            }
            else paused = false;
        }

        // right arrow key increases playback speed by 1
        if (Input.GetKeyDown(KeyCode.RightArrow)) {
            if (playbackSpeed == 0) paused = false;
            if (playbackSpeed == -1) paused = true;
            playbackSpeed += 1;
        }
        
        // left arrow key decreases playback speed by 1
        if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            if (playbackSpeed == 0) paused = false;
            if (playbackSpeed == 1) paused = true;
            playbackSpeed -= 1;
        }

        // period key to time step forwards
        if (paused == true && Input.GetKeyDown(KeyCode.Period)) {
            if (playbackSpeed == -1) {
                index += 2;
            }
            if (playbackSpeed == 0) {
                index++;
            }
            playbackSpeed = 1;
            executeTime(float.Parse(seperatedInput[index,1]));
            simulatedTime = nextTimeToExecute;
        }

        // comma key to time step backwards
        if (paused == true && Input.GetKeyDown(KeyCode.Comma)) {
            if (playbackSpeed == 1) {
                index -= 2;
            }
            if (playbackSpeed == 0) {
                index--;
            }
            playbackSpeed = -1;
            executeTime(float.Parse(seperatedInput[index,1]));
            simulatedTime = nextTimeToExecute;
        }

        //  check if we need to skip to a new time
        if (skipToTime != storedSkipTime) {
            int newIndex = indexFromTime(skipToTime);
            index = newIndex;
            nextTimeToExecute = float.Parse(seperatedInput[index,1]);
            simulatedTime = nextTimeToExecute;
            playbackSpeed = 1;
            timeSkip = true;
            storedSkipTime = skipToTime;
        }

        // show trial number somewhere
        // Error: should do nothing when index = 0 or index = last line -> just do nothing at last index
    }

    // executes a line of output
    void executeIndex(int i) {
        // if time is skipped, clear the scene
        if (timeSkip == true) {
            foreach(KeyValuePair<string, GameObject> item in activeModels) {
                item.Value.transform.position = new Vector3(300, 0, 0); // move model off screen to not cause flashes near participant when re-enabled
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
        // do not increase index out of bounds if we are at the end
        if (index < (seperatedInput.GetLength(0) - 1)) {
            if (playbackSpeed > 0) index++;
            if (playbackSpeed < 0) index--; 

            // Notes lines are always at t=0, which breaks replay
            if (seperatedInput[index, 0].Equals("Notes")) {
                if (playbackSpeed > 0) index++;
                if (playbackSpeed < 0) index--; 
                
            }
            nextTimeToExecute = float.Parse(seperatedInput[index,1]);
        }
        else {
            if (playbackSpeed < 0) {
                index--;
                nextTimeToExecute = float.Parse(seperatedInput[index,1]);
            }
            else {
                paused = true;
                simulatedTime = nextTimeToExecute;
            }
        }
    }

    // executes all lines with the given time
    void executeTime(float time) {
        while (float.Parse(seperatedInput[index,1]) == time) {
            executeIndex(index);
            simulatedTime += playbackSpeed * Time.deltaTime;
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
            // check if we need to move
            if (seperatedInput[i,0].Equals("M")) {
                string modelID = seperatedInput[i,2];
                moveModel(modelID, i);
            }
            // check if we need to delete
            if (seperatedInput[i,0].Equals("remove")){
                string modelID = seperatedInput[i,2];
                disableModel(modelID, i, false);
            }
        }
        // ensures the scene is clear
        foreach(KeyValuePair<string, GameObject> car in activeModels) {
            car.Value.SetActive(false);
        }
        activeModels.Clear();
    } 

    // returns index of seperatedInput where time is exactly or right before given input time
    // returns 0 if invalid
    int indexFromTime(float t) {
        int closestIndex = 0;
        if (t >= (float.Parse(seperatedInput[seperatedInput.GetLength(0)-1,1])) || t <= 0) return 0;
        for (int i=0; i<seperatedInput.GetLength(0); i++) {
            if((float.Parse(seperatedInput[i,1]) <= t)) closestIndex = i;
            else return closestIndex;
        }
        return closestIndex;
    }

    // creates new model at index i
    // only used by Start()
    void createNewModel(string modelID, int i) {
        if (alerts == 2 && !modelID.Equals("H0")) alerts = 1;
        
        float xPos;
        float yPos;
        float zPos;
        GameObject newObject;
        // create player model
        if (modelID.Equals("H0")) {
            xPos = float.Parse(seperatedInput[i,3]);
            yPos = float.Parse(seperatedInput[i,4]);
            zPos = float.Parse(seperatedInput[i,5]);
            newObject = Instantiate(playerModel, new Vector3(xPos, yPos, zPos), Quaternion.identity);
        }
        // create car model
        else {
            xPos = float.Parse(seperatedInput[i,6]);
            yPos = float.Parse(seperatedInput[i,7]);
            zPos = float.Parse(seperatedInput[i,8]);

            float xRotation = float.Parse(seperatedInput[i,9]);
            float yRotation = float.Parse(seperatedInput[i,10]);
            float zRotation = float.Parse(seperatedInput[i,11]);

            // get car model with same name
            GameObject findModel;
            string modelName = seperatedInput[i,3];
            if (modelName.Equals("1_Infiniti")) findModel = car1;
            else if (modelName.Equals("2_AudiS5")) findModel = car2;
            else if (modelName.Equals("3_Ford")) findModel = car3;
            else if (modelName.Equals("4_VW")) findModel = car4;
            else findModel = car5;
            newObject = Instantiate(findModel, new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRotation, yRotation, zRotation));

            // z = 1.3 means far lane
            if (seperatedInput[i,8].Equals("1.3")) {
                if (nextFarLaneAlert != null) {
                    float distanceBetween = xPos - nextFarLaneAlert.transform.position.x;
                    GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    bar.transform.parent = nextFarLaneAlert.transform;
                    bar.transform.position = bar.transform.parent.position;
                    bar.transform.localScale += new Vector3(distanceBetween - 1, -0.5f, -0.7f); // cube created of size 1, y should be 0.5f, z should be 0.3f
                    bar.transform.position += new Vector3((distanceBetween/2), 3, 0);
                    bar.transform.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Transparent/Diffuse")); // sets material type to be transparent (to see player model behind it)
                    bar.transform.GetComponent<MeshRenderer>().material.color = alert_colors[alert_colors_index_far];
                    alert_colors_index_far = (alert_colors_index_far + 1) % alert_colors.Length;
                }
                // if car being created has an alert
                if (seperatedInput[i,5].Equals("alert")) {
                    nextFarLaneAlert = newObject;
                }
                else nextFarLaneAlert = null;
            }
            // same as above but for near lane
            else {
                if (nextNearLaneAlert != null) {
                    float distanceBetween = nextNearLaneAlert.transform.position.x - xPos;
                    GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    bar.transform.parent = nextNearLaneAlert.transform;
                    bar.transform.position = bar.transform.parent.position;
                    bar.transform.localScale += new Vector3(distanceBetween - 1, -0.5f, -0.7f);
                    bar.transform.position -= new Vector3((distanceBetween/2), -3, 0);
                    bar.transform.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Transparent/Diffuse"));
                    bar.transform.GetComponent<MeshRenderer>().material.color = alert_colors[alert_colors_index_near];
                    alert_colors_index_near = (alert_colors_index_near + 1) % alert_colors.Length;
                }
                if (seperatedInput[i,5].Equals("alert")) {
                    nextNearLaneAlert = newObject;
                }
                else nextNearLaneAlert = null;
            }
        }
        allModels.Add(modelID, newObject);
        activeModels.Add(modelID, newObject); //lags preload?
        Debug.Log("new model added = " + modelID);
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
        allModels[modelID].transform.position = new Vector3(300, 0, 0); // move model off screen to not cause flashes near participant when re-enabled
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