using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.MagicLeap;


public class QRCodeReader : MonoBehaviour
{
    [System.Serializable]
    public class UnityStringEvent:UnityEvent<string> { }

    /// <summary>
    ///  Camera to use when tracking aruco markers.
    /// </summary>
    public bool UseRGBCamera;
    public float QRCodeSize = 0.1f;
    public GameObject QRCodeVisual;
    public UnityStringEvent OnQRCodeDetected;

    private MLMarkerTracker.Settings markerSettings;
    private MLMarkerTracker.MarkerType MarkerTypes = MLMarkerTracker.MarkerType.QR;
    private MLMarkerTracker.ArucoDictionaryName ArucoDicitonary;
    private float ArucoMarkerSize = 0.1f;
    private ASCIIEncoding asciiEncoder = new ASCIIEncoding();

    private void OnEnable()
    {
        MLMarkerTracker.OnMLMarkerTrackerResultsFoundArray += OnMLMarkerTrackerResultsFoundArray;
    }

    private void OnDisable()
    {
        if(MLMarkerTracker.IsStarted)
            MLMarkerTracker.StopScanningAsync();

        MLMarkerTracker.OnMLMarkerTrackerResultsFoundArray -= OnMLMarkerTrackerResultsFoundArray;
    }

    void Start()
    {
       var permissionResult = MLPermissions.CheckPermission(MLPermission.MarkerTracking);
       if (permissionResult.IsOk)
       {
           EnableMarkerTrackerExample();
       }
       else
       { 
           Debug.LogError($"Marker Tracking Not Initialized, {MLPermission.MarkerTracking} " +
                          $"permission was not present in the manifest");
       }
    }

    private void OnOnPermissionDenied(string permission)
    {
    }

    private void OnOnPermissionGranted(string permission)
    {
     
    }

    private void OnMLMarkerTrackerResultsFoundArray(MLMarkerTracker.MarkerData[] dataArray)
    {
        foreach (MLMarkerTracker.MarkerData data in dataArray)
        {
            ProcessSingleMarker(data);
        }
    }

    private void ProcessSingleMarker(MLMarkerTracker.MarkerData data)
    {
        switch (data.Type)
        {
            case MLMarkerTracker.MarkerType.QR:
            {
                if (QRCodeVisual)
                { 
                    QRCodeVisual.SetActive(true);
                    QRCodeVisual.transform.position = data.Pose.position;
                    QRCodeVisual.transform.rotation = data.Pose.rotation;
                }

                string id = asciiEncoder.GetString(data.BinaryData.Data, 0, data.BinaryData.Data.Length);
                OnQRCodeDetected.Invoke(id);
                break;
            }
        }
    }

    private void EnableMarkerTrackerExample()
    {
        try
        {
            // Unity has it's own value for Enum called Everything and sets it to -1
            MarkerTypes = (int)MarkerTypes == -1 ? MLMarkerTracker.MarkerType.All : MarkerTypes;
            markerSettings = MLMarkerTracker.Settings.Create(true, MarkerTypes, QRCodeSize, ArucoDicitonary, ArucoMarkerSize, UseRGBCamera?0:1, MLMarkerTracker.FPSHint.Medium);
            MLMarkerTracker.SetSettingsAsync(markerSettings).GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            Debug.Log($"EnableMarkerTrackerExample() => error: {e.Message}");
        }
    }
}
