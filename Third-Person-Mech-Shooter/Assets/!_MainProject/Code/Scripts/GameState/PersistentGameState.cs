using System.Collections.Generic;
using SceneLoading;
using UnityEngine;

namespace Gameplay.GameState
{
    /// <summary>
    ///     A class containing some data that needs to be passed between states to represent the session's win state.<br/>
    /// </summary>
    public class PersistentGameState
    {
        const GameMode DEFAULT_GAME_MODE = GameMode.FreeForAll;
        const string DEFAULT_MAP_NAME = "TestGameMap";


        private GameMode m_gameMode;
        public GameMode GameMode
        {
            get => m_gameMode;
            set
            {
                m_gameMode = value;
                OnGameStateDataChanged?.Invoke();
            }
        }

        private string m_mapName;
        public string MapName
        {
            get => m_mapName;
            set
            {
                if (!SceneLoader.IsValidMapName(value))
                {
                    Debug.LogWarning($"Map name \"{value}\" is invalid");
                    return;
                }

                m_mapName = value;
                OnGameStateDataChanged?.Invoke();
            }
        }



        private PersistentDataContainer _test;


        public void Init()
        {
            GameMode = DEFAULT_GAME_MODE;
            MapName = DEFAULT_MAP_NAME;
        }


        public void SetContainer<T>() where T : PersistentDataContainer, new() => _test = new T();
        public void SetContainer<T>(T newValue) where T : PersistentDataContainer => _test = newValue;
        public T GetContainer<T>() where T : PersistentDataContainer => _test as T;

        public void AssertContainerType<T>() where T : PersistentDataContainer
        {
            Debug.Assert(_test != null, $"We are trying to check the container type of {nameof(_test)} but it is still unset");
            Debug.Assert(_test.GetType() != typeof(T), $"Container Types Don't Match (Desired: '{typeof(T).ToString()}'. Actual: '{_test.GetType().ToString()}')");
        }


        public void Reset()
        {
            _test = null;
        }


        public event System.Action OnGameStateDataChanged;
        public void SubscribeToChangeAndCall(System.Action onDataChangedCallback)
        {
            OnGameStateDataChanged += onDataChangedCallback;
            onDataChangedCallback();
        }
    }


    public abstract class PersistentDataContainer
    { }
    public class FFAPersistentData : PersistentDataContainer
    {
        public FFAPostGameData[] GameData { get; set; }
    }
    public class TDMPersistentData : PersistentDataContainer
    {
        public int Test { get; set; }
    }
}