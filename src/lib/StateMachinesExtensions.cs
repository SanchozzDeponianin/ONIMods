using System;
using System.IO;
#if USESPLIB
using PeterHan.PLib.Detours;
#else
using System.Reflection;
#endif

namespace SanchozzONIMods.Lib
{
    internal static class StateMachinesExtensions
    {
        // добавляем новый стат в статэмашыну
        // если parent_state == null то будет использоваться sm.root
        // если new_state == null то будет создан простой новый стат без вложенных статов

        public static GameStateMachine<SM, I>.State CreateState<SM, I>
            (this GameStateMachine<SM, I> sm, string name,
                GameStateMachine<SM, I>.State parent_state = null,
                GameStateMachine<SM, I>.State new_state = null)
            where SM : GameStateMachine<SM, I, IStateMachineTarget, object>
            where I : GameStateMachine<SM, I, IStateMachineTarget, object>.GameInstance
        {
            return CreateState<SM, I, IStateMachineTarget, object>(sm, name, parent_state, new_state);
        }

        public static GameStateMachine<SM, I, M>.State CreateState<SM, I, M>
            (this GameStateMachine<SM, I, M> sm, string name,
                GameStateMachine<SM, I, M>.State parent_state = null,
                GameStateMachine<SM, I, M>.State new_state = null)
            where SM : GameStateMachine<SM, I, M, object>
            where I : GameStateMachine<SM, I, M, object>.GameInstance
            where M : IStateMachineTarget
        {
            return CreateState<SM, I, M, object>(sm, name, parent_state, new_state);
        }

        public static GameStateMachine<SM, I, M, D>.State CreateState<SM, I, M, D>
            (this GameStateMachine<SM, I, M, D> sm, string name,
                GameStateMachine<SM, I, M, D>.State parent_state = null,
                GameStateMachine<SM, I, M, D>.State new_state = null)
            where SM : GameStateMachine<SM, I, M, D>
            where I : GameStateMachine<SM, I, M, D>.GameInstance
            where M : IStateMachineTarget
            //where D : GameStateMachine<SM, I, M, D>.BaseDef or just object
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (sm == null)
                throw new ArgumentNullException(nameof(sm));
            if (parent_state == null)
                parent_state = sm.root;
            if (new_state == parent_state)
                throw new ArgumentException($"[{Utils.MyModName}] The new State should not be a parent for itself.", nameof(parent_state));
            if (new_state == null)
                new_state = new GameStateMachine<SM, I, M, D>.State();
            sm.CreateStates(new_state);
            sm.BindState(parent_state, new_state, name);
            sm.BindStates(new_state, new_state);
            return new_state;
        }

        // добавляем новый параметыр в статэмашыну
        public static P AddParameter<SM, I, P>
            (this GameStateMachine<SM, I> sm, string name, P parameter)
            where SM : GameStateMachine<SM, I>
            where I : GameStateMachine<SM, I>.GameInstance
            where P : StateMachine.Parameter
        {
            return AddParameter<SM, I, IStateMachineTarget, object, P>(sm, name, parameter);
        }

        public static P AddParameter<SM, I, M, P>
            (this GameStateMachine<SM, I, M> sm, string name, P parameter)
            where SM : GameStateMachine<SM, I, M>
            where I : GameStateMachine<SM, I, M>.GameInstance
            where M : IStateMachineTarget
            where P : StateMachine.Parameter
        {
            return AddParameter<SM, I, M, object, P>(sm, name, parameter);
        }

        public static P AddParameter<SM, I, M, D, P>
            (this GameStateMachine<SM, I, M, D> sm, string name, P parameter)
            where SM : GameStateMachine<SM, I, M, D>
            where I : GameStateMachine<SM, I, M, D>.GameInstance
            where M : IStateMachineTarget
            //where D : GameStateMachine<SM, I, M, D>.BaseDef or just object
            where P : StateMachine.Parameter
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (sm == null)
                throw new ArgumentNullException(nameof(sm));
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));
            if (!Test(typeof(P).GetGenericArguments(), typeof(SM), typeof(I), typeof(M), typeof(D)))
            {
                Debug.LogWarningFormat("[{0}] Achtung!!! Attempt to add a Parameter to an unsuitable StateMachine\nname: {1}\nParameter: {2}\nStateMachine: {3}",
                   Utils.MyModName, name, typeof(P), typeof(SM));
            }
            AddParameter(sm, parameter, name);
            return parameter;
        }

