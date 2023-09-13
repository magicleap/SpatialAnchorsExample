using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.MagicLeap;
using InputDevice = UnityEngine.XR.InputDevice;

namespace PersistentContentExample
{
    [System.Serializable]
    public class MLAnchorEvent : UnityEvent<MLAnchors.Anchor> {}

    [System.Serializable]
    public class StringEvent : UnityEvent<string> {}

    [System.Serializable]
    public class LocalizationInfoEvent : UnityEvent<MLSpace.LocalizationResult> {}

    /// <summary>
    /// This script allows users to query, create and destroy anchors as well as provides callbacks when anchors are Added, Updated or Removed.
    /// </summary>
    public class AnchorManager : MonoBehaviour
    {
        [Tooltip("How often, in ms, to check if localization has changed.")]
        private float SearchInterval = 100f;

        [Tooltip("The anchor search radius.")] public float SearchRadius = 100;

        [Tooltip("Where to search the anchors from.")]
        public Transform QueryCenter;

        /// <summary>
        /// Is the Magic Leap Localized into a space. Can the user create or remove anchors.
        /// </summary>
        public bool isLocalized
        {
            get { return _currentLocalization.LocalizationStatus == MLSpace.Status.Localized; }
        }

        /// <summary>
        /// The Anchors that have been tracked. Used to determine OnCreated/OnRemoved/OnUpdated events.
        /// </summary>
        private HashSet<string> _trackedAnchorIds = new HashSet<string>();

        /// <summary>
        /// The current localization status. Used to determine if anchors should be queried.
        /// </summary>
        private MLSpace.LocalizationResult _currentLocalization;

        private IEnumerator _updateAnchorsCoroutine;

        /// <summary>
        /// Used to force search localization even if the current time hasn't expired.
        /// </summary>
        private bool _searchNow;

        /// <summary>
        /// The timestamp when anchors were last queried.
        /// </summary>
        private float _lastTick;

        // Stores the spatial request.
        private MLAnchors.Request _spatialAnchorRequest;

        /// <summary>
        /// Called when the headset is has localized for the first time or into a new space.
        /// </summary>
        public LocalizationInfoEvent OnLocalized;

        /// <summary>
        /// Called when an anchor was already initialized, but the position might have updated.
        /// </summary>
        public MLAnchorEvent OnAnchorUpdated;

        /// <summary>
        /// Called when an anchor is first discovered.
        /// </summary>
        public MLAnchorEvent OnAnchorAdded;

        /// <summary>
        /// Called when a user deletes an existing anchor.
        /// </summary>
        public StringEvent OnAnchorRemoved;

        /// <summary>
        /// Called when Anchor bindings reset. The OnAnchor found will be called again for each anchor in the space when
        /// user re-localizes.
        /// </summary>
        public UnityEvent OnAnchorsLost;

        /// <summary>
        /// The headset device used to query head pose state from.
        /// </summary>
        private InputDevice _headDevice;

        /// <summary>
        /// The head tracking state that was last queried.
        /// </summary>
        private InputSubsystem.Extensions.MLHeadTracking.StateEx _previousHeadTrackingState;

        private void OnApplicationPause(bool pauseStatus)
        {
            //Query an anchor refresh if the user has unpaused the application.
            if (pauseStatus == false)
            {
                SearchNow();
            }
        }

        /// <summary>
        /// Assigns a default Query Center.
        /// </summary>
        private void OnValidate()
        {
            if (QueryCenter == null && Camera.main)
            {
                QueryCenter = Camera.main.transform;
            }
        }

        /// <summary>
        /// Bypass the search interval to immediately search changes to the Spatial Anchors
        /// </summary>
        public void SearchNow()
        {
            _searchNow = true;
        }

        private void Start()
        {
            //If no Query Center was provided, use the Main Camera or this transform.
            if (QueryCenter == null)
            {
                QueryCenter = Camera.main ? Camera.main.transform : this.transform;
            }

            //Initialize Anchor Request Object
            _spatialAnchorRequest = new MLAnchors.Request();
            MLSpace.OnLocalizationEvent += OnLocalizationChanged;
            _updateAnchorsCoroutine = DoUpdateAnchors();
            StartCoroutine(_updateAnchorsCoroutine);
        }

