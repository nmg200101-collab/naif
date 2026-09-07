using System;
using UnityEngine;

namespace RDA.V50.Academy
{
    [Serializable]
    public sealed class RDAEvaluationState
    {
        public int score = 100;
        public int faults;
        public bool failed;
    }

    public sealed class RDADrivingEvaluator : MonoBehaviour
    {
        [SerializeField] private int failScore = 50;
        public RDAEvaluationState State { get; } = new RDAEvaluationState();

        public void RegisterFault(int penalty)
        {
            penalty = Mathf.Max(0, penalty);
            State.faults++;
            State.score = Mathf.Max(0, State.score - penalty);
            State.failed = State.score < failScore;
        }

        public void ResetEvaluation()
        {
            State.score = 100;
            State.faults = 0;
            State.failed = false;
        }
    }
}