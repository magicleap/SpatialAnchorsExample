using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.MagicLeap;


/// <summary>
/// Allows user's to create spatial anchors and save them with additional data.
/// Press Trigger to create Prefab 1
/// Press Menu to create Prefab 2
/// Press the bumper to delete the prefab and spatial anchor binding.
/// </summary>
public class SpatialAnchorsExample : MonoBehaviour
{
    [Tooltip("The prefab that gets created when you press the Trigger Button.")]
    public GameObject Prefab1;
    [Tooltip("The prefab that gets created when you press the Menu Button.")]
    public GameObject Prefab2;

    [Tooltip("How often, in seconds, to check if localization has changed.")]
    public float SearchInterval = 5;

    [Tooltip("Text used to display localization info.")]
    public TMP_Text LocalizationInfoText;

    private string localizationStatus = "Not Localized";

    //Track the objects we already created to avoid duplicates
    private Dictionary<string, GameObject> _persistentObjectsById = new Dictionary<string, GameObject>();
    
    //The ID of the space the user is currently localized to.
    private string _localizedSpace;

    //Magic Leap inputs to detect if the user is pressing Menu, Bumper or Trigger.
    private MagicLeapInputs _magicLeapInputs;
    private MagicLeapInputs.ControllerActions _controllerActions;

    //Used to force search localization even if the current time hasn't expired
    private bool _searchNow;
    //The timestamp when anchors were last searched for
    private float _lastTick;

    //The amount of searches that were performed.
    //Used to make sure anchors are fully localized before instantiating them.
    //Currently, searching for an anchor has to performed at least 2 times before
    //anchors report their correct position.
    private int numberOfSearches;

    // Stores the spatial request.
    private MLAnchors.Request _spatialAnchorRequest;

    // Start is called before the first frame update
    void Start()
    {
        //Load Saved Anchor 
        SimpleAnchorBinding.Storage.LoadFromFile();

        //Initialize Anchor Request Object
        _spatialAnchorRequest = new MLAnchors.Request();

        //Check if Spatial Anchor permission is present in the manifest.
        var result = MLPermissions.CheckPermission(MLPermission.SpatialAnchors);
        if (result.IsOk != true)
        {
            Debug.LogError($"Cannot perform Spatial Anchor Functions. MLPermission: {MLPermission.SpatialAnchors} returned {result}." +
                           $"Disabling Spatial Anchors Script.");
            enabled = false;
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus == false)
        {
            SearchNow();
        }
    }

    void OnEnable()
    {
        //Initialize Input
        if (_magicLeapInputs == null)
        {
            _magicLeapInputs = new MagicLeapInputs();
            _controllerActions = new MagicLeapInputs.ControllerActions(_magicLeapInputs);
        }
        //Enable input if it was disabled previously
        _magicLeapInputs.Enable();
        _controllerActions.Bumper.started += BumperStarted;
        _controllerActions.Trigger.started += TriggerStarted;
        _controllerActions.Menu.started += MenuStarted;
    }

    private void OnDisable()
    {
        //Unsubscribe from controller input when the object is disabled
        if (_magicLeapInputs !=null)
        {
            _magicLeapInputs.Disable();
            _controllerActions.Bumper.started -= BumperStarted;
            _controllerActions.Trigger.started -= TriggerStarted;
            _controllerActions.Menu.started -= MenuStarted;
        }
    }

    /// <summary>
    /// Bypass the search interval to immediately search changes to the Spatial Anchors
    /// </summary>
    public void SearchNow()
    {
        _searchNow = true;
    }

    // Updates Every Frame
    void Update()
    {
        UpdateStatusText();

        // Only search when the update time lapsed 
        if ( !_searchNow && Time.time - _lastTick < SearchInterval)
            return;

        _lastTick = Time.time;

        //Check if Localization info is available
        MLResult mlResult = MLAnchors.GetLocalizationInfo(out MLAnchors.LocalizationInfo info);
        if (!mlResult.IsOk)
        {
            localizationStatus = "Could not get localization Info " + mlResult;
            Debug.Log(localizationStatus);
            return;
        }

        //Check if the user is localized. If they are not, clear existing bindings.
        if (info.LocalizationStatus == MLAnchors.LocalizationStatus.Localized)
        {
            //If we are in a new space clear the existing visuals and reset the search counter.
            if (info.SpaceId != _localizedSpace)
            {
                ClearVisuals();
                numberOfSearches = 0;
                localizationStatus = "Localized into : " + info.SpaceId;
                _localizedSpace = info.SpaceId;
            }

            //Search for spatial anchors in the current space.
            SearchSpatialAnchors();
        }
        else
        {
            //Clear the old visuals
            ClearVisuals();
            _localizedSpace = "";
            numberOfSearches = 0;
            localizationStatus = "Not Localized " + info.LocalizationStatus;
            Debug.Log(localizationStatus);
            return;
        }
     
    }

    /// <summary>
    /// Display localization status if info text is not null.
    /// </summary>
    private void UpdateStatusText()
    {
        if(LocalizationInfoText !=null)
            LocalizationInfoText.text = localizationStatus;
    }

    /// <summary>
    /// Remove all instantiated objects
    /// </summary>
    private void ClearVisuals()
    {
        foreach (var prefab in _persistentObjectsById.Values)
        {
            Destroy(prefab);
        }
        _persistentObjectsById.Clear();
    }

