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
                new(FilterField.AvailableSlots, "0", FilterOperation.Greater),
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
        public async Task<ISession> CreateSession(string sessionName, int maxPlayers, bool isPrivate, Dictionary<string, PlayerProperty> playerProperties, Dictionary<string, SessionProperty> sessionProperties)
        {
            SessionOptions sessionOptions = new SessionOptions
            {
                Name = sessionName,
                MaxPlayers = maxPlayers,
                IsPrivate = isPrivate,
                IsLocked = false,
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
        private void ToggleFilterOption(FilterOption filterOption)   // Note: Doesn't re-query.
        {
            if (_filterOptions.Contains(filterOption))
                _filterOptions.Remove(filterOption);
            else
                _filterOptions.Add(filterOption);
        }

        public void ClearFilters() => _filterOptions.Clear();



        const FilterField GAME_MODE_QUERY_FIELD = FilterField.StringIndex1;
        const FilterField MAP_QUERY_FIELD = FilterField.StringIndex2;


        public void ToggleGameModeFilter(Gameplay.GameMode gameMode) => ToggleFilterOption(new FilterOption(GAME_MODE_QUERY_FIELD, gameMode.ToString(), FilterOperation.Contains));
        public void ToggleMapFilter(string mapName) => ToggleFilterOption(new FilterOption(MAP_QUERY_FIELD, mapName, FilterOperation.Contains));

        #endregion


        #region Sort Options

        private List<SortOption> GetSortOptions() => _sortOptions.Count > 0 ? _sortOptions : _defaultSortOptions;
        public void ClearSortOptions() => _sortOptions.Clear();

        #endregion
    }
}