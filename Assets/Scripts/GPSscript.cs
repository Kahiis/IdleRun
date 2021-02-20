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
    public float updateFrequency = 1f;

    public TextMeshProUGUI debugUI;
    public TextMeshProUGUI secondaryDebugUI;
    public AudioSource coinSound;   

    public Animator animator;

    public int coinPoints = 5;

    public bool DEBUG;
    public float DEBUGSPEED;
    public static GPSscript Instance { set; get; }

    // X will be latitude, Y will be longitude
    private Vector2 oldCoordinates;
    private Vector2 newCoordinates;

    private float distanceTravelled = 0f;
    // Current speed in meters per second
    private float currentSpeed = 0f;
    private int points = 0;

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (debugUI == null) Debug.Log("No debug UI given!");
        if (secondaryDebugUI == null) Debug.Log("No secondary debug ui given!");
        if (animator == null) Debug.Log("No animator given!");

        // If the user doesn't have location permission enabled, ask the user to enable them
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
        }
        StartCoroutine(InitializeLocationService());
    }

    // Initializes the location service.
    // Based mostly on the example code provided by Unity documentation
    // https://docs.unity3d.com/ScriptReference/LocationService.Start.html
    IEnumerator InitializeLocationService()
    {
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

        // Service started, let's begin polling location
        if (Input.location.status == LocationServiceStatus.Running)
        {
            UpdateDebugText("Location Service is running succesfully");
            StartCoroutine(LocationUpdate());
        }
    }


    // Coroutine which polls the location service for coordinates.
    IEnumerator LocationUpdate()
    {
        // Sometimes when the location service is starting, it can give pretty wild location info.
        // This bit will hopefully prevent those crazy infos from jolting the character to victory instantly
        yield return new WaitForSecondsRealtime(5f);
        int tick = -1;
        while (Input.location.status == LocationServiceStatus.Running)
        {
            tick++;

            // This is pretty much for the only poll, since otherwise it would put crazy 
            // values as distance travelled and speed for the first tick
            if (oldCoordinates.x == 0 || oldCoordinates.y == 0)
            {
                oldCoordinates.x = Input.location.lastData.latitude;
                oldCoordinates.y = Input.location.lastData.longitude;
                UpdateDebugText(String.Format("No coordinates yet, tick: {0}\n" +
                                              "Latitude: {1}\n" +
                                              "Longitude: {2}", tick, oldCoordinates.x, oldCoordinates.y));
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
                                        "Distance Travelled: {4:f2} m\n" +
                                        "Points: {5}", newCoordinates.x, newCoordinates.y, currentSpeed, tick, distanceTravelled, points);
            UpdateDebugText(temp);
            yield return new WaitForSecondsRealtime(updateFrequency);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // For debugging basic mechanics
        if(DEBUG)
        {
            animator.SetFloat("Speed", DEBUGSPEED);
            this.transform.position += transform.forward * DEBUGSPEED *Time.deltaTime;
        } else
        {
            this.transform.position += transform.forward * currentSpeed * Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Hit something!");
        if(other.CompareTag("Coin"))
        {
            Debug.Log("Got coin!");
            points += coinPoints;
            Destroy(other.gameObject);
            coinSound.Play();
            // A bit of silly hardcode but it'll have to do for now
            if(points == 100)
            {
                StopCoroutine(LocationUpdate());
                animator.SetFloat("Speed", 0f);
                secondaryDebugUI.text = "Congratulations!\n" +
                                        "You are the ultimate walker/runner!\n" +
                                        "Close and restart the app to try again!";
            }
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