    /// <summary>
    /// Searches the spatial anchors in the current space.
    /// </summary>
    /// <returns>Will return false if any of the search requests result in an error.</returns>
    private bool SearchSpatialAnchors()
    {
        MLResult startStatus = _spatialAnchorRequest.Start(new MLAnchors.Request.Params(Camera.main.transform.position, 100, 0, false));
        numberOfSearches++;

        if (!startStatus.IsOk)
        {
            Debug.LogError("Could not start" + startStatus);
            return false;
        }

        MLResult queryStatus = _spatialAnchorRequest.TryGetResult(out MLAnchors.Request.Result result);

        if (!queryStatus.IsOk)
        {
            Debug.LogError("Could not get result " + queryStatus);
            return false;
        }

        //Restore Spatial Anchor Bindings.
        for (int i = 0; i < result.anchors.Length; i++)
        {
            MLAnchors.Anchor anchor = result.anchors[i];
            var savedAnchor = SimpleAnchorBinding.Storage.Bindings.Find(x => x.Id == anchor.Id);
            if (savedAnchor !=null && _persistentObjectsById.ContainsKey(anchor.Id) == false)
            {
                if (savedAnchor.JsonData == Prefab1.name)
                {
                    var persistentObject = Instantiate(Prefab1, anchor.Pose.position, anchor.Pose.rotation);
                    _persistentObjectsById.Add(anchor.Id, persistentObject);
                }
                else
                {
                    var persistentObject = Instantiate(Prefab2, anchor.Pose.position, anchor.Pose.rotation);
                    _persistentObjectsById.Add(anchor.Id, persistentObject);
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Creates an anchor at the controller's position
    /// and binds Prefab2 to the anchor's ID.
    /// </summary>
    private void MenuStarted(InputAction.CallbackContext obj)
    {
        Pose controllerPose = new Pose(_controllerActions.Position.ReadValue<Vector3>(),
            _controllerActions.Rotation.ReadValue<Quaternion>());

        MLAnchors.Anchor.Create(controllerPose, (long)TimeSpan.FromDays(365).TotalSeconds, out MLAnchors.Anchor anchor);
        //Try creating an anchor 
        var result = anchor.Publish();
        if (result.IsOk)
        {
            //If the anchor is successfully created,
            //bind a prefab name to the anchor and save it to local file storage.
            SimpleAnchorBinding savedAnchor = new SimpleAnchorBinding();
            savedAnchor.Bind(anchor, Prefab2.name);
            SimpleAnchorBinding.Storage.SaveToFile();

            //Instantiate the prefab at the anchors location
            var persistentObject = Instantiate(Prefab2, controllerPose.position, controllerPose.rotation);
            _persistentObjectsById.Add(anchor.Id, persistentObject);
        }
    }

    /// <summary>
    /// Creates an anchor at the controller's position
    /// and binds Prefab1 to the anchor's ID.
    /// </summary>
    private void TriggerStarted(InputAction.CallbackContext obj)
    {
        Pose controllerPose = new Pose(_controllerActions.Position.ReadValue<Vector3>(),
            _controllerActions.Rotation.ReadValue<Quaternion>());

        MLAnchors.Anchor.Create(controllerPose, (long)TimeSpan.FromDays(365).TotalSeconds, out MLAnchors.Anchor anchor);
        
        //Try creating an anchor 
        var result = anchor.Publish();
        if (result.IsOk)
        {
            //If the anchor is successfully created,
            //bind a prefab name to the anchor and save it to local file storage.
            SimpleAnchorBinding savedAnchor = new SimpleAnchorBinding();
            savedAnchor.Bind(anchor, Prefab1.name);
            SimpleAnchorBinding.Storage.SaveToFile();

            //Instantiate the prefab at the anchors location
            var persistentObject = Instantiate(Prefab1, controllerPose.position, controllerPose.rotation);
            _persistentObjectsById.Add(anchor.Id, persistentObject);
        }
    }


    /// <summary>
    /// Removes the Spatial Anchor nearest to the controller
    /// </summary>
    private void BumperStarted(InputAction.CallbackContext obj)
    {
        //Request anchors near the controller's position
        MLAnchors.Request.Params requestParams = 
            new MLAnchors.Request.Params(
               _controllerActions.Position.ReadValue<Vector3>(), 100, 0, true);

        // Start the search using the parameters specified in the Update function.
        MLResult startResult =_spatialAnchorRequest.Start(requestParams);

        if (!startResult.IsOk)
        {
            Debug.LogWarning("Anchor start error: "+ startResult);
            return;
        }

        MLResult queryResult = _spatialAnchorRequest.TryGetResult(out MLAnchors.Request.Result result);
        if (!queryResult.IsOk)
        {
            Debug.LogWarning("Anchor query error: " + startResult);
            return;
        }

        // Get the search results.
        for (int i = 0; i < result.anchors.Length; i++)
        {
            // Get the closest anchor that we saved
            var anchor = result.anchors[i];
            if (RemoveAnchor(anchor.Id))
            {
                break;
            }
        }
    }

    /// <summary>
    /// Remove an anchor binding from local storage
    /// </summary>
    /// <param name="id">The Anchor ID</param>
    /// <returns>Returns true if the ID existed in the localized space and in the saved data</returns>
    private bool RemoveAnchor(string id)
    {
        //Delete the anchor using the Anchor's ID
        var savedAnchor = SimpleAnchorBinding.Storage.Bindings.Find(x => x.Id == id);
        //Delete the gameObject if it exists
        if (savedAnchor != null)
        {
            if (_persistentObjectsById.ContainsKey(id))
            {
                GameObject anchorVisual = _persistentObjectsById[id];
                _persistentObjectsById.Remove(id);
                Destroy(anchorVisual);
            }

            MLAnchors.Anchor.DeleteAnchorWithId(id);
            savedAnchor.UnBind();
            SimpleAnchorBinding.Storage.SaveToFile();
            return true;
        }

        return false;
    }

}
