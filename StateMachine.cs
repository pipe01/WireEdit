using PiTung.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WireEdit
{
    public class StateMachine<TState, TTrigger>
    {
        public TState CurrentState { get; private set; }

        private IList<Permit> Permits = new List<Permit>();
        private IDictionary<TState, Action> OnExit = new Dictionary<TState, Action>();
        private IDictionary<TState, Action> OnEnter = new Dictionary<TState, Action>();

        public StateMachine(TState initialState)
        {
            this.CurrentState = initialState;

            if (OnEnter.TryGetValue(initialState, out var onEnter))
                onEnter();
        }

        public Configurer Configure(TState state) => new Configurer(state, this);

        public void Fire(TTrigger trigger)
        {
            foreach (var permit in Permits)
            {
                if (permit.Original.Equals(CurrentState) && permit.Trigger.Equals(trigger))
                {
                    bool? cont = permit.Execute?.Invoke();

                    if (cont == false)
                        break;

                    if (OnExit.TryGetValue(CurrentState, out var onExit))
                        onExit();
                    
                    this.CurrentState = permit.NextState;

                    if (OnEnter.TryGetValue(CurrentState, out var onEnter))
                        onEnter();

                    break;
                }
            }
        }

        private struct Permit
        {
            public TState Original;
            public TTrigger Trigger;
            public TState NextState;
            public Func<bool> Execute;

            public Permit(TState original, TTrigger trigger, TState nextState, Func<bool> execute)
            {
                this.Original = original;
                this.Trigger = trigger;
                this.NextState = nextState;
                this.Execute = execute;
            }
        }

        public class Configurer
        {
            private readonly TState State;
            private readonly StateMachine<TState, TTrigger> Machine;

            public Configurer(TState state, StateMachine<TState, TTrigger> machine)
            {
                this.State = state;
                this.Machine = machine;
            }

            public Configurer OnExit(Action action)
            {
                Machine.OnExit[State] = action;

                return this;
            }

            public Configurer OnEnter(Action action)
            {
                Machine.OnEnter[State] = action;

                return this;
            }

            public Configurer Permit(TTrigger trigger, TState changeTo, Action execute)
                => Permit(trigger, changeTo, () => { execute(); return true; });

            public Configurer Permit(TTrigger trigger, TState changeTo)
                => Permit(trigger, changeTo, () => true);

            public Configurer Permit(TTrigger trigger, TState changeTo, Func<bool> condition)
            {
                Machine.Permits.Add(new Permit(State, trigger, changeTo, condition));

                return this;
            }
        }
    }
}