        /// <summary>
        /// Called when the localization status changed.
        /// Used to trigger an anchor refresh when localizing into a new space or regaining localization.
        /// </summary>
        /// <param name="result"> The localization result obtained from the MLSpaces.OnLocalizationEvent API</param>
        void OnLocalizationChanged(MLSpace.LocalizationResult result)
        {
            //Check if the user regained localization
            if (result.LocalizationStatus == MLSpace.Status.Localized)
            {
                if (string.IsNullOrEmpty(_currentLocalization.Space.SpaceName)
                    || !_currentLocalization.Space.SpaceId.Equals(result.Space.SpaceId))
                {
                    ClearAnchors();
                }

                OnLocalized.Invoke(result);
            }

            _currentLocalization = result;

        }

        /// <summary>
        /// Remove all instantiated objects
        /// </summary>
        private void ClearAnchors()
        {
            _trackedAnchorIds.Clear();
            OnAnchorsLost.Invoke();
        }

        private void OnDestroy()
        {
            StopCoroutine(_updateAnchorsCoroutine);
            ThreadDispatcher.DispatchAllAndShutdown();
        }

        void Update()
        {
            ThreadDispatcher.DispatchAll();
        }

        private IEnumerator DoUpdateAnchors()
        {
            //Get the localization state as soon as the application starts
            MLSpace.GetLocalizationResult(out MLSpace.LocalizationResult result);
            _currentLocalization = result;

            while (true)
            {
                // Wait before querying again for localization status
                if (Time.time - _lastTick >= SearchInterval && !_searchNow)
                {
                    yield return null;
                }

                UpdateHeadPoseStatus();

                //Only search for anchors if head pose is valid and the user is localized
                if (_previousHeadTrackingState.Status ==
                    InputSubsystem.Extensions.MLHeadTracking.HeadTrackingStatus.Valid
                    && _currentLocalization.LocalizationStatus == MLSpace.Status.Localized)
                {
                    //UpdateAnchorsOnWorkerThread(QueryCenter.position, SearchRadius);
                    ThreadDispatcher.ScheduleWork(() =>
                        UpdateAnchorsOnWorkerThread(QueryCenter.position, SearchRadius));
                }

                _searchNow = false;
                _lastTick = Time.time;
                yield return null;
            }
        }

        /// <summary>
        /// Obtain the head pose state to determine if anchors should be re-queried.
        /// </summary>
        private void UpdateHeadPoseStatus()
        {
            if (!_headDevice.isValid)
            {
                _headDevice = InputSubsystem.Utils.FindMagicLeapDevice(
                    InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice);
                return;
            }

            if (_headDevice.isValid && InputSubsystem.Extensions.MLHeadTracking.TryGetStateEx(
                    _headDevice, out InputSubsystem.Extensions.MLHeadTracking.StateEx state))
            {

                if (_previousHeadTrackingState.Status !=
                    InputSubsystem.Extensions.MLHeadTracking.HeadTrackingStatus.Valid
                    && state.Status == InputSubsystem.Extensions.MLHeadTracking.HeadTrackingStatus.Valid)
                {
                    Debug.Log($"Head Tracking Regained:{state.Status}");
                    SearchNow();
                }

                _previousHeadTrackingState = state;
            }
        }

        private void UpdateAnchorsOnWorkerThread(Vector3 position, float radius)
        {
            //Start the anchor query from a given position and search radius
            MLAnchors.Request.Params queryParams = new MLAnchors.Request.Params(position, radius, 0, false);
            MLResult startStatus = _spatialAnchorRequest.Start(queryParams);
            if (!startStatus.IsOk)
            {
                ThreadDispatcher.ScheduleMain(() =>
                    LogOnMainThread($"Could not start MLAnchor Request: {startStatus}", LogType.Error));
            }

            //Get the result of the anchor query.
            MLResult queryStatus = _spatialAnchorRequest.TryGetResult(out MLAnchors.Request.Result result);
            if (!queryStatus.IsOk)
            {
                ThreadDispatcher.ScheduleMain(() =>
                    LogOnMainThread($"UpdateAnchorsOnWorkerThread: failed to query anchors: {queryStatus}",
                        LogType.Error));
            }

            // Since the anchor positions are returned on the perception frame, some anchor positions might not be initialized.
            // In this instance, we skip the query until all of the anchor positions are valid.
            foreach (MLAnchors.Anchor anchor in result.anchors)
            {
                if (anchor.Pose.rotation.x == 0
                    && anchor.Pose.rotation.y == 0
                    && anchor.Pose.rotation.z == 0
                    && anchor.Pose.rotation.w == 0)
                {
                    ThreadDispatcher.ScheduleMain(() =>
                        LogOnMainThread(
                            $"UpdateAnchorsOnWorkerThread: Some anchors have invalid poses, This can happen.: {queryStatus}",
                            LogType.Warning));

                    return;
                }
            }

            ThreadDispatcher.ScheduleMain(() => UpdateAnchorsOnMainThread(result.anchors));
        }


