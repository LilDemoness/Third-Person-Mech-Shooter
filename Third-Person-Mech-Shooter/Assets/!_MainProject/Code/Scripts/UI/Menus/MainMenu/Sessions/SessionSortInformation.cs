using System.Collections.Generic;
using System.Linq;
using Unity.Services.Multiplayer;

namespace Gameplay.UI.Menus.Session
{
    public enum SessionSortField
    {
        Default = 0,
        RoomName,
        GameMode,
        Map,
        PlayerCount,
    }
    public enum SessionSortOrder
    {
        Ascending,
        Descending
    }


    public static class SessionSortFieldExtensions
    {
        private static Dictionary<SessionSortField, string> s_sortFieldToDisplayNameDict;

        public static string ToDisplayString(this SessionSortField sessionSortField)
        {
            if (s_sortFieldToDisplayNameDict.TryGetValue(sessionSortField, out string displayString))
                return displayString;

            throw new System.ArgumentException($"Invalid Argument: {sessionSortField}");
        }
        public static string[] GetDisplayStrings() => s_sortFieldToDisplayNameDict.Values.ToArray();


        public static SortField ToSortField(this SessionSortField sessionSortField) => sessionSortField switch
        {
            SessionSortField.Default => SortField.CreationTime,
            SessionSortField.RoomName => SortField.Name,
            SessionSortField.GameMode => Constants.GAME_MODE_SORT_FIELD,
            SessionSortField.Map => Constants.MAP_SORT_FIELD,
            SessionSortField.PlayerCount => SortField.AvailableSlots,
            _ => throw new System.NotImplementedException()
        };
    }

    public static class SessionSortOrderExtensions
    {
        public static SortOrder ToSortOrder(this SessionSortOrder sortOrder) => sortOrder == SessionSortOrder.Ascending ? SortOrder.Ascending : SortOrder.Descending;
        public static SessionSortOrder ToSessionSortOrder(this SortOrder sortOrder) => sortOrder == SortOrder.Ascending ? SessionSortOrder.Ascending : SessionSortOrder.Descending;
    }


    public static class SortFieldExtensions
    {
        public static SortOrder GetDefaultSortOrder(this SortField sortField, bool isInverted = false)
        {
            switch (sortField)
            {
                case SortField.AvailableSlots:
                    return isInverted == false ? SortOrder.Ascending : SortOrder.Descending;
                
                default: return isInverted == false ? SortOrder.Descending : SortOrder.Ascending;
            }
        }
    }
}