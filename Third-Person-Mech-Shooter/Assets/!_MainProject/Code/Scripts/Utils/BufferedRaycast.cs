using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utils
{
    /// <summary>
    ///     Allows performing a multi-target raycast which should be more consistent than RaycastAll/RaycastAllNonAlloc while still being fairly performant.<br/>
    ///     Results are always returned in ascending distance order due to the nature of the multiple raycast calls.
    /// </summary>
    /// <remarks> Inspired by: 'https://community.gamedev.tv/t/buffered-raycast/171755/4'.</remarks>
    public class BufferedRaycast
    {
        private const int DEFAULT_BUFFER_SIZE = 20;
        private const float RAYCAST_EPSILON_DISTANCE = 0.01f;


        private RaycastHit[] _raycastBuffer;
        private int _allowedSize;
        private readonly bool _canResize;

        private int m_hitSize;
        private int _hitSize
        {
            get => m_hitSize;
            set
            {
                if (value > _allowedSize)
                {
                    if (!_canResize)
                        throw new System.IndexOutOfRangeException("Trying to set 'm_hitSize' to an out-of-range value.");

                    DoubleSizeOfBuffers();
                }
                m_hitSize = value;
            }
        }


        /// <summary>
        ///     Creates a new <see cref="BufferedRaycast"/> instance.
        /// </summary>
        /// <param name="allowedSize"> The max number of <see cref="GameObject"/> hits that can be returned from the raycast.</param>
        public BufferedRaycast(int allowedSize, bool canResize = false)
        {
            _allowedSize = allowedSize;
            _raycastBuffer = new RaycastHit[allowedSize];
            _canResize = canResize;
        }
        public BufferedRaycast() : this(DEFAULT_BUFFER_SIZE, true)
        { }


        #region Default Raycast

        /// <summary>
        ///     Performs a raycast using the allocated buffers.
        /// </summary>
        /// <param name="ray"> The <see cref="Ray"/> to be used for the raycast.</param>
        /// <param name="maxDistance"> The maximum distance to travel along the <see cref="Ray"/>.</param>
        /// <param name="maxHitsOverride"> A temporary override for the maximum number of objects this ray will hit. Clamped to the buffer size.</param>
        /// <param name="layerMask"> (Optional) The <see cref="LayerMask"/> that will be used for the raycast.</param>
        /// <returns> The result of the raycast.</returns>
        public IEnumerable<RaycastHit> Raycast(Ray ray, float maxDistance = Mathf.Infinity, int maxHitsOverride = -1, int layerMask = ~0)
        {
            // Perform the raycast
            HandleRaycastCall(ray, maxDistance, maxHitsOverride, layerMask);

            // Return our raycast hit info.
            for (int i = 0; i < _hitSize; ++i)
                yield return _raycastBuffer[i];
        }
        /// <inheritdoc cref="Raycast(Ray, float, int, int)"/>
        /// <param name="origin"> The origin of the <see cref="Ray"/> to be used for the raycast.</param>
        /// <param name="direction"> The direction of the <see cref="Ray"/> to be used for the raycast.</param>
        public IEnumerable<RaycastHit> Raycast(Vector3 origin, Vector3 direction, float maxDistance = Mathf.Infinity, int maxHitsOverride = -1, int layerMask = ~0)
            => Raycast(new Ray(origin, direction), maxDistance, maxHitsOverride, layerMask);


        /// <summary>
        ///     Performs a raycast for the first object along the ray, if one exists.
        /// </summary>
        /// <param name="ray"> The <see cref="Ray"/> to be used for the raycast.</param>
        /// <param name="hitInfo"> The output <see cref="RaycastHit"/> of the Raycast, or default if there were no hits.</param>
        /// <param name="maxDistance"> The maximum distance to travel along the <see cref="Ray"/>.</param>
        /// <param name="layerMask"> (Optional) The <see cref="LayerMask"/> that will be used for the raycast.</param>
        /// <returns> True if a valid object was hit, otherwise false.</returns>
        public bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance = Mathf.Infinity, int layerMask = ~0)
        {
            IEnumerable<RaycastHit> hits = Raycast(ray, maxDistance, 1, layerMask);

            if (hits.IsNullOrEmpty())
            {
                hitInfo = default(RaycastHit);
                return false;
            }

            hitInfo = hits.First();
            return true;
        }
        /// <inheritdoc cref="Raycast(Ray, out RaycastHit, float, int)"/>
        /// <param name="origin"> The origin of the <see cref="Ray"/> to be used for the raycast.</param>
        /// <param name="direction"> The direction of the <see cref="Ray"/> to be used for the raycast.</param>
        public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance = Mathf.Infinity, int layerMask = ~0)
            => Raycast(new Ray(origin, direction), out hitInfo, maxDistance, layerMask);


        #region Linecast Variant

        /// <inheritdoc cref="Raycast(Ray, float, int, int)"/>
        /// <param name="start"> The start position of the linecast.</param>
        /// <param name="end"> The end position of the linecast.</param>
        public IEnumerable<RaycastHit> Linecast(Vector3 start, Vector3 end, int maxHitsOverride = -1, int layerMask = ~0)
        {
            Vector3 direction = (end - start);
            return Raycast(new Ray(start, direction), direction.magnitude, maxHitsOverride, layerMask);
        }
        /// <inheritdoc cref="Raycast(Ray, out RaycastHit, float, int)"/>
        /// <param name="start"> The start position of the linecast.</param>
        /// <param name="end"> The end position of the linecast.</param>
        public bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo, int layerMask = ~0)
        {
            IEnumerable<RaycastHit> hits = Linecast(start, end, 1, layerMask);

            if (hits.IsNullOrEmpty())
            {
                hitInfo = default(RaycastHit);
                return false;
            }

            hitInfo = hits.First();
            return true;
        }

        #endregion

        #endregion


        #region Conditional Raycast

        /// <summary>
        ///     Performs a raycast using the allocated buffers.<br/>
        ///     If a hit object doesn't fullfil the passed condition, then it is excluded from the results.
        /// </summary>
        /// <param name="ray"> The <see cref="Ray"/> to be used for the raycast.</param>
        /// <param name="condition"> The condition that a hit target must fullfil to be included in the results.</param>
        /// <param name="maxDistance"> The maximum distance to travel along the <see cref="Ray"/>.</param>
        /// <param name="maxHitsOverride"> A temporary override for the maximum number of objects this ray will hit. Clamped to the buffer size.</param>
        /// <param name="layerMask"> (Optional) The <see cref="LayerMask"/> that will be used for the raycast.</param>
        /// <returns> The calid results from the raycast.</returns>
        public IEnumerable<RaycastHit> ConditionalRaycast(Ray ray, System.Func<RaycastHit, bool> condition, float maxDistance = Mathf.Infinity, int maxHitsOverride = -1, int layerMask = ~0)
        {
            // Perform the conditional raycast.
            HandleConditionalRaycastCall(ray, condition, maxDistance, maxHitsOverride, layerMask);

            // Return our raycast hit info.
            for (int i = 0; i < _hitSize; ++i)
                yield return _raycastBuffer[i];
        }
        /// <inheritdoc cref="ConditionalRaycast(Ray, Func{RaycastHit, bool}, float, int, int)"/>
        /// <param name="origin"> The origin of the <see cref="Ray"/> to be used for the raycast.</param>
        /// <param name="direction"> The direction of the <see cref="Ray"/> to be used for the raycast.</param>
        public IEnumerable<RaycastHit> ConditionalRaycast(Vector3 origin, Vector3 direction, System.Func<RaycastHit, bool> condition, float maxDistance = Mathf.Infinity, int maxHitsOverride = -1, int layerMask = ~0)
            => ConditionalRaycast(new Ray(origin, direction), condition, maxDistance, maxHitsOverride, layerMask);


        /// <summary>
        ///     Performs a raycast for the first object along the ray that fullfils the passed condition, if one exists.
        /// </summary>
        /// <param name="ray"> The <see cref="Ray"/> to be used for the raycast.</param>
        /// <param name="hitInfo"> The output <see cref="RaycastHit"/> of the Raycast, or default if there were no hits.</param>
        /// <param name="maxDistance"> The maximum distance to travel along the <see cref="Ray"/>.</param>
        /// <param name="layerMask"> (Optional) The <see cref="LayerMask"/> that will be used for the raycast.</param>
        /// <returns> True if a valid object was hit, otherwise false.</returns>
        public bool ConditionalRaycast(Ray ray, System.Func<RaycastHit, bool> condition, out RaycastHit hitInfo, float maxDistance = Mathf.Infinity, int layerMask = ~0)
        {
            IEnumerable<RaycastHit> hits = ConditionalRaycast(ray, condition, maxDistance, 1, layerMask);

            if (hits.IsNullOrEmpty())
            {
                hitInfo = default(RaycastHit);
                return false;
            }

            hitInfo = hits.First();
            return true;
        }
        /// <inheritdoc cref="ConditionalRaycast(Ray, Func{RaycastHit, bool}, out RaycastHit, float, int)"/>
        /// <param name="origin"> The origin of the <see cref="Ray"/> to be used for the raycast.</param>
        /// <param name="direction"> The direction of the <see cref="Ray"/> to be used for the raycast.</param>
        public bool ConditionalRaycast(Vector3 origin, Vector3 direction, System.Func<RaycastHit, bool> condition, out RaycastHit hitInfo, float maxDistance = Mathf.Infinity, int layerMask = ~0)
            => ConditionalRaycast(new Ray(origin, direction), condition, out hitInfo, maxDistance, layerMask);


        #region Linecast Variant

        /// <inheritdoc cref="ConditionalRaycast(Ray, Func{RaycastHit, bool}, float, int, int)"/>
        /// <param name="start"> The start position of the linecast.</param>
        /// <param name="end"> The end position of the linecast.</param>
        public IEnumerable<RaycastHit> ConditionalLinecast(Vector3 start, Vector3 end, System.Func<RaycastHit, bool> condition, int maxHitsOverride = -1, int layerMask = ~0)
        {
            Vector3 direction = (end - start);
            return ConditionalRaycast(new Ray(start, direction), condition, direction.magnitude, maxHitsOverride, layerMask);
        }
        /// <inheritdoc cref="ConditionalRaycast(Ray, Func{RaycastHit, bool}, out RaycastHit, float, int)"/>
        /// <param name="start"> The start position of the linecast.</param>
        /// <param name="end"> The end position of the linecast.</param>
        public bool ConditionalLinecast(Vector3 start, Vector3 end, System.Func<RaycastHit, bool> condition, out RaycastHit hitInfo, int layerMask = ~0)
        {
            IEnumerable<RaycastHit> hits = ConditionalLinecast(start, end, condition, 1, layerMask);

            if (hits.IsNullOrEmpty())
            {
                hitInfo = default(RaycastHit);
                return false;
            }

            hitInfo = hits.First();
            return true;
        }

        #endregion

        #endregion


        #region Filtered Raycast

        /// <summary>
        ///     Returns all the <see cref="MonoBehaviour"/> components of type <typeparamref name="T"/> on the GameObjects hit by the raycast.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ray"> The <see cref="Ray"/> to be used for the raycast.</param>
        /// <param name="maxDistance"> The maximum distance to travel along the <see cref="Ray"/>.</param>
        /// <param name="maxHitsOverride"> A temporary override for the maximum number of objects this ray will hit. Clamped to the buffer size.</param>
        /// <param name="layerMask"> (Optional) The <see cref="LayerMask"/> that will be used for the raycast.</param>
        /// <returns> All the <typeparamref name="T"/> <see cref="MonoBehaviour"/> results from the raycast.</returns>
        public IEnumerable<T> FilteredRaycast<T>(Ray ray, float maxDistance = Mathf.Infinity, int maxHitsOverride = -1, int layerMask = ~0) where T : MonoBehaviour
        {
            // Perform the raycast
            HandleRaycastCall(ray, maxDistance, maxHitsOverride, layerMask);

            // Return our filtered results.
            for (int i = 0; i < _hitSize; ++i)
                foreach(T component in _raycastBuffer[i].transform.GetComponents<T>())
                    yield return component;
        }
        /// <inheritdoc cref="FilteredRaycast{T}(Ray, float, int, int)"/>
        /// <param name="origin"> The origin of the <see cref="Ray"/> to be used for the raycast.</param>
        /// <param name="direction"> The direction of the <see cref="Ray"/> to be used for the raycast.</param>
        public IEnumerable<T> FilteredRaycast<T>(Vector3 origin, Vector3 direction, float maxDistance = Mathf.Infinity, int maxHitsOverride = -1, int layerMask = ~0) where T : MonoBehaviour
            => FilteredRaycast<T>(new Ray(origin, direction), maxDistance, maxHitsOverride, layerMask);


        #region Linecast Variant

        /// <inheritdoc cref="FilteredRaycast{T}(Ray, float, int, int)"/>
        /// <param name="start"> The start position of the linecast.</param>
        /// <param name="end"> The end position of the linecast.</param>
        public IEnumerable<T> FilteredLinecast<T>(Vector3 start, Vector3 end, int maxHitsOverride = -1, int layerMask = ~0) where T : MonoBehaviour
        {
            Vector3 direction = (end - start);
            return FilteredRaycast<T>(new Ray(start, direction), direction.magnitude, maxHitsOverride, layerMask);
        }

        #endregion

        #endregion


        #region Raycast Functions

        #region Standard Raycasts

        private void HandleRaycastCall(Ray ray, float maxDistance, int maxHitsOverride, int layerMask)
        {
            // Trigger the appropriate raycast call for our given parameters.
            if (maxDistance <= 0.0f || maxDistance == Mathf.Infinity)
                DoRaycastInfiniteRange(ray, maxHitsOverride, layerMask);
            else
                DoRaycast(ray, maxDistance, maxHitsOverride, layerMask);
        }
        private void DoRaycast(Ray ray, float maxDistance, int maxHitsOverride, int layerMask)
        {
            _hitSize = 0;

            // Cancellation values.
            float sqrMaxDistance = maxDistance * maxDistance;
            Vector3 initialOrigin = ray.origin;
            maxHitsOverride = GetMaxHits(maxHitsOverride);

            // Preventing infinite loops.
            Vector3 epsilonVector = ray.direction * RAYCAST_EPSILON_DISTANCE;

            do
            {
                if (!Physics.Raycast(ray, out RaycastHit hitInfo, maxDistance, layerMask))
                    break; // Nothing was hit, so we've reached the end of our ray.
                if ((hitInfo.point - initialOrigin).sqrMagnitude > sqrMaxDistance)
                    break; // We've exceeded our max range with this check. Don't return the value as its our of our max range.
                    
                // The hit was within our max range.
                // Cache the hit.
                _raycastBuffer[_hitSize] = hitInfo;
                ++_hitSize;


                // Prepare for the next raycast.
                ray.origin = hitInfo.point + epsilonVector;
            } while (HasntExceededMaxHits(maxHitsOverride)); // If we've performed all hits, we should exit.
        }
        private void DoRaycastInfiniteRange(Ray ray, int maxHitsOverride, int layerMask)
        {
            _hitSize = 0;

            // Cancellation values.
            maxHitsOverride = GetMaxHits(maxHitsOverride);

            // Preventing infinite loops.
            Vector3 epsilonVector = ray.direction * RAYCAST_EPSILON_DISTANCE;

            do
            {
                if (!Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, layerMask))
                    break; // Nothing was hit, so we've reached the end of our ray.

                // The hit was within our max range.
                // Cache the hit.
                _raycastBuffer[_hitSize] = hitInfo;
                ++_hitSize;

                // Prepare for the next raycast.
                ray.origin = hitInfo.point + epsilonVector;
            } while (HasntExceededMaxHits(maxHitsOverride)); // If we've performed all hits, we should exit.
        }


        #endregion


        #region Conditional Raycasts

        private void HandleConditionalRaycastCall(Ray ray, System.Func<RaycastHit, bool> condition, float maxDistance, int maxHitsOverride, int layerMask)
        {
            // Trigger the appropriate raycast call for our given parameters.
            if (maxDistance <= 0.0f || maxDistance == Mathf.Infinity)
                DoConditionalRaycastInfiniteRange(ray, condition, maxHitsOverride, layerMask);
            else
                DoConditionalRaycast(ray, condition, maxDistance, maxHitsOverride, layerMask);
        }
        private void DoConditionalRaycast(Ray ray, System.Func<RaycastHit, bool> condition, float maxDistance, int maxHitsOverride, int layerMask)
        {
            _hitSize = 0;

            // Cancellation values.
            float sqrMaxDistance = maxDistance * maxDistance;
            Vector3 initialOrigin = ray.origin;

            maxHitsOverride = GetMaxHits(maxHitsOverride);

            // Preventing infinite loops.
            Vector3 epsilonVector = ray.direction * RAYCAST_EPSILON_DISTANCE;

            // Perform the raycasts.
            do
            {
                if (!Physics.Raycast(ray, out RaycastHit hitInfo, maxDistance, layerMask))
                    break; // Nothing was hit, so we've reached the end of our ray.
                if ((hitInfo.point - initialOrigin).sqrMagnitude > sqrMaxDistance)
                    break; // We've exceeded our max range with this check. Don't return the value as its our of our max range.
                // The hit was within our max range.

                try
                {
                    if (condition(hitInfo)) // If the condition passes, record the hit.
                    {
                        // Cache the hit.
                        _raycastBuffer[_hitSize] = hitInfo;
                        ++_hitSize;
                    }
                }
                catch (System.Exception e)  // If an error occurs within the condition it will crash the game. Catch it here and log it rather than crashing the game.
                {
                    Debug.LogException(e);
                }

                // Prepare for the next raycast.
                ray.origin = hitInfo.point + epsilonVector;
            } while (HasntExceededMaxHits(maxHitsOverride)); // If we've performed all hits, we should exit.
        }
        private void DoConditionalRaycastInfiniteRange(Ray ray, System.Func<RaycastHit, bool> condition, int maxHitsOverride, int layerMask)
        {
            _hitSize = 0;

            // Cancellation values.
            maxHitsOverride = GetMaxHits(maxHitsOverride);

            // Preventing infinite loops.
            Vector3 epsilonVector = ray.direction * RAYCAST_EPSILON_DISTANCE;

            // Perform the raycasts.
            do
            {
                if (!Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, layerMask))
                    break; // Nothing was hit, so we've reached the end of our ray.

                try
                {
                    if (condition(hitInfo)) // If the condition passes, record the hit.
                    {
                        // Cache the hit.
                        _raycastBuffer[_hitSize] = hitInfo;
                        ++_hitSize;
                    }
                }
                catch (System.Exception e)  // If an error occurs within the condition it will crash the game. Catch it here and log it rather than crashing the game.
                {
                    Debug.LogException(e);
                }

                // Prepare for the next raycast.
                ray.origin = hitInfo.point + epsilonVector;
            } while (HasntExceededMaxHits(maxHitsOverride)); // If we've performed all hits, we should exit.
        }

        #endregion

        #endregion



        /// <summary>
        ///     Doubles the size of the buffered raycast's buffers without resetting the data.
        /// </summary>
        private void DoubleSizeOfBuffers()
        {
            _allowedSize *= 2;
            Array.Resize(ref _raycastBuffer, _allowedSize);
        }

        /// <summary>
        ///     Returns the value for 'maxHits' given this <see cref="BufferedRaycast"/> instance's parameters and the passed <paramref name="maxHitsOverride"/>.
        /// </summary>
        /// <param name="maxHitsOverride"></param>
        /// <returns> The number of max hits to check. Less-than-zero if no max hit check should be performed.</returns>
        private int GetMaxHits(int maxHitsOverride)
        {
            return _canResize ? 
                maxHitsOverride :
                maxHitsOverride > 0 ? Mathf.Min(maxHitsOverride, _allowedSize) : _allowedSize;
        }
        private bool HasntExceededMaxHits(int maxHits) => maxHits > 0 && _hitSize < maxHits;
    }
}