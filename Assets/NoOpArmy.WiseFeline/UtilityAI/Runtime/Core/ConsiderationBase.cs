using System;
using UnityEngine;

namespace NoOpArmy.WiseFeline
{
    /// <summary>
    /// Considerations should inherit from this class.
    /// Each action has a list of these and multiplies the value of calling their GetValue methods to calculate its score.
    /// Each consideration is either a consideration for the
    /// target of the action or the action itself.
    /// The target based ones will be executed once per target when the action score is being calculated for that target.
    /// </summary>
    public abstract class ConsiderationBase : AIObject
    {
        /// <summary>
        /// If true, a new instance of the consideration will be created per action per instance of the brain component which needs this.
        /// Otherwise a single instance will be used for all of them.
        /// If you don't use fields in the class and the GetValue method is stateless, then turn this off to allocate less memory.
        /// </summary>
        [Header("Optimization")]
        [SerializeField]
        [Tooltip("If you don't need a new instance of this class per brain turn this off, this will share a unique class with all agents.")]
        protected bool instantiatePerAgent = true;

        /// <summary>
        /// Does this consideration work on targets of the agent with the brain component or on the GameObject itself.
        /// </summary>
        [Header("Consideration")]
        [SerializeField]
        internal bool NeedTarget;

        /// <summary>
        /// Minimum value that the consideration can return from GetValue
        /// </summary>
        [SerializeField]
        protected float minRange = 0;

        /// <summary>
        /// Maximum value that the consideration can return from GetValue
        /// </summary>
        [SerializeField]
        protected float maxRange = 1;

        [SerializeField]
        private AnimationCurve ResponseCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));



        /// <summary>
        /// The score of this consideration returned from the last GetValue call.
        /// </summary>
        public float Score { get { return score; } }

#if UNITY_EDITOR
        public void SetScoreToZero()
        {
            score = 0;
        }

        public void SetScore(float s)
        {
            score = s;
        }

        /// <summary>
        /// The normalized input (0 to 1, after the min and max range are applied) that the response curve
        /// was last evaluated with. Editor only; the Wise Feline window uses it to mark the current point
        /// on the curve while debugging.
        /// </summary>
        public float LastNormalizedInput { get; private set; }

        /// <summary>
        /// The raw value returned by GetValue on the last evaluation, before the range is applied. Editor only.
        /// </summary>
        public float LastRawValue { get; private set; }

        /// <summary>
        /// The minimum of the accepted input range. Editor only; exposed for the debugger.
        /// </summary>
        public float MinRange { get { return minRange; } }

        /// <summary>
        /// The maximum of the accepted input range. Editor only; exposed for the debugger.
        /// </summary>
        public float MaxRange { get { return maxRange; } }

        /// <summary>
        /// Does this consideration score the action's targets rather than the agent itself. Editor only; exposed for the debugger.
        /// </summary>
        public bool IsTargeted { get { return NeedTarget; } }

        /// <summary>
        /// The response curve. Editor only; exposed for the debugger's curve preview.
        /// </summary>
        public AnimationCurve Curve { get { return ResponseCurve; } }
#endif

        /// <summary>
        /// The Brain component that this consideration is being attached to one of its actions
        /// </summary>
        protected Brain Brain { get; private set; }

        /// <summary>
        /// Is the consideration initialized or not
        /// </summary>
        internal bool IsInitialized { get; private set; }

        private float score;

        /// <summary>
        /// Returns a cloned copy of the consideration or the main instance if instancing is turned off.
        /// </summary>
        /// <returns></returns>
        public ConsiderationBase Clone()
        {
            if (instantiatePerAgent || !Application.isPlaying)
                return Instantiate(this);
            return this;
        }

        /// <summary>
        /// Initializes the consideration
        /// </summary>
        /// <param name="brain"></param>
        internal void Initialize(Brain brain)
        {
            if (minRange >= maxRange)
                throw new InvalidOperationException($"minRange of a consideration should always be less than its maxRange {this.GetType().Name}");
            Brain = brain;
            IsInitialized = true;
            OnInitialized();
        }

        /// <summary>
        /// Fired when the consideration is initialized
        /// </summary>
        protected virtual void OnInitialized()
        {

        }

        /// <summary>
        /// Updates the range of acceptable values from GetValue
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public void UpdateRange(float min, float max)
        {
            if (min >= max)
                throw new ArgumentException($"minRange should be always less than maxRange {this.GetType().Name}");
                minRange = min;
            maxRange = max;
            
        }

        internal float GetScore(Component target)
        {
            if (!IsInitialized)
            {
                throw new Exception($"Considerations should be initialized before being used. Initialize {this.Name} before using it");
            }

            float value = GetValue(target);

            //normalizes the value based on the range
            float length = maxRange - minRange;
            value -= minRange;

            value = Mathf.Clamp(value, 0, length);
            float normalizedValue = value / Mathf.Abs(length);
            score = ResponseCurve.Evaluate(normalizedValue);
#if UNITY_EDITOR
            LastRawValue = value + minRange;
            LastNormalizedInput = normalizedValue;
#endif
            return score;
        }


        /// <summary>
        /// This method should return the current value of the consideration which is used for multiplication with other considerations to calculate the score of the action
        /// </summary>
        /// <param name="target">If the consideration has a target then this value is the target component which the consideration should be calculated for. The actual type of this depends on the type of the component that the action stores in its target list in the UpdateTargets call. If the consideration doesn't have a target, then the Brain component of the executing agent itself is passed to the method.</param>
        /// <returns>A value between minRange and maxRange which indicates the current value/score of the consideration</returns>
        protected abstract float GetValue(Component target);
    }
}