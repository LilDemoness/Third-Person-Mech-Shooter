using Gameplay.GameplayObjects.Players;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Minimap
{
    [DefaultExecutionOrder(10)]    // Ensure this runs after RadarManager instances.
    public class RadarUI : MonoBehaviour
    {
        private readonly Dictionary<BaseLocatable, BaseLocatableIcon> _locatableIconDict = new();
        private readonly Dictionary<BaseLocatable, (float TimePinged, float PingEffectStartTime)> _tempPingTimings = new();


        [Header("Radar")]
        [SerializeField]
        [Tooltip("Container for our radar ping icons")]
        private RectTransform _iconContainer;

        [SerializeField, Min(0.1f)]
        [Tooltip("The detection range of the radar (In metres)")]
        private float _detectionRange = 30.0f;

        [SerializeField]
        [Tooltip("Should the radar pings rotate with the player's forward direction, or be fixed?")]
        private bool _rotateWithPlayer = true;

        [SerializeField]
        [Tooltip("An offset towards the centre of the radar applied to the outer border where clamped pings are positioned to keep them in view even if the radar's edge isn't visible")]
        private float _clampedPingOffset = 0.0f;



        [Header("Radar Ping")]
        [SerializeField]
        private Image _pingIndicator;

        [SerializeField]
        [Tooltip("How long a ping lasts before fading completely (In seconds).")]
        private float _pingDuration = 0.5f;


        [Header("Danger Highlighting")]
        [SerializeField]
        [Tooltip("The inner segment of the danger indicator.")]
        private CanvasGroup _innerSegment;

        [SerializeField]
        [Tooltip("The middle segments of the danger indicators. Starts with top, moving clockwise.")]
        private CanvasGroup[] _middleSegments;

        [SerializeField]
        [Tooltip("The outer segments of the danger indicators. Starts with top, moving clockwise.")]
        private CanvasGroup[] _outerSegments;

        [Space(5)]
        [SerializeField]
        [Tooltip("The distance that danger must be within to be shown in the inner segment (In metres).")]
        private float _innerSegmentRange;

        [SerializeField]
        [Tooltip("The distance that danger must be within to be shown in the middle segment (In metres).")]
        private float _middleSegmentRange;

        [SerializeField]
        [Tooltip("The distance that danger must be within to be shown in the outer segment (In metres).")]
        private float _outerSegmentRange;

        const int RADAR_DANGER_SEGMENTS = 8;
        const float INDIVIDUAL_SEGMENT_ANGLE = (360.0f / RADAR_DANGER_SEGMENTS) * Mathf.Deg2Rad;
        const float INDIVIDUAL_SEGMENT_HALF_ANGLE = INDIVIDUAL_SEGMENT_ANGLE / 2.0f;




        private void OnEnable()
        {
            RadarManager.OnLocatableUpdated += OnLocatableUpdated;
            RadarManager.OnPingStopped += RadarManager_OnPingStopped;
            RadarManager.OnPingReset += RadarManager_OnPingReset;

            LocatableManager.OnLocatableAdded += OnLocatableAdded;
            LocatableManager.OnLocatableRemoved += OnLocatableRemoved;
        }
        private void OnDisable()
        {
            RadarManager.OnLocatableUpdated -= OnLocatableUpdated;
            RadarManager.OnPingStopped -= RadarManager_OnPingStopped;
            RadarManager.OnPingReset -= RadarManager_OnPingReset;

            LocatableManager.OnLocatableAdded -= OnLocatableAdded;
            LocatableManager.OnLocatableRemoved -= OnLocatableRemoved;
        }
        private void Start()
        {
            RadarManager.Instance.SimulatePreviousEventsForListener(OnLocatableAdded, OnLocatableUpdated);
        }

        private void OnLocatableAdded(BaseLocatable locatable)
        {
            if (locatable == null || _locatableIconDict.ContainsKey(locatable))
                return;

            // Create the corresponding icon for the locatable.
            BaseLocatableIcon icon = locatable.CreateIcon();
            icon.transform.SetParent(_iconContainer, false);

            // Ensure the icon starts hidden, just in case.
            icon.SetVisible(false);

            // Cache the locatable and the icon.
            _locatableIconDict.Add(locatable, icon);
        }
        private void OnLocatableRemoved(BaseLocatable locatable)
        {
            if (locatable == null || !_locatableIconDict.TryGetValue(locatable, out BaseLocatableIcon icon))
                return;

            // Remove the locatable.
            _locatableIconDict.Remove(locatable);
            _tempPingTimings.Remove(locatable);

            // Cleanup the locatable's icon.
            Destroy(icon.gameObject);
        }
        private void OnLocatableUpdated(BaseLocatable locatable, RadarManager.DesiredOperation desiredOperation)
        {
            // We received this notification before LocatableManager.OnLocatableAdded was called.
            // Do required logic for the locatable before progressing.
            if (!_locatableIconDict.ContainsKey(locatable))
                OnLocatableAdded(locatable);


            // Update all required cached values for the change of operation.

            if (desiredOperation == RadarManager.DesiredOperation.PingLogic)
                _tempPingTimings.Add(locatable, (-1.0f, -1.0f));
            else
                _tempPingTimings.Remove(locatable);
        }

        private void RadarManager_OnPingStopped()
        {
            // Hide the ping indicator.
            _pingIndicator.CrossFadeAlpha(0.0f, 0.0f, true);
        }
        private void RadarManager_OnPingReset()
        {
            // Reset our cached ping timings and hide active indicators.
            List<BaseLocatable> keys = new List<BaseLocatable>(_tempPingTimings.Keys);
            foreach (BaseLocatable locatable in keys)
            {
                _tempPingTimings[locatable] = (-1.0f, _tempPingTimings[locatable].PingEffectStartTime);
            }
        }


        private void Update()
        {
            if (Player.LocalClientInstance == null)
                return;

            // Update Icon Positions.
            UpdateLocatableIconPositions();

            // Update Icon Visibility/Alphas.
            UpdateDefaultLocatableIcons();
            UpdatePingVisuals();
            UpdatePingedIcons();

            // Update indicators for danger/'loud actions'.
            UpdateDangerIndicators();
        }


        /// <summary>
        ///     Updates the position of all locatable icons.
        /// </summary>
        private void UpdateLocatableIconPositions()
        {
            float radarRadius = GetRadarUIRadius();
            float sqrRadarRadius = radarRadius * radarRadius;

            foreach(var kvp in _locatableIconDict)
            {
                (kvp.Value.transform as RectTransform).anchoredPosition = GetIconLocation(kvp.Key);
            }
        }
        /// <summary>
        ///     Updates the visibility of all locatable icons with a desired operation of 'Default Logic'.
        /// </summary>
        private void UpdateDefaultLocatableIcons()
        {
            float radarRadius = GetRadarUIRadius();
            float sqrRadarRadius = radarRadius * radarRadius;

            foreach (BaseLocatable locatable in RadarManager.Instance.GetLocatablesForOperation(RadarManager.DesiredOperation.DefaultLogic))
            {
                BaseLocatableIcon icon = _locatableIconDict[locatable];
                icon.SetVisible(locatable.ClampToRadarBorder || (icon.transform as RectTransform).anchoredPosition.sqrMagnitude < sqrRadarRadius);
            }
        }


        /// <summary>
        ///     Determines the desired position of the icon on the radar.
        /// </summary>
        /// <returns> True if the icon is visible, otherwise false.</returns>
        private Vector2 GetIconLocation(BaseLocatable locatable)
        {
            Vector2 iconLocation = GetDistanceVectorToPlayer(locatable);
            float radarRadius = GetRadarUIRadius();

            float scale = radarRadius / _detectionRange;
            iconLocation *= scale;

            // Rotate the icons by the player's y rotation
            if (_rotateWithPlayer)
                iconLocation = RotatePositionWithPlayer(iconLocation);

            // Clamp locatables that with to be clamped to the radar border.
            if (locatable.ClampToRadarBorder)
                iconLocation = Vector2.ClampMagnitude(iconLocation, radarRadius - _clampedPingOffset);

            return iconLocation;
        }
        private Vector2 RotatePositionWithPlayer(Vector2 position)
        {
            // Get the player's forward direction flattened to the horizontal axis.
            Vector3 playerHorizontalForwardDirection = Vector3.ProjectOnPlane(Player.LocalClientInstance.GetForward(), Vector3.up).normalized;
            // Calculate the player's horizontal rotation.
            Quaternion rotation = Quaternion.LookRotation(playerHorizontalForwardDirection);

            // Mirror the 'y' rotation for display [Why].
            Vector3 eulerAngles = rotation.eulerAngles;
            eulerAngles.y = -eulerAngles.y;
            rotation.eulerAngles = eulerAngles;

            // Rotate the icon's position around the player to match the desired rotation.
            Vector3 rotatedIconDirection = rotation * new Vector3(position.x, 0.0f, position.y); // In 3D for quaternions.
            return new Vector2(rotatedIconDirection.x, rotatedIconDirection.z); // Convert back to 2D and return.
        }



        /// <summary>
        ///     Updates the scale and alpha of the ping indicator.
        /// </summary>
        private void UpdatePingVisuals()
        {
            float pingPercentage = RadarManager.Instance.CurrentPingPercentage;
            _pingIndicator.transform.localScale = new Vector3(pingPercentage, pingPercentage);

            const float PING_FADE_PERCENTAGE = 0.9f;    // Fade the ping past this point.
            _pingIndicator.CrossFadeAlpha(pingPercentage >= PING_FADE_PERCENTAGE
                ? 1.0f - ((pingPercentage - PING_FADE_PERCENTAGE) / (1.0f - PING_FADE_PERCENTAGE))
                : 1.0f
            , 0.0f, true);
        }
        /// <summary>
        ///     Updates the visibility of any pingable locatables given the current and previous frames' radar ping values.<br/>
        /// </summary>
        private void UpdatePingedIcons()
        {
            float pingRadius = GetRadarUIRadius();

            // Cache sqr values for more performant distance calculation.
            float sqrPreviousDistance = Mathf.Pow(RadarManager.Instance.PreviousPingPercentage * pingRadius, 2);
            float sqrCurrentDistance = Mathf.Pow(RadarManager.Instance.CurrentPingPercentage * pingRadius, 2);


            // Ping locatables now within our ping radius.
            foreach (BaseLocatable locatable in RadarManager.Instance.GetLocatablesForOperation(RadarManager.DesiredOperation.PingLogic))
            {
                float sqrDistance = (_locatableIconDict[locatable].transform as RectTransform).anchoredPosition.sqrMagnitude;

                if (sqrDistance > sqrCurrentDistance)
                    continue;   // Not within ping radius.
                if (sqrDistance < sqrPreviousDistance && _tempPingTimings[locatable].TimePinged > 0.0f)
                    continue;   // Already been pinged.

                // Start a ping.
                _tempPingTimings[locatable] = (Time.time, Time.time);
            }

            List<BaseLocatable> keys = new List<BaseLocatable>(_tempPingTimings.Keys);
            foreach (var key in keys)
            {
                if (_tempPingTimings[key].PingEffectStartTime < 0.0f)
                    continue;   // Hasn't yet been pinged OR thier ping is no longer visible.

                float pingDurationPercentage = (Time.time - _tempPingTimings[key].PingEffectStartTime) / _pingDuration;
                if (pingDurationPercentage >= 1.0f)
                {
                    _locatableIconDict[key].SetVisible(false);
                    continue;
                }

                _locatableIconDict[key].SetVisible(true);
                const float FADE_IN_PERCENTAGE = 0.2f;
                if (pingDurationPercentage > FADE_IN_PERCENTAGE) // Fade out.
                    _locatableIconDict[key].SetAlpha((1.0f - FADE_IN_PERCENTAGE) - (pingDurationPercentage - FADE_IN_PERCENTAGE));
                else // Fade in.
                    _locatableIconDict[key].SetAlpha(pingDurationPercentage / FADE_IN_PERCENTAGE);
            }
        }



        /// <summary>
        ///     Updates the alphas of the Danger Indicator segments so that only those with a dangerous quadrant inside them are shown.
        /// </summary>
        private void UpdateDangerIndicators()
        {
            // Hide all ring segments.
            _innerSegment.alpha = 0.0f;
            for(int i = 0; i < RADAR_DANGER_SEGMENTS; ++i)
            {
                _middleSegments[i].alpha = 0.0f;
                _outerSegments[i].alpha = 0.0f;
            }

            // Cache sqr ranges for quick distance checking.
            float sqrInnerSegmentRange = _innerSegmentRange * _innerSegmentRange;
            float sqrMiddleSegmentRange = _middleSegmentRange * _middleSegmentRange;
            float sqrOuterSegmentRange = _outerSegmentRange * _outerSegmentRange;


            // Evaluate each location we wish to highlight.
            foreach(var quadrantIndicies in RadarManager.DangerQuadrants.Keys)
            {
                Vector2 dangerLocalPosition = GetDistanceVectorToPlayer(RadarManager.MapQuadToPosition(quadrantIndicies));
                float sqrDistance = dangerLocalPosition.sqrMagnitude;

                if (sqrDistance < sqrInnerSegmentRange)
                {
                    // Highlight the inner segment.
                    _innerSegment.alpha = 1.0f;
                }
                else if (sqrDistance < sqrMiddleSegmentRange)
                {
                    // Determine which middle ring to highlight.
                    _middleSegments[GetRingIndex(dangerLocalPosition.normalized)].alpha = 1.0f;
                }
                else if (sqrDistance < sqrOuterSegmentRange)
                {
                    // Determine which outer ring to highlight.
                    _outerSegments[GetRingIndex(dangerLocalPosition.normalized)].alpha = 1.0f;
                }
            }

            // If desired, rotate the segments root so that our segments remain static in their WORLD direction.
            if (_rotateWithPlayer)
                _innerSegment.transform.parent.up = RotatePositionWithPlayer(Vector2.up);
        }
        /// <summary>
        ///     Returns the index for the danger indicator to be highlighted given the passed direction.
        /// </summary>
        /// <param name="localDirection"> The direction to the danger source, local to the top of the radar.</param>
        private int GetRingIndex(Vector2 localDirection)
        {
            const float TWO_PI = 2.0f * Mathf.PI;

            float angle = Mathf.Atan2(localDirection.x, localDirection.y);
            float adjustedAngle = angle + INDIVIDUAL_SEGMENT_HALF_ANGLE;
            if (adjustedAngle < 0.0f)
                adjustedAngle += TWO_PI;

            return Mathf.FloorToInt(adjustedAngle / INDIVIDUAL_SEGMENT_ANGLE);
        }






        /// <summary>
        ///     Returns the player's horizontal distance to the locatable.<br/>
        ///     Ignores elevation.
        /// </summary>
        private Vector2 GetDistanceVectorToPlayer(BaseLocatable locatable) => GetDistanceVectorToPlayer(locatable.transform.position);
        private Vector2 GetDistanceVectorToPlayer(Vector2 position) => GetDistanceVectorToPlayer(new Vector3(position.x, 0.0f, position.y));
        /// <summary>
        ///     Returns the player's horizontal distance to <paramref name="position"/>.<br/>
        ///     Ignores elevation.
        /// </summary>
        private Vector2 GetDistanceVectorToPlayer(Vector3 position)
        {
            Vector3 distanceToPlayer = position - Player.LocalClientInstance.GetPosition();
            return new Vector2(distanceToPlayer.x, distanceToPlayer.z);
        }

        /// <summary>
        ///     Returns the radius of the radar UI.
        /// </summary>
        private float GetRadarUIRadius() => _iconContainer.rect.width / 2.0f;
    }
}