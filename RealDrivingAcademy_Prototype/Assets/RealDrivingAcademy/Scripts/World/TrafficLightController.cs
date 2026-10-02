using System.Collections;
using UnityEngine;

namespace RealDrivingAcademy.World
{
    public class TrafficLightController : MonoBehaviour
    {
        public enum State { Red, RedYellow, Green, Yellow }
        public State state = State.Red;
        public float redSeconds = 8f;
        public float redYellowSeconds = 1f;
        public float greenSeconds = 10f;
        public float yellowSeconds = 2f;
        public Renderer redLamp;
        public Renderer yellowLamp;
        public Renderer greenLamp;
        public bool runAutomatically = true;

        void Start()
        {
            ApplyVisuals();
            if (runAutomatically) StartCoroutine(Cycle());
        }

        IEnumerator Cycle()
        {
            while (true)
            {
                SetState(State.Red); yield return new WaitForSeconds(redSeconds);
                SetState(State.RedYellow); yield return new WaitForSeconds(redYellowSeconds);
                SetState(State.Green); yield return new WaitForSeconds(greenSeconds);
                SetState(State.Yellow); yield return new WaitForSeconds(yellowSeconds);
            }
        }

        public void SetState(State newState)
        {
            state = newState;
            ApplyVisuals();
        }

        void ApplyVisuals()
        {
            if (redLamp != null) redLamp.enabled = state == State.Red || state == State.RedYellow;
            if (yellowLamp != null) yellowLamp.enabled = state == State.Yellow || state == State.RedYellow;
            if (greenLamp != null) greenLamp.enabled = state == State.Green;
        }
    }
}
