using UnityEngine;
using RealDrivingAcademy.Core;

namespace RealDrivingAcademy.UI
{
    public sealed class RdaMenuActions : MonoBehaviour
    {
        public GameFlowState flowStateOnStart = GameFlowState.Boot;

        void Start()
        {
            if (RdaGameManager.Instance != null)
                RdaGameManager.Instance.SetFlowState(flowStateOnStart);
        }

        public void ContinueAsGuest()
        {
            var game = RdaGameManager.Instance;
            if (game == null) return;

            game.StartGuestSession();
            game.OpenScene(RdaSceneNames.MainMenu, GameFlowState.MainMenu);
        }

        public void ContinueWithLocalProfile()
        {
            var game = RdaGameManager.Instance;
            if (game == null) return;

            game.StartRegisteredSession("local_profile", "Learner");
            game.OpenScene(RdaSceneNames.MainMenu, GameFlowState.MainMenu);
        }

        public void OpenTrainingMenu() => Open(RdaSceneNames.TrainingMenu, GameFlowState.Training);
        public void StartTrainingDrive() => Open(RdaSceneNames.TrainingGround, GameFlowState.Training);
        public void OpenTestMenu() => Open(RdaSceneNames.TestMenu, GameFlowState.DrivingTest);
        public void OpenGarage() => Open(RdaSceneNames.Garage, GameFlowState.Garage);
        public void OpenProgress() => Open(RdaSceneNames.Progress, GameFlowState.Progress);
        public void OpenSettings() => Open(RdaSceneNames.Settings, GameFlowState.Settings);
        public void BackToMainMenu() => Open(RdaSceneNames.MainMenu, GameFlowState.MainMenu);

        public void EndSession()
        {
            var game = RdaGameManager.Instance;
            if (game == null) return;

            game.EndSession();
            game.OpenScene(RdaSceneNames.Welcome, GameFlowState.Boot);
        }

        public void QuitApplication()
        {
            if (RdaGameManager.Instance != null && RdaGameManager.Instance.Persistence != null)
                RdaGameManager.Instance.Persistence.SaveNow();

            Application.Quit();
        }

        void Open(string sceneName, GameFlowState state)
        {
            var game = RdaGameManager.Instance;
            if (game != null)
                game.OpenScene(sceneName, state);
        }
    }
}
