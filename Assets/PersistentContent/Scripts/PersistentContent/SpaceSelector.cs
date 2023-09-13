using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

namespace PersistentContentExample
{
    /// <summary>
    /// Simple script that allows users to select a space from a dropdown and localize into it.
    /// </summary>
    public class SpaceSelector : MonoBehaviour
    {
        public TMP_Dropdown SpaceSelectDropdown;
        public Button LocalizeButton;
        public TMP_Text StatusText;

        void Start()
        {
            StatusText.text = "";
            LocalizeButton.onClick.AddListener(RequestLocalization);
            SetDropdown();

            var localizationResult = MLSpace.GetLocalizationResult(out MLSpace.LocalizationResult result);
            if (MLResult.DidNativeCallSucceed(localizationResult, nameof(MLSpace.GetLocalizationResult)))
            {
                OnLocalizationChanged(result);
            }
            else
            {
                string status = $"Cannot Get Localization Status. Result: {localizationResult}";
                StatusText.text = status;
                Debug.LogError(status);
            }

            MLSpace.OnLocalizationEvent += OnLocalizationChanged;
            LocalizeButton.onClick.AddListener(RequestLocalization);
        }

        private void SetDropdown()
        {
            SpaceSelectDropdown.ClearOptions();

            var spaceListResult = MLSpace.GetSpaceList(out MLSpace.Space[] list);
            if (MLResult.DidNativeCallSucceed(spaceListResult, nameof(MLSpace.GetSpaceList)))
            {
                var spaceNames = list.Select(x => x.SpaceName).ToList();
                SpaceSelectDropdown.AddOptions(spaceNames);
            }
            else
            {
                string status = $"Cannot Get Space List. Result: {spaceListResult}";
                StatusText.text = status;
                Debug.LogError(status);
            }
        }

        private void RequestLocalization()
        {
            if (SpaceSelectDropdown.options.Count == 0)
            {
                string status = $"No Spaces Found. Please Create One";
                StatusText.text = status;
                Debug.LogError(status);
                return;
            }

            var spaceListResult = MLSpace.GetSpaceList(out MLSpace.Space[] list);
            if (MLResult.DidNativeCallSucceed(spaceListResult, nameof(MLSpace.GetSpaceList)))
            {
                for (int i = 0; i < list.Length; i++)
                {
                    if (list[i].SpaceName == SpaceSelectDropdown.options[SpaceSelectDropdown.value].text)
                    {

                        // Declare an MLSpace.SpaceInfo
                        MLSpace.SpaceInfo info;

                        // Set the SpaceId
                        info.SpaceId = list[i].SpaceId;
                        MLResult.Code requestResult = MLSpace.RequestLocalization(ref info);
                        if (requestResult == MLResult.Code.Ok)
                        {
                            string status = $"Request to localize into Space with ID {info.SpaceId} was successful.";
                            StatusText.text = status;
                            Debug.Log(status);
                        }
                        else
                        {
                            string status = $"Error requesting to localize into Space: {requestResult}";
                            StatusText.text = status;
                            Debug.LogError(status);
                        }

                        return;
                    }
                }

            }
            else
            {
                string status = $"Could Not Get Space List: {spaceListResult}";
                StatusText.text = status;
                Debug.LogError(status);
            }
        }

        void OnLocalizationChanged(MLSpace.LocalizationResult result)
        {
            SetDropdown();
            for (int i = 0; i < SpaceSelectDropdown.options.Count; i++)
            {
                if (SpaceSelectDropdown.options[i].text == result.Space.SpaceName)
                {
                    SpaceSelectDropdown.SetValueWithoutNotify(i);
                    string status = $"Localized Successfully into: {result.Space.SpaceName}.";
                    StatusText.text = status;
                    Debug.Log(status);
                    return;
                }
            }
        }
    }
}
