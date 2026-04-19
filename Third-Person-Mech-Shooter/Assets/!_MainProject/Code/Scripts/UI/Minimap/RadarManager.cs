using Gameplay.GameplayObjects.Players;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gameplay.UI.Minimap
{
    public class Minimap : MonoBehaviour
    {

    }

    [DefaultExecutionOrder(0)]    // Ensure this runs before RadarUI instances (Executed at order 10).
    public class RadarManager : LazySingleton<RadarManager>
    {
        private static readonly Dictionary<DesiredOperation, HashSet<BaseLocatable>> s_functionToLocatablesDict = new();

        public static readonly Dictionary<Vector2Int, float> DangerQuadrants = new();
        private const float MAP_SEGMENT_SIZE = 2.5f;    // In metres.


        private bool _shouldPerformPings;
        private float _pingFrequency;

        public float PreviousPingPercentage { get; private set; }
        public float CurrentPingPercentage { get; private set; }



        #region Events

        public static event System.Action<BaseLocatable, DesiredOperation> OnLocatableUpdated;    // Added or DesiredOperation changed.

        public static event System.Action OnPingStopped;
        public static event System.Action OnPingReset;

        #endregion


        #region MonoBehaviour Loop

        private void OnEnable()
        {
            // Ensure we know of any existing locatables.
            foreach(BaseLocatable locatable in LocatableManager.Locatables)
            {
                Debug.Log("Existing Locatables Include: " + locatable.name);
                OnLocatableAdded(locatable);
            }
            
            // Subscribe for when new locatables are added/old ones are removed.
            LocatableManager.OnLocatableAdded += OnLocatableAdded;
            LocatableManager.OnLocatableRemoved += OnLocatableRemoved;

            // Subscribe for 'loud actions'.
            Actions.Action.OnLoudActionTriggered_Client += OnLoudActionTriggered;
        }
        private void OnDisable()
        {
            // Unsubscribe from events.
            LocatableManager.OnLocatableAdded -= OnLocatableAdded;
            LocatableManager.OnLocatableRemoved -= OnLocatableRemoved;

            Actions.Action.OnLoudActionTriggered_Client -= OnLoudActionTriggered;
        }
        private void Update()
        {
            if (_shouldPerformPings)
                UpdateRadarPing();

            CleanupOldDangerInformation();
        }

        #endregion


        private void OnLocatableAdded(BaseLocatable locatable)
        {
            if (locatable == null)
                return; // The locatable is invalid.

            // If the locatable's type changed, we will want to update our cached values.
            locatable.OnLocatableTypeChanged += BaseLocatable_OnLocatableTypeChanged;

            // Cache the locatable with any information required for the desired operation.
            DesiredOperation desiredOperation = GetDesiredDisplayOperation(locatable.LocatableType);
            s_functionToLocatablesDict.GetOrCreateAndReturnValue(desiredOperation).Add(locatable);

            // Notify listeners of the Locatable having been added.
            OnLocatableUpdated.Invoke(locatable, desiredOperation);
        }
        private void OnLocatableRemoved(BaseLocatable locatable)
        {
            if (locatable == null)
            {
                Debug.Log("Invalid Locatable attempted to be removed");
                return; // The locatable is invalid.
            }

            Debug.Log("Locatable Removed from Radar Manager");

            locatable.OnLocatableTypeChanged -= BaseLocatable_OnLocatableTypeChanged;

            // Remove the locatable.
            s_functionToLocatablesDict.GetOrCreateAndReturnValue(GetDesiredDisplayOperation(locatable.LocatableType)).Remove(locatable);
        }

        private void BaseLocatable_OnLocatableTypeChanged(BaseLocatable locatable, LocatableType oldValue)
        {
            DesiredOperation desiredOperation = GetDesiredDisplayOperation(locatable.LocatableType);

            s_functionToLocatablesDict[GetDesiredDisplayOperation(oldValue)].Remove(locatable);
            s_functionToLocatablesDict.GetOrCreateAndReturnValue(desiredOperation).Add(locatable);

            // Notify listeners of the locatable's operation change.
            OnLocatableUpdated.Invoke(locatable, desiredOperation);
        }

        private void OnLoudActionTriggered(Actions.Action loudAction, Gameplay.GameplayObjects.Character.ServerCharacter owningCharacter)
        {
            if (owningCharacter.TeamID == Player.LocalClientInstance.ServerCharacter.TeamID)
                return; // Actions from the same team don't count as dangerous.

            Vector2Int quadrantIndicies = PositionToMapQuad(loudAction.GetActionOrigin());

            const float DANGER_TIME = 5.0f;
            float fadeTime = Time.time + DANGER_TIME;

            // Cache the origin position of the action for a desired duration.
            DangerQuadrants.AddOrSet(quadrantIndicies, fadeTime);
        }



        /// <summary>
        ///     Simulates the events that would have fired for a listener if they were subscribed from the first instance of this object's creation.
        /// </summary>
        public void SimulatePreviousEventsForListener(System.Action<BaseLocatable> onLocatableAddedCallback, System.Action<BaseLocatable, DesiredOperation> onLocatableUpdatedCallback)
        {
            IEnumerable<DesiredOperation> allDesiredOperationTypes = System.Enum.GetValues(typeof(DesiredOperation))
                .Cast<DesiredOperation>();

            foreach(DesiredOperation desiredOperation in allDesiredOperationTypes)
            {
                foreach(BaseLocatable locatable in s_functionToLocatablesDict.GetOrCreateAndReturnValue(desiredOperation))
                {
                    onLocatableAddedCallback?.Invoke(locatable);
                    onLocatableUpdatedCallback?.Invoke(locatable, desiredOperation);
                }
            }
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
        public enum DesiredOperation { DefaultLogic, PingLogic }


        #region Radar Pings

        /// <summary>
        ///     Start the radar pinging at the desired frequency.
        /// </summary>
        /// <param name="pingFrequency"> The number of pings performed every second.</param>
        public void StartRadarPings(float pingFrequency)
        {
            Instance.PreviousPingPercentage = 0.0f;
            Instance.CurrentPingPercentage = 0.0f;

            Instance._pingFrequency = pingFrequency;
            Instance._shouldPerformPings = true;

            ResetRadarPing();
        }

        /// <summary>
        ///     Cancel the current radar ping and prevent future ones from automatically occuring.
        /// </summary>
        public void StopRadarPings()
        {
            Instance._shouldPerformPings = false;

            // Hide the ping visuals.
            OnPingStopped?.Invoke();

            // Hide all current pings.
            ResetRadarPing();
        }

        /// <summary>
        ///     Resets all the values for a ping to allow for a new one to start.
        /// </summary>
        private void ResetRadarPing()
        {
            Instance.PreviousPingPercentage = 0.0f;
            Instance.CurrentPingPercentage = 0.0f;

            OnPingReset?.Invoke();
        }


        /// <summary>
        ///     Performs all logic necessary to update the radar ping.<br/>
        ///     Includes increasing the radius, updating the targets & the alpha of their indicators, and updating the scale & alpha of the indication ring.
        /// </summary>
        private void UpdateRadarPing()
        {
            // Update our ping distance.
            PreviousPingPercentage = CurrentPingPercentage;
            CurrentPingPercentage += _pingFrequency * Time.deltaTime;

            // If we've completed a ping, reset our values for a new ping starting next frame.
            if (CurrentPingPercentage > 1.0f)
                ResetRadarPing();
        }


        #endregion


        #region Dangerous Quadrants

        /// <summary>
        ///     Converts a world position into a quadrant position.<br/>
        ///     Quadrants are a simplification of the map into index-based squares, used to reduce the amount of data we need to store.
        /// </summary>
        public static Vector2Int PositionToMapQuad(Vector3 position) => new Vector2Int(Mathf.RoundToInt(position.x / MAP_SEGMENT_SIZE), Mathf.RoundToInt(position.z / MAP_SEGMENT_SIZE));
        /// <summary>
        ///     Converts a quadrant index vector into a position vector representing an object's horizontal position (x=x, y=z).
        /// </summary>
        public static Vector2 MapQuadToPosition(Vector2Int quadrantIndicies) => new Vector2(quadrantIndicies.x * MAP_SEGMENT_SIZE, quadrantIndicies.y * MAP_SEGMENT_SIZE);

        /// <summary>
        ///     Removes any quadrants within '_dangerQuadrants' that are no longer 'loud'.
        /// </summary>
        private void CleanupOldDangerInformation()
        {
            // Find the quadrants that are no longer 'loud'.
            List<Vector2Int> quadrantsToRemove = new List<Vector2Int>();
            foreach (var kvp in DangerQuadrants)
                if (Time.time > kvp.Value)
                    quadrantsToRemove.Add(kvp.Key);

            // Remove our desired quadrants from the dictionary.
            for (int i = 0; i < quadrantsToRemove.Count; i++)
                DangerQuadrants.Remove(quadrantsToRemove[i]);
        }

        #endregion


        public HashSet<BaseLocatable> GetLocatablesForOperation(DesiredOperation operation) => s_functionToLocatablesDict.GetOrCreateAndReturnValue(operation);
    }
}