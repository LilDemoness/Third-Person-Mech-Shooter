using Gameplay.GameplayObjects.Players;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Minimap
{
    public class Radar : MonoBehaviour
    {
        private readonly Dictionary<BaseLocatable, BaseLocatableIcon> _locatableIconDict = new();
        private readonly Dictionary<DesiredOperation, List<BaseLocatable>> _functionToLocatablesDict = new();
        private readonly Dictionary<BaseLocatable, (float TimePinged, float PingEffectStartTime)> _tempPingTimings = new();
        private readonly Dictionary<Vector2Int, float> _dangerQuadrants = new();


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
        
        [Space(5)]

        [SerializeField]
        [Tooltip("The number of pings that the radar completes per second.")]
        private float _pingFrequency = 0.5f;

        [SerializeField]
        [Tooltip("How long a ping lasts before fading completely (In seconds).")]
        private float _pingDuration = 0.5f;

        [SerializeField]
        private bool _shouldPerformPings;
        private float _currentPingRadius;


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

        const float MAP_SEGMENT_SIZE = 2.5f;    // In metres.



        private void OnEnable()
        {
            LocatableManager.OnLocatableAdded += OnLocatableAdded;
            LocatableManager.OnLocatableRemoved += OnLocatableRemoved;

            Actions.Action.OnLoudActionTriggered_Client += OnLoudActionTriggered;
        }
        private void OnDisable()
        {
            LocatableManager.OnLocatableAdded -= OnLocatableAdded;
            LocatableManager.OnLocatableRemoved -= OnLocatableRemoved;

            Actions.Action.OnLoudActionTriggered_Client -= OnLoudActionTriggered;
        }

        private void OnLocatableAdded(BaseLocatable locatable)
        {
            if (locatable == null || _locatableIconDict.ContainsKey(locatable))
                return; // We've already added this locatable (Or it's invalid).

            // Create the corresponding icon for the locatable.
            BaseLocatableIcon icon = locatable.CreateIcon();
            icon.transform.SetParent(_iconContainer, false);

            // Ensure that the icon starts hidden, just in case.
            icon.SetVisible(false);

            locatable.OnLocatableTypeChanged += BaseLocatable_OnLocatableTypeChanged;

            // Cache the locatable and icon.
            _locatableIconDict.Add(locatable, icon);

            // Cache the locatable with any information required for the desired operation.
            DesiredOperation desiredOperation = GetDesiredDisplayOperation(locatable.LocatableType);
            _functionToLocatablesDict.GetOrCreateAndReturnValue(desiredOperation).Add(locatable);

            if (desiredOperation == DesiredOperation.PingLogic)
                _tempPingTimings.Add(locatable, (-1.0f, -1.0f));
        }
        private void OnLocatableRemoved(BaseLocatable locatable)
        {
            if (locatable == null || !_locatableIconDict.TryGetValue(locatable, out BaseLocatableIcon icon))
                return; // We've already removed this locatable (Or it's invalid).

            locatable.OnLocatableTypeChanged -= BaseLocatable_OnLocatableTypeChanged;

            // Remove the locatable.
            _locatableIconDict.Remove(locatable);

            _functionToLocatablesDict.GetOrCreateAndReturnValue(GetDesiredDisplayOperation(locatable.LocatableType)).Remove(locatable);
            _tempPingTimings.Remove(locatable);

            // Cleanup the locatable's icon.
            Destroy(icon.gameObject);
        }

        private void BaseLocatable_OnLocatableTypeChanged(BaseLocatable locatable, LocatableType oldValue)
        {
            DesiredOperation desiredOperation = GetDesiredDisplayOperation(locatable.LocatableType);

            _functionToLocatablesDict[GetDesiredDisplayOperation(oldValue)].Remove(locatable);
            _functionToLocatablesDict.GetOrCreateAndReturnValue(desiredOperation).Add(locatable);

            if (desiredOperation == DesiredOperation.PingLogic)
                _tempPingTimings.Add(locatable, (-1.0f, -1.0f));
            else
                _tempPingTimings.Remove(locatable);
        }

        private void OnLoudActionTriggered(Actions.Action loudAction, Gameplay.GameplayObjects.Character.ServerCharacter owningCharacter)
        {
            if (owningCharacter.TeamID == Player.LocalClientInstance.ServerCharacter.TeamID)
                return; // Actions from the same team don't count as dangerous.

            Vector2Int quadrantIndicies = PositionToMapQuad(loudAction.GetActionOrigin());

            const float DANGER_TIME = 5.0f;
            float fadeTime = Time.time + DANGER_TIME;

            // Cache the origin position of the action for a desired duration.
            _dangerQuadrants.AddOrSet(quadrantIndicies, fadeTime);
        }


        private void Update()
        {
            if (Player.LocalClientInstance == null)
                return;

            UpdateLocatableIconPositions();

            UpdateDefaultLocatableIcons();
            if (_shouldPerformPings)
                UpdateRadarPing();
            UpdateDangerIndicators();

            CleanupOldDangerInformation();
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

            foreach (BaseLocatable locatable in _functionToLocatablesDict[DesiredOperation.DefaultLogic])
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
        ///     Start the radar pinging at the desired frequency.
        /// </summary>
        /// <param name="pingFrequency"> The number of pings performed every second.</param>
        public void StartRadarPings(float pingFrequency)
        {
            _currentPingRadius = 0.0f;
            _pingFrequency = pingFrequency;
            _shouldPerformPings = true;

            ResetRadarPing();
        }
        /// <summary>
        ///     Cancel the current radar ping and prevent future ones from automatically occuring.
        /// </summary>
        public void StopRadarPings()
        {
            _shouldPerformPings = false;

            // Hide the ping visuals.
            _pingIndicator.CrossFadeAlpha(0.0f, 0.0f, true);

            // Hide all current pings.
            ResetRadarPing();
        }
        /// <summary>
        ///     Performs all logic necessary to update the radar ping.<br/>
        ///     Includes increasing the radius, updating the targets & the alpha of their indicators, and updating the scale & alpha of the indication ring.
        /// </summary>
        private void UpdateRadarPing()
        {
            // Cache repeatedly used values.
            float radarRadius = GetRadarUIRadius();
            float scale = radarRadius / _detectionRange;

            // Update our ping distance.
            float previousPingDistance = _currentPingRadius;
            _currentPingRadius += radarRadius * _pingFrequency * Time.deltaTime;

            UpdatePingVisuals();
            UpdatePingedIcons(previousPingDistance);


            // If we've completed a ping, reset our values for a new ping starting next frame.
            if (_currentPingRadius > radarRadius)
                ResetRadarPing();
        }
        /// <summary>
        ///     Updates the scale and alpha of the ping indicator.
        /// </summary>
        private void UpdatePingVisuals()
        {
            float pingPercentage = _currentPingRadius / GetRadarUIRadius();
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
        private void UpdatePingedIcons(float previousPingDistance)
        {
            // Cache sqr values for more performant distance calculation.
            float sqrPreviousDistance = previousPingDistance * previousPingDistance;
            float sqrCurrentDistance = _currentPingRadius * _currentPingRadius;


            // Ping locatables now within our ping radius.
            foreach (Locatable locatable in _functionToLocatablesDict.GetOrCreateAndReturnValue(DesiredOperation.PingLogic))
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
        ///     Resets all the values for a ping to allow for a new one to start.
        /// </summary>
        private void ResetRadarPing()
        {
            _currentPingRadius = 0.0f;

            // Reset our cached ping timings and hide active indicators.
            List<BaseLocatable> keys = new List<BaseLocatable>(_tempPingTimings.Keys);
            foreach (BaseLocatable locatable in keys)
            {
                _tempPingTimings[locatable] = (-1.0f, _tempPingTimings[locatable].PingEffectStartTime);
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
            foreach(var quadrantIndicies in _dangerQuadrants.Keys)
            {
                Vector2 dangerLocalPosition = GetDistanceVectorToPlayer(MapQuadToPosition(quadrantIndicies));
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
        ///     Converts a world position into a quadrant position.<br/>
        ///     Quadrants are a simplification of the map into index-based squares, used to reduce the amount of data we need to store.
        /// </summary>
        private Vector2Int PositionToMapQuad(Vector3 position) => new Vector2Int(Mathf.RoundToInt(position.x / MAP_SEGMENT_SIZE), Mathf.RoundToInt(position.z / MAP_SEGMENT_SIZE));
        /// <summary>
        ///     Converts a quadrant index vector into a position vector representing an object's horizontal position (x=x, y=z).
        /// </summary>
        private Vector2 MapQuadToPosition(Vector2Int quadrantIndicies) => new Vector2(quadrantIndicies.x * MAP_SEGMENT_SIZE, quadrantIndicies.y * MAP_SEGMENT_SIZE);

        /// <summary>
        ///     Removes any quadrants within '_dangerQuadrants' that are no longer 'loud'.
        /// </summary>
        private void CleanupOldDangerInformation()
        {
            // Find the quadrants that are no longer 'loud'.
            List<Vector2Int> quadrantsToRemove = new List<Vector2Int>();
            foreach(var kvp in _dangerQuadrants)
                if (Time.time > kvp.Value)
                    quadrantsToRemove.Add(kvp.Key);

            // Remove our desired quadrants from the dictionary.
            for (int i = 0; i < quadrantsToRemove.Count; i++)
                _dangerQuadrants.Remove(quadrantsToRemove[i]);
        }



        /// <summary>
        ///     Returns an enum corresponding to the desired operation for the locatable.
        /// </summary>
        private DesiredOperation GetDesiredDisplayOperation(LocatableType locatableType)
        {
            switch (locatableType)
            {
                case LocatableType.Objective:
                case LocatableType.Friendly:
                    return DesiredOperation.DefaultLogic;

                case LocatableType.Enemy:
                    return DesiredOperation.PingLogic;

                default: throw new System.NotImplementedException($"No Radar DesiredOperation value set for {locatableType.ToString()}");
            };
        }
        private enum DesiredOperation { DefaultLogic, PingLogic }


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