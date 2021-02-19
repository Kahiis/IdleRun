using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Author Niko Kahilainen
/// A GPS script to track the distance which the user has moved and to track the users current speed.
/// Based mostly on unity's on locationservice documentation -> https://docs.unity3d.com/ScriptReference/LocationService.Start.html
/// </summary>
public class GPSscript : MonoBehaviour
{
    public int desiredAccuracy = 1;
    public int updateDistance = 5;

    public TextMeshProUGUI debugUI;
    public TextMeshProUGUI secondaryDebugUI;

    public Animator animator;
    public static GPSscript Instance { set; get; }

    // X will be latitude, Y will be longitude
    private Vector2 oldCoordinates;
    private Vector2 newCoordinates;

    private float distanceTravelled = 0f;
    // Current speed in meters per second
    private float currentSpeed = 0f;

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (debugUI == null) Debug.Log("No debug UI given!");
        if (animator == null) Debug.Log("No animator given!");

        // If the user doesn't have location permission enabled, ask the user to enable them
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
        }
        StartCoroutine(InitializeLocationService());
    }

    IEnumerator InitializeLocationService()
    {
        // Thanks Stackoverflow
        // https://stackoverflow.com/questions/45340418/input-locationservice-isenabledbyuser-returning-false-with-unity-remote-in-the-e
        #if UNITY_EDITOR
        //Wait until Unity connects to the Unity Remote, while not connected, yield return null
        while (!UnityEditor.EditorApplication.isRemoteConnected)
        {
            yield return null;
        }
        #endif
        // First check if the user has location service enabled
        if (!Input.location.isEnabledByUser)
        {
            UpdateDebugText("User Doesn't have gps enabled");
            yield break;
        }

        // Let's set the desired accuracy and update distance
        Input.location.Start(desiredAccuracy, updateDistance);

        // Wait until service initializes
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            UpdateDebugText("Initializing service");
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // Service didn't initialize in 20 seconds
        if (maxWait < 1)
        {
            UpdateDebugText("Timed out");
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Running)
        {
            UpdateDebugText("Location Service is running succesfully");
            StartCoroutine(LocationUpdate());
        }
    }

    // Update is called once per frame
    void Update()
    {
        // secondaryDebugUI.text = Input.location.status.ToString();
    }

    IEnumerator LocationUpdate()
    {
        int tick = -1;
        while (Input.location.status == LocationServiceStatus.Running)
        {
            tick++;
            
            if (oldCoordinates.x == 0 || oldCoordinates.y == 0)
            {
                oldCoordinates.x = Input.location.lastData.latitude;
                oldCoordinates.y = Input.location.lastData.longitude;
                UpdateDebugText(String.Format("No coordinates yet, tick: {0}\n" +
                                              "Latitude: {1}\n" +
                                              "Longitude: {2}",  tick, oldCoordinates.x, oldCoordinates.y));
            }

            newCoordinates.x = Input.location.lastData.latitude;
            newCoordinates.y = Input.location.lastData.longitude;

            
            float distance = DistanceBetweenCoordinates(oldCoordinates, newCoordinates);
            currentSpeed = distance;
            animator.SetFloat("Speed", currentSpeed);
            distanceTravelled += distance;
            
            oldCoordinates = newCoordinates;

            string temp = String.Format("fetching new coords \n" +
                                        "Latitude: {0}\n" +
                                        "Longitude: {1}\n" +
                                        "Speed: {2} m/s\n" +
                                        "Tick: {3}\n" +
                                        "Distance Travelled: {4}", newCoordinates.x, newCoordinates.y, currentSpeed, tick, distanceTravelled);
            UpdateDebugText(temp);
            yield return new WaitForSecondsRealtime(1f);
        }
    }

    /// <summary>
    /// Calculates the distance in meters between 2 coordinates.
    /// Based on a post on stackoverflow
    /// https://stackoverflow.com/questions/22686161/calculating-the-distance-between-2-points-in-c-sharp
    /// </summary>
    /// <param name="oldCoordinates"></param>
    /// <param name="newCoordinates"></param>
    /// <returns></returns>
    private float DistanceBetweenCoordinates(Vector2 c1, Vector2 c2)
    {
        // if old and new coordinates are the same then we don't need to calculate the distance between the two.
        if (c1 == c2)
        {
            return 0f;
        }

        float R = 6371000.0f;
        float lat1 = c1.x * Mathf.Deg2Rad;
        float lon1 = c1.y * Mathf.Deg2Rad;
        float lat2 = c2.x * Mathf.Deg2Rad;
        float lon2 = c2.y * Mathf.Deg2Rad;
        float dLat = lat2 - lat1;
        float dLon = lon2 - lon1;
        var a = Math.Pow(Math.Sin(dLat / 2), 2) + (Math.Pow(Math.Sin(dLon / 2), 2) * Math.Cos(lat1) * Math.Cos(lat2));
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distance = R * c;
        // Let's hope casting from double to float doesn't introduce any rounding errors etc.
        distance = Mathf.Abs((float)distance);
        return (float)distance;
    }

    public void UpdateDebugText(string text)
    {
        Debug.Log(text);
        debugUI.text = text;
    }
}