        /// <summary>
        /// Calls the OnCreated, On Updated and OnRemoved events on the main thread.
        /// </summary>
        /// <param name="anchors"></param>
        private void UpdateAnchorsOnMainThread(MLAnchors.Anchor[] anchors)
        {
            //Create a HashSet of the existing anchors.
            //Assume that all of them were removed since last queried.
            HashSet<string> anchorsToRemove = new HashSet<string>();
            foreach (var trackedAnchorId in _trackedAnchorIds)
            {
                anchorsToRemove.Add(trackedAnchorId);
            }

            // For each of the anchors retrieved in the query,
            // call OnAnchorAdded if the anchor was not previously tracked,
            // call OnAnchorUpdated and remove it from the anchors to remove hashset.
            for (int i = 0; i < anchors.Length; i++)
            {
                MLAnchors.Anchor anchor = anchors[i];
                if (_trackedAnchorIds.Contains(anchor.Id))
                {
                    OnAnchorUpdated.Invoke(anchor);
                    anchorsToRemove.Remove(anchor.Id);
                }
                else
                {
                    OnAnchorAdded.Invoke(anchor);
                    _trackedAnchorIds.Add(anchor.Id);
                }
            }

            //If the anchors were not retrieved in the last query, consider them removed.
            foreach (var removedAnchorIds in anchorsToRemove)
            {
                _trackedAnchorIds.Remove(removedAnchorIds);
                OnAnchorRemoved.Invoke(removedAnchorIds);
            }
        }


        /// <summary>
        /// Creates an anchor at the given pose with the set expiration Time. Publishes the anchor on a separate thread.
        /// </summary>
        /// <param name="anchorPose">The pose at which the anchor will be created.</param>
        /// <param name="expirationTime">The anchors expiration time</param>
        /// <param name="createdAnchor">The resulting anchor (The result will not be the unpublished version)</param>
        /// <returns>Returns the result of the MLCreate method</returns>
        public MLResult TryCreateAnchor(Pose anchorPose, TimeSpan expirationTime, out MLAnchors.Anchor createdAnchor)
        {
            MLResult createResult = MLAnchors.Anchor.Create(anchorPose, (long)expirationTime.TotalSeconds,
                out MLAnchors.Anchor anchor);
            createdAnchor = new MLAnchors.Anchor();
            if (createResult.IsOk)
            {
                createdAnchor = anchor;
                //Publishing an anchor can cause a drop in performance, perform the function on another thread.
                MLResult publishResult = anchor.Publish();
                return publishResult;
            }

            Debug.LogWarning("Anchor not created : " + createResult);
            return createResult;
        }

        /// <summary>
        /// Remove an anchor binding from local storage
        /// </summary>
        /// <param name="id">The Anchor ID</param>
        /// <returns>Returns the result of the MLAnchors.DeleteAnchorWithId method.</returns>
        public MLResult TryRemoveAnchor(string id)
        {
            MLResult deleteAnchorResult = MLAnchors.Anchor.DeleteAnchorWithId(id);
            if (deleteAnchorResult.IsOk)
            {
                if (_trackedAnchorIds.Contains(id))
                    _trackedAnchorIds.Remove(id);

                OnAnchorRemoved.Invoke(id);
                return deleteAnchorResult;
            }
            else
            {
                Debug.LogWarning("Failed To Delete Anchor : " + deleteAnchorResult.Result);
                return deleteAnchorResult;
            }
        }

        /// <summary>
        /// Used to log messages that were sent from a separate thread.
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="option">The type of log.</param>
        private void LogOnMainThread(string message, LogType option)
        {
            switch (option)
            {
                case LogType.Error:
                    Debug.LogError(message);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(message);
                    break;
                default:
                    Debug.Log(message);
                    break;
            }
        }
    }
}