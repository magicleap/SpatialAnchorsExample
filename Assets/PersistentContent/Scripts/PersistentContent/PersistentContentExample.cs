 using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.MagicLeap;

 namespace PersistentContentExample
 {
     /// <summary>
     /// This script shows how to use the AnchorManager and the BindingsLocalStorage scripts to save and restore anchors.
     /// </summary>
     public class PersistentContentExample : MonoBehaviour
     {
         [Tooltip("The prefab that gets created when you press the Trigger Button.")]
         public GameObject Prefab1;

         [Tooltip("The prefab that gets created when you press the Menu Button.")]
         public GameObject Prefab2;

         //Track the objects we already created to avoid duplicates
         private Dictionary<string, GameObject> _persistentObjectsById = new Dictionary<string, GameObject>();

         public AnchorManager anchorManager;

         // Used to prevent creating anchors when selecting UI
         private XRRayInteractor xRRayInteractor;

         //Magic Leap inputs to detect if the user is pressing Menu, Bumper or Trigger.
         private MagicLeapInputs _magicLeapInputs;
         private MagicLeapInputs.ControllerActions _controllerActions;

         // Start is called before the first frame update
         void Start()
         {
             if (anchorManager == null)
             {
                 Debug.LogError($"Anchor Manager Not Assigned, Script will not function properly. Disabling {this}");
                 enabled = false;
                 return;
             }

             //Load Saved Anchor 
             SimpleAnchorBinding.Storage.LoadFromFile();

             anchorManager.OnAnchorsLost.AddListener(ClearVisuals);
             anchorManager.OnAnchorAdded.AddListener(RestoreAnchor);
             anchorManager.OnAnchorRemoved.AddListener(RemoveAnchorBinding);
             anchorManager.OnAnchorUpdated.AddListener(UpdateAnchorPosition);
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
             if (_magicLeapInputs != null)
             {
                 _magicLeapInputs.Disable();
                 _controllerActions.Bumper.started -= BumperStarted;
                 _controllerActions.Trigger.started -= TriggerStarted;
                 _controllerActions.Menu.started -= MenuStarted;
             }
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

         public void RestoreAnchor(MLAnchors.Anchor anchor)
         {
             var savedAnchor = SimpleAnchorBinding.Storage.Bindings.Find(x => x.Id == anchor.Id);

             if (savedAnchor != null && _persistentObjectsById.ContainsKey(anchor.Id) == false)
             {

                 if (savedAnchor.Extras == Prefab1.name)
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

         public void RemoveAnchorBinding(string anchorId)
         {
             SimpleAnchorBinding anchorBinding = SimpleAnchorBinding.Storage.Bindings.Find(x => x.Id == anchorId);

             if (anchorBinding != null)
             {
                 if (_persistentObjectsById.ContainsKey(anchorId))
                 {
                     GameObject anchorVisual = _persistentObjectsById[anchorId];
                     Destroy(anchorVisual);
                     _persistentObjectsById.Remove(anchorId);
                 }

                 anchorBinding.UnBind();
             }
         }

         public void UpdateAnchorPosition(MLAnchors.Anchor anchor)
         {
             if (_persistentObjectsById.ContainsKey(anchor.Id))
             {
                 GameObject anchorVisual = _persistentObjectsById[anchor.Id];
                 anchorVisual.transform.position = anchor.Pose.position;
                 anchorVisual.transform.rotation = anchor.Pose.rotation;
             }
         }

         /// <summary>
         /// Creates an anchor at the controller's queryPosition
         /// and binds Prefab2 to the anchor's ID.
         /// </summary>
         private void MenuStarted(InputAction.CallbackContext obj)
         {
             Pose controllerPose = new Pose(_controllerActions.Position.ReadValue<Vector3>(),
                 _controllerActions.Rotation.ReadValue<Quaternion>());

             //Try creating an anchor 
             var result =
                 anchorManager.TryCreateAnchor(controllerPose, TimeSpan.FromDays(365), out MLAnchors.Anchor anchor);
             if (result.IsOk)
             {
                 //If the anchor is successfully created,
                 //bind a prefab name to the anchor and save it to local file storage.
                 SimpleAnchorBinding savedAnchor = new SimpleAnchorBinding();
                 savedAnchor.Bind(anchor, Prefab2.name);
                 if (SimpleAnchorBinding.Storage.SaveToFile())
                 {
                     //Instantiate the prefab at the anchors location
                     var persistentObject = Instantiate(Prefab2, controllerPose.position, controllerPose.rotation);
                     _persistentObjectsById.Add(anchor.Id, persistentObject);
                 }
             }
         }

         private bool IsPointerOverUI()
         {
             if (xRRayInteractor == null)
             {
                 xRRayInteractor = FindObjectOfType<XRRayInteractor>();
             }

             return xRRayInteractor != null &&
                    xRRayInteractor.TryGetCurrentUIRaycastResult(out UnityEngine.EventSystems.RaycastResult result);
         }

         /// <summary>
         /// Creates an anchor at the controller's queryPosition
         /// and binds Prefab1 to the anchor's ID.
         /// </summary>
         private void TriggerStarted(InputAction.CallbackContext obj)
         {
             //Do not create anchors when selecting UI
             if (IsPointerOverUI())
                 return;

             Pose controllerPose = new Pose(_controllerActions.Position.ReadValue<Vector3>(),
                 _controllerActions.Rotation.ReadValue<Quaternion>());

             MLResult creationResult =
                 anchorManager.TryCreateAnchor(controllerPose, TimeSpan.FromDays(365), out MLAnchors.Anchor anchor);
             if (creationResult.IsOk)
             {
                 //If the anchor is successfully created,
                 //bind a prefab name to the anchor and save it to local file storage.
                 SimpleAnchorBinding savedAnchor = new SimpleAnchorBinding();
                 if (savedAnchor.Bind(anchor, Prefab1.name))
                 {
                     //Instantiate the prefab at the anchors location
                     var persistentObject = Instantiate(Prefab1, controllerPose.position, controllerPose.rotation);
                     _persistentObjectsById.Add(anchor.Id, persistentObject);
                 }
             }
         }

         /// <summary>
         /// Removes the Spatial Anchor nearest to the controller
         /// </summary>
         private void BumperStarted(InputAction.CallbackContext obj)
         {
             //Gets the controller's queryPosition and finds the closest anchor visual to remove
             Vector3 controllerPosition = _controllerActions.Position.ReadValue<Vector3>();
             KeyValuePair<string, GameObject> closestVisual = GetClosestVisual(controllerPosition);

             if (closestVisual.Value != null)
             {
                 //Remove the binding. We use the OnRemove Callback to remove the visual and pin
                 if (anchorManager.TryRemoveAnchor(closestVisual.Key).IsOk)
                 {
                     _persistentObjectsById.Remove(closestVisual.Key);
                     Destroy(closestVisual.Value);

                     var savedAnchor = SimpleAnchorBinding.Storage.Bindings.Find(x => x.Id == closestVisual.Key);
                     if (savedAnchor != null)
                         savedAnchor.UnBind();
                 }
             }
         }

         private KeyValuePair<string, GameObject> GetClosestVisual(Vector3 queryPosition)
         {
             if (_persistentObjectsById.Count == 0)
             {
                 return new KeyValuePair<string, GameObject>();
             }

             //First sort by distance
             //Then choose the first element
             var sortedFactors = _persistentObjectsById
                 .OrderBy(x => Vector3.SqrMagnitude(x.Value.transform.position - queryPosition))
                 .First();
             return sortedFactors;
         }
     }
 }