using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI.Debugging
{
    // Source: 'https://gist.github.com/st4rdog/80057b406bfd00f44c8ec8796a071a13'.
    public class FPSCounterUI : MonoBehaviour
    {
        public enum DeltaTimeType
        {
            Smooth,
            Unscaled
        }

        [SerializeField] private BoolOptionValue _showFPSOptionValue;


        [Space(10)]
        [SerializeField] private TMP_Text _fpsCounterText;
        
        [SerializeField]
        [Tooltip("Unscaled is more accurate, but jumpy, or if your game modifies Time.timeScale. Use Smooth for smoothDeltaTime.")]
        private DeltaTimeType DeltaType = DeltaTimeType.Smooth;

        private int[] _frameRateSamples;
        private int _averageFromAmount = 30;
        private int _averageCounter;
        private int _currentAveraged;

        private const int MAX_DISPLAYED_VALUE = 300;
        private static readonly string MAX_DISPLAYED_VALUE_STRING = $"> {MAX_DISPLAYED_VALUE}";

        void Awake()
        {
            _frameRateSamples = new int[_averageFromAmount];

            _showFPSOptionValue.SubscribeToOnValueChangedAndTryTrigger(OnShowValueChanged);
        }
        private void OnDestroy() => _showFPSOptionValue.UnsubscribeFromOnValueChanged(OnShowValueChanged);
        private void OnShowValueChanged() => this.gameObject.SetActive(_showFPSOptionValue.Value);


        void Update()
        {
            SampleFramerate();
            CalculateAverageFramerate();
            DisplayAverageFramerate();
        }

        private void SampleFramerate()
        {
            int currentFrame = (int)Mathf.Round(1f / DeltaType switch
            {
                DeltaTimeType.Smooth => Time.smoothDeltaTime,
                DeltaTimeType.Unscaled => Time.unscaledDeltaTime,
                _ => Time.unscaledDeltaTime
            });
            _frameRateSamples[_averageCounter] = currentFrame;
        }
        private void CalculateAverageFramerate()
        {
            float average = 0f;
            foreach (var frameRate in _frameRateSamples)
                average += frameRate;

            _currentAveraged = (int)Mathf.Round(average / _averageFromAmount);
            _averageCounter = (_averageCounter + 1) % _averageFromAmount;
        }
        private void DisplayAverageFramerate()
        {
            if (_currentAveraged < 0)
                _fpsCounterText.text = "< 0";
            else if (_currentAveraged < 0)
                _fpsCounterText.text = MAX_DISPLAYED_VALUE_STRING;
            else
                _fpsCounterText.text = _currentAveraged.ToString();
        }
    }
}