#if USESPLIB
        private static IDetouredField<StateMachine, StateMachine.Parameter[]> Parameters
                    = PDetours.DetourFieldLazy<StateMachine, StateMachine.Parameter[]>("parameters");

        private static void AddParameter(StateMachine sm, StateMachine.Parameter parameter, string name)
        {
            var parameters = Parameters.Get(sm);
            parameter.name = name;
            parameter.idx = parameters.Length;
            parameters = parameters.Append(parameter);
            Parameters.Set(sm, parameters);
        }
#else
        private static FieldInfo Parameters;

        private static void AddParameter(StateMachine sm, StateMachine.Parameter parameter, string name)
        {
            if (Parameters == null)
                Parameters = typeof(StateMachine).GetField("parameters", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var parameters = (StateMachine.Parameter[])Parameters.GetValue(sm);
            parameter.name = name;
            parameter.idx = parameters.Length;
            parameters = parameters.Append(parameter);
            Parameters.SetValue(sm, parameters);
        }
#endif

        private static bool Test(Type[] p_types, params Type[] sm_types)
        {
            if (p_types != null && sm_types != null && p_types.Length >= sm_types.Length)
            {
                for (int i = 0; i < sm_types.Length; i++)
                {
                    if (p_types[i] != sm_types[i])
                        return false;
                }
                return true;
            }
            else return false;
        }

#if PUBLICISED
        // заказать анимацию при выходе из статуса
        public static GameStateMachine<SM, I, M, D>.State QueueAnimOnExit<SM, I, M, D>
            (this GameStateMachine<SM, I, M, D>.State @this,
                string anim,
                bool loop = false,
                Func<I, string> suffix_callback = null)
            where SM : GameStateMachine<SM, I, M, D>
            where I : GameStateMachine<SM, I, M, D>.GameInstance
            where M : IStateMachineTarget
            //where D : GameStateMachine<SM, I, M, D>.BaseDef or just object
        {
            if (@this == null)
                throw new ArgumentNullException(nameof(@this));
            var state_target = @this.GetStateTarget();
            var mode = loop ? KAnim.PlayMode.Loop : KAnim.PlayMode.Once;
            @this.Exit($"QueueAnimOnExit({anim}, {mode.ToString()})", (I smi) =>
            {
                string suffix = (suffix_callback != null) ? suffix_callback(smi) : string.Empty;
                var kbac = state_target.Get<KAnimControllerBase>(smi);
                if (kbac != null)
                    kbac.Queue(anim + suffix, mode);
            });
            return @this;
        }
#endif

        // ObjectParameter который не будет срать в логи при загрузке и сохранении
        public class NonSerializedObjectParameter<SM, I, M, D, ObjectType> : StateMachine<SM, I, M, D>.ObjectParameter<ObjectType>
            where SM : StateMachine<SM, I, M, D>
            where I : StateMachine<SM, I, M, D>.GenericInstance
            where M : IStateMachineTarget
            //where D : GameStateMachine<SM, I, M, D>.BaseDef or just object
            where ObjectType : class
        {
            public new class Context : StateMachine<SM, I, M, D>.ObjectParameter<ObjectType>.Context
            {
                public Context(StateMachine.Parameter parameter, ObjectType default_value) : base(parameter, default_value) { }
                public override void Deserialize(IReader reader, StateMachine.Instance smi) { }
                public override void Serialize(BinaryWriter writer) { }
            }

            public override StateMachine.Parameter.Context CreateContext()
            {
                return new Context(this, defaultValue);
            }
        }
    }
}
