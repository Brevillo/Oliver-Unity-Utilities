/* Made by Oliver Beebe 2023 */
using System.Collections.Generic;
using System;

namespace OliverBeebe.UnityUtilities.Runtime {

    [Serializable]
    public class StateMachine<TContext> {

        #if UNITY_EDITOR
        [UnityEngine.SerializeField, UnityEngine.TextArea(3, 3)] private string debug;
        #endif

        public event Action<State<TContext>, State<TContext>> OnTransition;

        private readonly State<TContext> firstState;
        private readonly TransitionDictionary transitions;

        public State<TContext> currentState     { get; private set; }
        public State<TContext> previousState    { get; private set; }
        public float stateDuration              { get; private set; }

        public StateMachine(State<TContext> firstState, TransitionDictionary transitions) {

            this.firstState = firstState;
            this.transitions = transitions;

            Reset();
        }

        public void Reset() {

            if (currentState != null) {
                currentState.Exit();
                currentState.ManualExit();
            }

            currentState = previousState = firstState;
            stateDuration = float.MaxValue;

            currentState.Enter();
        }

        public void ChangeState(State<TContext> toState) {

            if (currentState != null) {
                currentState.Exit();
                currentState.ManualExit();
            }

            previousState = currentState;
            currentState = toState;
            stateDuration = 0;

            currentState.ManualEnter();
            currentState.Enter();
 
            OnTransition?.Invoke(previousState, currentState);
        }

        private void ChangeState(State<TContext> toState, Action behavior) {

            currentState?.Exit();

            behavior?.Invoke();

            previousState = currentState;
            currentState = toState;
            stateDuration = 0;

            currentState.Enter();

            OnTransition?.Invoke(previousState, currentState);
        }

        public void Update(float dt) {

            stateDuration += dt;
            currentState.Update();

            if (transitions.TryGetValue(currentState, out var stateTransitions)) {
                var transition = stateTransitions.Find(Transition.CanTransition);
                if (transition) ChangeState(transition.toState, transition.behavior);
            }

            #if UNITY_EDITOR
            debug = $"Current State : {currentState.GetType().Name}\nPrevious State : {previousState.GetType().Name}\nDuration : {stateDuration}";
            #endif
        }

        public class TransitionDictionary : Dictionary<State<TContext>, List<Transition>> { }

        public readonly struct Transition {

            public static Predicate<Transition> CanTransition = transition => transition.canTransition.Invoke();

            public static implicit operator bool(Transition t) => t.exists;

            private readonly bool exists;
            private readonly TransitionDelegate canTransition;
            public  readonly State<TContext> toState;
            public  readonly Action behavior;

            public Transition(State<TContext> toState, TransitionDelegate canTransition)
                => (exists, this.toState, this.canTransition, this.behavior)
                =  (true, toState, canTransition, null);

            public Transition(State<TContext> toState, TransitionDelegate canTransition, Action behavior)
                => (exists, this.toState, this.canTransition, this.behavior)
                =  (true, toState, canTransition, behavior);
        }
    }

    public delegate bool TransitionDelegate();

    public abstract class State<TContext> {

        public State(TContext context) => this.context = context;

        protected readonly TContext context;

        public virtual void Enter () { }
        public virtual void Update() { }
        public virtual void Exit  () { }
        public virtual void ManualEnter() { }
        public virtual void ManualExit() { }
    }

    public abstract class SubState<TContext, TSuperState> : State<TContext> where TSuperState : State<TContext> {

        public SubState(TContext context, TSuperState superState) : base(context) => this.superState = superState;

        protected readonly TSuperState superState;

        public override void Enter()  {
            base.Enter();
            superState.Enter();
        }
        public override void Update() {
            superState.Update();
            base.Update();
        }
        public override void Exit()   {
            superState.Exit();
            base.Exit();
        }
        public override void ManualEnter() {
            base.ManualEnter();
            superState.ManualEnter();
        }
        public override void ManualExit() {
            superState.ManualExit();
            base.ManualExit();
        }
    }
}
