using System.Collections.Generic;
using System.Linq;

namespace Gameplay
{
    /// <summary>
    ///     Enum of the different types of GameMode within the game.
    /// </summary>
    [System.Serializable]
    public enum GameMode
    {
        Invalid = -1,

        // Game Modes (Ensure their indicies are contiguous).
        FreeForAll = 0,
        TeamDeathmatch,
        KingOfTheHill
    }

    /// <summary>
    ///     Extension functions for the <see cref="GameMode"/> enum.
    /// </summary>
    public static class GameModeExtensions
    {
        public static GameMode[] GetAllGameModes() => new GameMode[]
        {
            GameMode.FreeForAll,
            GameMode.TeamDeathmatch,
            GameMode.KingOfTheHill,
        };

        public static List<string> GetAllGameModeNames() => new List<string>()
        {
            GameMode.FreeForAll.ToDisplayName(),
            GameMode.TeamDeathmatch.ToDisplayName(),
            GameMode.KingOfTheHill.ToDisplayName(),
        };
        public static List<string> GetAllGameModeAcronyms() => new List<string>()
        {
            GameMode.FreeForAll.ToAcronym(),
            GameMode.TeamDeathmatch.ToAcronym(),
            GameMode.KingOfTheHill.ToAcronym(),
        };


        public static string ToDisplayName(this GameMode gameMode)
        {
            return gameMode switch
            {
                GameMode.FreeForAll => "Free For All",
                GameMode.TeamDeathmatch => "Team Deathmatch",
                GameMode.KingOfTheHill => "King of the Hill",
                _ => throw new System.NotImplementedException($"No Display Name set for {gameMode.ToString()}"),
            };
        }
        public static string ToAcronym(this GameMode gameMode) => gameMode switch
        {
            GameMode.FreeForAll => "FFA",
            GameMode.TeamDeathmatch => "TDM",
            GameMode.KingOfTheHill => "KotH",
            _ => throw new System.NotImplementedException($"No Acronym set for {gameMode.ToString()}")
        };
    }
}