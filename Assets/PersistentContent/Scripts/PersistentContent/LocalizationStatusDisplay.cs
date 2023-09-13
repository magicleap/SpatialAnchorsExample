using TMPro;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.MagicLeap.Native;

namespace PersistentContentExample
{
    /// <summary>
    /// Simple script used to display localization status on a TMP Text.
    /// </summary>
    public class LocalizationStatusDisplay : MonoBehaviour
    {
        public TMP_Text StatusText;

        // Start is called before the first frame update
        void Start()
        {
            var localizationResult = MLSpace.GetLocalizationResult(out MLSpace.LocalizationResult result);
            if (MLResult.DidNativeCallSucceed(localizationResult, nameof(MLSpace.GetLocalizationResult)))
            {
                OnLocalizationChanged(result);

            }

            MLSpace.OnLocalizationEvent += OnLocalizationChanged;
        }

        void OnLocalizationChanged(MLSpace.LocalizationResult result)
        {
            StatusText.text = $"Localization Status: {result.LocalizationStatus} \n " +
                              $"Space Name: {result.Space.SpaceName}  \n" +
                              $"Space SpaceId: {MLConvert.ToUnity(result.Space.SpaceId)}  \n";
        }
    }
}