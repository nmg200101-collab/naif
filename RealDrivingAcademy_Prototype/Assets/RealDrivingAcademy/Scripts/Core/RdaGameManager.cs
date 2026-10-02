using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using RealDrivingAcademy.Persistence;

namespace RealDrivingAcademy.Core
{
    [DefaultExecutionOrder(-1000)]
    public sealed class RdaGameManager : MonoBehaviour
    {
        public static RdaGameManager Instance { get; private set; }

        [SerializeField] GameFlowState flowState = GameFlowState.Boot;
        [SerializeField] GameSessionState session = new GameSessionState();

        RdaSceneLoader sceneLoader;
        RdaPersistenceService persistence;

        public GameFlowState FlowState => flowState;
        public GameSessionState Session => session;
        public RdaSceneLoader SceneLoader => sceneLoader;
        public RdaPersistenceService Persistence => persistence;

        public event Action<GameFlowState> FlowStateChanged;
        public event Action SessionChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            sceneLoader = GetComponent<RdaSceneLoader>();
            if (sceneLoader == null)
                sceneLoader = gameObject.AddComponent<RdaSceneLoader>();

            persistence = GetComponent<RdaPersistenceService>();
            if (persistence == null)
                persistence = gameObject.AddComponent<RdaPersistenceService>();
        }

        void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape) || sceneLoader == null || sceneLoader.IsLoading)
                return;

            string active = SceneManager.GetActiveScene().name;
            if (active != RdaSceneNames.Welcome && active != RdaSceneNames.MainMenu)
                OpenScene(RdaSceneNames.MainMenu, GameFlowState.MainMenu);
        }

        public void SetFlowState(GameFlowState next)
        {
            if (flowState == next)
                return;

            flowState = next;
            FlowStateChanged?.Invoke(flowState);
        }

        public void StartGuestSession()
        {
            session.BeginGuest();
            SessionChanged?.Invoke();
        }

        public void StartRegisteredSession(string profileId, string displayName)
        {
            session.BeginRegistered(profileId, displayName);
            SessionChanged?.Invoke();
        }

        public void EndSession()
        {
            session.Clear();
            SessionChanged?.Invoke();
        }

        public bool OpenScene(string sceneName, GameFlowState nextState)
        {
            if (!sceneLoader.TryLoad(sceneName))
                return false;

            SetFlowState(nextState);
            return true;
        }
    }
}
