using System;
using UnityEngine;

namespace RealDrivingAcademy.Core
{
    [DefaultExecutionOrder(-1000)]
    public sealed class RdaGameManager : MonoBehaviour
    {
        public static RdaGameManager Instance { get; private set; }

        [SerializeField] GameFlowState flowState = GameFlowState.Boot;
        [SerializeField] GameSessionState session = new GameSessionState();

        RdaSceneLoader sceneLoader;

        public GameFlowState FlowState => flowState;
        public GameSessionState Session => session;
        public RdaSceneLoader SceneLoader => sceneLoader;

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
