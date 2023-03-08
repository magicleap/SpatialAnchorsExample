using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

public class MLSpaceIntentLauncher : MonoBehaviour
{
    public bool UseARCloud;

    [Tooltip("Used to determine if the QR Code Data passed contains a Map ID")]
    public string MapPrefix = "MAGICLEAP-ARCLOUD-MAP-ID:";

    public void TryLocalize(string qrCodeData)
    {
        var stringSections = qrCodeData.Split(MapPrefix);

        if (stringSections.Length == 2)
        {
            var result = MLPermissions.CheckPermission(MLPermission.SpatialAnchors);
            if (result.IsOk)
            {
                MLResult mlResult = MLAnchors.GetLocalizationInfo(out MLAnchors.LocalizationInfo info);
                if (info.LocalizationStatus == MLAnchors.LocalizationStatus.NotLocalized)
                {
                    OpenSpacesApp(stringSections[1]);
                }
            }
        }
    }
    private void OpenSpacesApp(string spaceID)
    {
        Debug.Log("Trying To Localize into " + spaceID);
#if UNITY_MAGICLEAP || UNITY_ANDROID
        try
        { 
            AndroidJavaClass activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent",
                "com.magicleap.intent.action.SELECT_SPACE");
            intent.Call<AndroidJavaObject>(
                "putExtra", "com.magicleap.intent.extra.SPACE_ID",
                spaceID);
            intent.Call<AndroidJavaObject>(
                "putExtra", "com.magicleap.intent.extra.MAPPING_MODE",
                UseARCloud ? 1 : 0);
            activity.Call("startActivityForResult", intent, 0);
        }
        catch (Exception e)
        {
            Debug.LogError("Error while launching spaces app: " + e);
        }
#endif
    }
}
