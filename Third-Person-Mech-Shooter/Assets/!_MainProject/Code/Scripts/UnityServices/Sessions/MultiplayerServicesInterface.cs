using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace UnityServices.Sessions
{
    /// <summary>
    ///     A wrapper for all the interactions with the Sessions API.
    /// </summary>
    public class MultiplayerServicesInterface
    {
        private const int MAX_SESSIONS_TO_SHOW = 16;    // If more are necessary, consider retrieving paginated results or using filters.
        private const int MAX_PLAYERS = 8;

        private readonly List<FilterOption> _defaultFilterOptions;
        private readonly List<SortOption> _defaultSortOptions;
        private List<FilterOption> _filterOptions;
        private List<SortOption> _sortOptions;


        public MultiplayerServicesInterface()
        {
            // Filter for open sessions only.
            _defaultFilterOptions = new List<FilterOption>
            {
                new(FilterField.AvailableSlots, "0", FilterOperation.Greater)
            };

            // Order by fullness, then newest first.
            _defaultSortOptions = new List<SortOption>()
            {
                new(SortOrder.Descending, SortField.AvailableSlots),
                new(SortOrder.Descending, SortField.CreationTime),
            };


            _filterOptions = new();
            _sortOptions = new();
        }


        /// <summary>
        ///     Create a session with the given parameters.
        /// </summary>
        public async Task<ISession> CreateSession(string sessionName, int maxPlayers, bool isPrivate, string sessionPassword, Dictionary<string, PlayerProperty> playerProperties, Dictionary<string, SessionProperty> sessionProperties)
        {
            SessionOptions sessionOptions = new SessionOptions
            {
                Name = sessionName,
                MaxPlayers = maxPlayers,
                IsPrivate = isPrivate,
                IsLocked = false,
                Password = sessionPassword,
                PlayerProperties = playerProperties,
                SessionProperties = sessionProperties,
            }.WithRelayNetwork();

            return await MultiplayerService.Instance.CreateSessionAsync(sessionOptions);
        }

        /// <summary>
        ///     Join the session with the given code.
        /// </summary>
        public async Task<ISession> JoinSessionByCode(string sessionCode, Dictionary<string, PlayerProperty> localUserData)
        {
            JoinSessionOptions joinSessionOptions = new JoinSessionOptions
            {
                PlayerProperties = localUserData,
            };
            return await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCode, joinSessionOptions);
        }

        /// <summary>
        ///     Join the session with the given id.
        /// </summary>
        public async Task<ISession> JoinSessionById(string sessionId, Dictionary<string, PlayerProperty> localUserData)
        {
            JoinSessionOptions joinSessionOptions = new JoinSessionOptions
            {
                PlayerProperties = localUserData,
            };
            return await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId, joinSessionOptions);
        }

        /// <summary>
        ///     Join the first available session.
        /// </summary>
        public async Task<ISession> QuickJoinSession(Dictionary<string, PlayerProperty> localUserData, bool ignoreFilters = false)
        {
            QuickJoinOptions quickJoinOptions = new QuickJoinOptions
            {
                Filters = GetFilterOptions(ignoreFilters),
                CreateSession = true,   // Create a matching session if no matching session was found.
            };

            SessionOptions sessionOptions = new SessionOptions
            {
                MaxPlayers = MAX_PLAYERS,
                PlayerProperties = localUserData,
            }.WithRelayNetwork();

            return await MultiplayerService.Instance.MatchmakeSessionAsync(quickJoinOptions, sessionOptions);
        }



        /// <summary>
        ///     Retrieve a list of all sessions, filtered and sorted by the active filters & sort mode.
        /// </summary>
        public async Task<QuerySessionsResults> QuerySessions()
        {
            QuerySessionsOptions querySessionsOptions = new QuerySessionsOptions
            {
                Count = MAX_SESSIONS_TO_SHOW,
                FilterOptions = GetFilterOptions(ignoreFilters: false),
                SortOptions = GetSortOptions(),
            };

            return await MultiplayerService.Instance.QuerySessionsAsync(querySessionsOptions);
        }
        /// <summary>
        ///     Attempt to reconnect to the session with the corresponding sessionId.
        /// </summary>
        public async Task<ISession> ReconnectToSession(string sessionId) => await MultiplayerService.Instance.ReconnectToSessionAsync(sessionId);



        #region Filter Options

        private List<FilterOption> GetFilterOptions(bool ignoreFilters) 
        {
            List<FilterOption> filters = new List<FilterOption>(ignoreFilters ? _defaultFilterOptions.Count : _defaultFilterOptions.Count + _filterOptions.Count);
            filters.AddRange(_defaultFilterOptions);
            if (!ignoreFilters) { filters.AddRange(_filterOptions); }

            return filters;
        }
        /// <summary>
        ///     If no FilterOptions with the same Field exist, adds the passed FilterOption to the filters.
        ///     Otherwise, replaces the FilterOption with the one passed.
        /// </summary>
        private void SetFilterOptionForField(FilterOption filterOption, bool replaceIfFound = true)   // Note: Doesn't re-query.
        {
            if (ClearFilter(filterOption.Field))
            {
                if (!replaceIfFound)
                    return; // We're not wanting to replace the field, just remove it.
            }

            _filterOptions.Add(filterOption);
        }

        public void ClearFilters() => _filterOptions.Clear();
        /// <returns> True if a filter was removed. Otherwise, false.</returns>
        public bool ClearFilter(FilterField filterField)
        {
            for (int i = 0; i < _filterOptions.Count; ++i)
            {
                if (_filterOptions[i].Field == filterField)
                {
                    // Found a filter with the same field.
                    _filterOptions.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }



        const FilterField GAME_MODE_QUERY_FIELD = FilterField.StringIndex1;
        const FilterField MAP_QUERY_FIELD = FilterField.StringIndex2;


        public void SetGameModeFilter(Gameplay.GameMode gameMode) => SetFilterOptionForField(new FilterOption(GAME_MODE_QUERY_FIELD, gameMode.ToString(), FilterOperation.Equal));
        public void SetMapFilter(string mapName) => SetFilterOptionForField(new FilterOption(MAP_QUERY_FIELD, mapName, FilterOperation.Contains));
        public void SetShowPasswordProtectedLobbies(bool showPasswordProtectedLobbies)
        {
            if (showPasswordProtectedLobbies)
                ClearFilter(FilterField.HasPassword); // Allow password protected lobbies = no password protection filter.
            else
                _filterOptions.Add(new FilterOption(FilterField.HasPassword, false.ToString(), FilterOperation.Equal)); // Hide password protected lobbies = Apply filter to show only non-password protected.
        }

        public void ClearGameModeFilter() => ClearFilter(GAME_MODE_QUERY_FIELD);
        public void ClearMapFilter() => ClearFilter(MAP_QUERY_FIELD);

        #endregion


        #region Sort Options

        private List<SortOption> GetSortOptions() => _sortOptions.Count > 0 ? _sortOptions : _defaultSortOptions;
        public void ClearSortOptions() => _sortOptions.Clear();

        #endregion
    }
}