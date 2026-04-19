using UnityEngine;

namespace UI.Debugging
{
    public class OverheatWarningUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        [Space(5)]
        [SerializeField] private AnimationCurve _overheatFlashRateCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);
        [SerializeField] private float _curveMultiplier = 3.0f;
        private float _flashFrequency;
        private float _currentFlashValue;

        [Space(5)]
        [SerializeField, Range(0.0f, 1.0f)]
        private float _fadePercentage = 0.2f;
        private float _maxAlpha;


        public void Show()
        {
            if (!this.gameObject.activeSelf)
            {
                this.gameObject.SetActive(true);
                _currentFlashValue = 0.0f;
                _canvasGroup.alpha = 0.0f;
            }
        }
        public void Hide()
        {
            if (this.gameObject.activeSelf)
                this.gameObject.SetActive(false);
        }

        public void UpdateFlashRate(float overheatPercentage)
        {
            _maxAlpha = Mathf.Min(overheatPercentage / _fadePercentage, 1.0f);
            _flashFrequency = (_curveMultiplier * _overheatFlashRateCurve.Evaluate(overheatPercentage));
        }
        


        private void Update()
        {
            _currentFlashValue += _flashFrequency * Time.deltaTime;
            float lerpValue = _currentFlashValue % 1.0f;
            _canvasGroup.alpha = Mathf.Min(Mathf.Lerp(0.0f, 1.0f, lerpValue > 0.5f ? 1.0f - lerpValue : lerpValue), _maxAlpha);
        }
    }
}