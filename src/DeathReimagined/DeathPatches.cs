using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using Klei.AI;
using STRINGS;
using UnityEngine;
using SanchozzONIMods.Lib;
using System.Reflection;
using System.Reflection.Emit;

namespace DeathReimagined
{
    public static class DeathPatches
    {
        public const string MOURNING = "Mourning";
        public const string FUNERAL = "Funeral";
        public const string MELANCHOLY = "Melancholy";
        public const string MELANCHOLY_TRACKING = "Melancholy_tracking";
        public const string UNBURIED_CORPSE = "UnburiedCorpse";
        public const string OBSERVED_CORPSE = "ObservedCorpse";
        public const string OBSERVED_ROTTEN_CORPSE = "ObservedRottenCorpse";
        public const string DESTROYED_CORPSE = "DestroyedCorpse";
        public const string GRAVE_DESECRATION = "GraveDesecration";

        // добавляем новые эффекты
        [HarmonyPatch(typeof(ModifierSet), "LoadEffects")]
        internal static class ModifierSet_LoadEffects
        {
            private static void Postfix(ModifierSet __instance)
            {

                // добавим снижение аморали для траура
                Effect effectMourning = __instance.effects.Get(MOURNING);
                effectMourning.Add(new AttributeModifier("QualityOfLife", -20, MOURNING, false));

                //TODO: ДЛЯ ТЕСТИРОВАНИЯ. ПОТОМ УБРАТЬ !!!
                effectMourning.duration = 90;

                //TODO: отбалансить и настроить эффекты и сроки

                //TODO: небольшой компенсационный бафф за похороны
                Effect effectFuneral = new Effect(
                    id: FUNERAL,
                    name: STRINGS.DUPLICANTS.MODIFIERS.FUNERAL.NAME,
                    description: STRINGS.DUPLICANTS.MODIFIERS.FUNERAL.TOOLTIP,
                    duration: effectMourning.duration,
                    show_in_ui: true,
                    trigger_floating_text: true,
                    is_bad: false);
                effectFuneral.Add(new AttributeModifier("StressDelta", ModifierSet.ConvertValue(-10, Units.PerDay), effectFuneral.Name, false));
                effectFuneral.Add(new AttributeModifier("QualityOfLife", +10, effectFuneral.Name, false));
                __instance.effects.Add(effectFuneral);

                //TODO: экзистенциальная меланхолия
                Effect effectMelancholy = new Effect(
                    id: MELANCHOLY,
                    name: STRINGS.DUPLICANTS.MODIFIERS.MELANCHOLY.NAME,
                    description: STRINGS.DUPLICANTS.MODIFIERS.MELANCHOLY.TOOLTIP,
                    duration: 0,
                    show_in_ui: true,
                    trigger_floating_text: true,
                    is_bad: true);
                effectMelancholy.Add(new AttributeModifier("StressDelta", ModifierSet.ConvertValue(+20, Units.PerDay), effectMelancholy.Name, false));
                effectMelancholy.Add(new AttributeModifier("QualityOfLife", -5, effectMelancholy.Name, false));
                __instance.effects.Add(effectMelancholy);

                Effect effectMelancholy_tracking = new Effect(MELANCHOLY_TRACKING, "", "", effectMourning.duration, false, false, false);
                __instance.effects.Add(effectMelancholy_tracking);

                //TODO: дебаф за незакопанные трупы
                Effect effectUnburiedCorpse = new Effect(
                    id: UNBURIED_CORPSE,
                    name: STRINGS.DUPLICANTS.MODIFIERS.UNBURIED_CORPSE.NAME,
                    description: STRINGS.DUPLICANTS.MODIFIERS.UNBURIED_CORPSE.TOOLTIP,
                    duration: 0,
                    show_in_ui: true,
                    trigger_floating_text: true,
                    is_bad: true);
                effectUnburiedCorpse.Add(new AttributeModifier("StressDelta", ModifierSet.ConvertValue(+20, Units.PerDay), effectUnburiedCorpse.Name, false));
                effectUnburiedCorpse.Add(new AttributeModifier("QualityOfLife", -10, effectUnburiedCorpse.Name, false));
                __instance.effects.Add(effectUnburiedCorpse);

                //TODO: дебафы за вид свежего и подгнившего трупа
                Effect effectObservedCorpse = new Effect(
                    id: OBSERVED_CORPSE,
                    name: STRINGS.DUPLICANTS.MODIFIERS.OBSERVED_CORPSE.NAME,
                    description: STRINGS.DUPLICANTS.MODIFIERS.OBSERVED_CORPSE.TOOLTIP,
                    duration: 30,
                    show_in_ui: true,
                    trigger_floating_text: true,
                    is_bad: true);
                effectObservedCorpse.Add(new AttributeModifier("StressDelta", ModifierSet.ConvertValue(+10, Units.PerDay), effectObservedCorpse.Name, false));
                effectObservedCorpse.Add(new AttributeModifier("QualityOfLife", -5, effectObservedCorpse.Name, false));
                __instance.effects.Add(effectObservedCorpse);

                Effect effectObservedRottenCorpse = new Effect(
                    id: OBSERVED_ROTTEN_CORPSE,
                    name: STRINGS.DUPLICANTS.MODIFIERS.OBSERVED_ROTTEN_CORPSE.NAME,
                    description: STRINGS.DUPLICANTS.MODIFIERS.OBSERVED_ROTTEN_CORPSE.TOOLTIP,
                    duration: 30,
                    show_in_ui: true,
                    trigger_floating_text: true,
                    is_bad: true);
                effectObservedRottenCorpse.Add(new AttributeModifier("StressDelta", ModifierSet.ConvertValue(+30, Units.PerDay), effectObservedRottenCorpse.Name, false));
                effectObservedRottenCorpse.Add(new AttributeModifier("QualityOfLife", -15, effectObservedRottenCorpse.Name, false));
                __instance.effects.Add(effectObservedRottenCorpse);

                //TODO: дебаф при полном уничтожении трупа
                Effect effectDestroyedCorpse = new Effect(
                    id: DESTROYED_CORPSE,
                    name: STRINGS.DUPLICANTS.MODIFIERS.DESTROYED_CORPSE.NAME,
                    description: STRINGS.DUPLICANTS.MODIFIERS.DESTROYED_CORPSE.TOOLTIP,
                    duration: 60,
                    show_in_ui: true,
                    trigger_floating_text: true,
                    is_bad: true);
                effectDestroyedCorpse.Add(new AttributeModifier("StressDelta", ModifierSet.ConvertValue(+40, Units.PerDay), effectDestroyedCorpse.Name, false));
                effectDestroyedCorpse.Add(new AttributeModifier("QualityOfLife", -20, effectDestroyedCorpse.Name, false));
                __instance.effects.Add(effectDestroyedCorpse);

                //TODO: дебаф за осквернение могилы
                Effect effectGraveDesecration = new Effect(
                    id: GRAVE_DESECRATION,
                    name: STRINGS.DUPLICANTS.MODIFIERS.GRAVE_DESECRATION.NAME,
                    description: STRINGS.DUPLICANTS.MODIFIERS.GRAVE_DESECRATION.TOOLTIP,
                    duration: 60,
                    show_in_ui: true,
                    trigger_floating_text: true,
                    is_bad: true);
                effectGraveDesecration.Add(new AttributeModifier("StressDelta", ModifierSet.ConvertValue(+30, Units.PerDay), effectGraveDesecration.Name, false));
                effectGraveDesecration.Add(new AttributeModifier("QualityOfLife", -10, effectGraveDesecration.Name, false));
                __instance.effects.Add(effectGraveDesecration);
            }
        }

        // добавляем новые "мысли" и черепушку 
        [HarmonyPatch(typeof(Database.Thoughts), MethodType.Constructor, new Type[] { typeof(ResourceSet) })]
        internal static class Thoughts_Constructor
        {
            private static Sprite LoadSprite(string fileName, int width, int height)
            {
                Sprite result = null;
                try
                {
                    byte[] data = File.ReadAllBytes(Path.Combine(Utils.modInfo.spritesDirectory, fileName));
                    Texture2D texture2D = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    texture2D.filterMode = FilterMode.Trilinear;
                    texture2D.LoadImage(data);
                    result = Sprite.Create(texture2D, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 1f);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    result = null;
                }
                return result;
            }

            private static void Postfix(Database.Thoughts __instance)
            {
                Sprite spriteSkull = LoadSprite("skull.png", 80, 80);
                if (spriteSkull != null)
                {
                    MelancholyMonitor.MelancholyThought = new Thought(MELANCHOLY, __instance, spriteSkull, null, "crew_state_unhappy", "bubble_alert", SpeechMonitor.PREFIX_SAD, DUPLICANTS.THOUGHTS.UNHAPPY.TOOLTIP)
                    {
                        priority = __instance.Happy.priority + 1
                    };
                }
                else
                {
                    MelancholyMonitor.MelancholyThought = __instance.Unhappy;
                }
            }
        }

        // при смерти продлять эффект траура
        [HarmonyPatch(typeof(MinionModifiers), "OnDeath")]
        internal static class MinionModifiers_OnDeath
        {
            private static bool Prefix(object data)
            {
                Debug.LogFormat("OnDeath {0}", new object[] { data });
                EffectsExtensions.AddEffectToAllLiveMinions(MOURNING, true, true);
                return false;
            }
        }

        // добавляем дупликам визуализатор болезни, гниениe
        [HarmonyPatch(typeof(MinionConfig), "CreatePrefab")]
        internal static class MinionConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                __result.AddOrGet<DiseaseSourceVisualizer>();
                __result.AddOrGet<Decomposition>();
            }
        }

        /* 
         * DeathMonitor
         */

        // убираем c трупа эффект траура
        // если труп сохранён в стораге, убираем ссылки на него из общих списков
        // и наоборот - восстанавливаем все как было
        [HarmonyPatch(typeof(DeathMonitor), "InitializeStates")]
        internal static class DeathMonitor_InitializeStates
        {
            private static void Postfix(DeathMonitor __instance)
            {
                __instance.dead.RemoveEffect(MOURNING);
                __instance.dead.carried
                    .Enter(HideCorpse)
                    .Exit(UnHideCorpse);
            }
        }

        // TODO: нужно разобраться с отображением имён закопанных трупов

        // труп сохранён в стораге - удаляем ссылки на труп из глобальных списков
        // и ещё раз отменяем все назначения
        private static void HideCorpse(DeathMonitor.Instance smi)
        {
            MinionIdentity mi = smi.GetComponent<MinionIdentity>();
            if (mi != null)
            {
                Components.MinionIdentities.Remove(mi);
                MinionAssignablesProxy map = mi.assignableProxy.Get();
                if (map != null)
                {
                    Components.MinionAssignablesProxy.Remove(map);
                }
                mi.GetSoleOwner().UnassignAll();
                mi.GetEquipment().UnequipAll();
            }


            Debug.Log("HideCorpse");
            //NameDisplayScreen.Instance.entries.Find(entry => entry.world_go == smi.gameObject)?.display_go.SetActive(false);

            //Debug.Log(NameDisplayScreen.Instance.entries.Find(entry => entry.world_go == smi.gameObject)?.display_go.name);
            //NameDisplayScreen.Instance.entries.Find(entry => entry.world_go == smi.gameObject).display_go.GetComponent<KBatchedAnimController>().sceneLayer = Grid.SceneLayer.NoLayer;
        }

        // труп изъят из стораге - вернуть все как было
        // а также корректируем позицию чтобы не зависал в воздухе
        private static void UnHideCorpse(DeathMonitor.Instance smi)
        {
            MinionIdentity mi = smi.GetComponent<MinionIdentity>();
            if (mi != null)
            {
                if (!Components.MinionIdentities.Items.Contains(mi))
                {
                    Components.MinionIdentities.Add(mi);
                }
                MinionAssignablesProxy map = mi.assignableProxy.Get();
                if (map != null && !Components.MinionAssignablesProxy.Items.Contains(map))
                {
                    Components.MinionAssignablesProxy.Add(map);
                }
            }
            Transform transform = smi.master.transform;
            Vector3 position = Grid.CellToPos(Grid.PosToCell(transform.GetPosition()));
            position.z = transform.GetPosition().z;
            transform.SetPosition(position);

            Debug.Log("UnHideCorpse");

            //NameDisplayScreen.Instance.entries.Find(entry => entry.world_go == smi.gameObject)?.display_go.SetActive(true);
            //NameDisplayScreen.Instance.entries.Find(entry => entry.world_go == smi.gameObject).display_go.GetComponent<KBatchedAnimController>().sceneLayer = Grid.SceneLayer.Move;
        }

        /*
         * Grave
         */

        // если непустая могила разобрана, добавляем всем дебаф за осквернение могилы
        private static readonly EventSystem.IntraObjectHandler<Grave> OnDeconstructCompleteDelegate = new EventSystem.IntraObjectHandler<Grave>(delegate (Grave grave, object data)
        {
            if (grave.burialTime >= 0)
            {
                EffectsExtensions.AddEffectToAllLiveMinions(GRAVE_DESECRATION, true, true);
            }
        });

        [HarmonyPatch(typeof(Grave), "OnSpawn")]
        internal static class Grave_OnSpawn
        {
            private static void Postfix(Grave __instance)
            {
                __instance.Subscribe((int)GameHashes.DeconstructComplete, OnDeconstructCompleteDelegate);
            }
        }

        [HarmonyPatch(typeof(Grave), "OnCleanUp")]
        internal static class Grave_OnCleanUp
        {
            private static void Prefix(Grave __instance)
            {
                __instance.Unsubscribe((int)GameHashes.DeconstructComplete, OnDeconstructCompleteDelegate);
            }
        }

        // отключаем уничтожение трупа при захоронении
        [HarmonyPatch(typeof(Grave), "OnStorageChanged")]
        internal static class Grave_OnStorageChanged
        {
            private static bool Prefix(Grave __instance, object data)
            {
                GameObject gameObject = (GameObject)data;
                if (gameObject != null && gameObject.HasTag(GameTags.Corpse))
                {
                    __instance.graveName = gameObject.name;
                }
                return false;
            }
        }

        // добавляем в могилу необходимые компоненты
        /*
        [HarmonyPatch(typeof(GraveConfig), "ConfigureBuildingTemplate")]
        static class GraveConfig_ConfigureBuildingTemplate
        {
            static void Postfix(GameObject go)
            {
                go.AddOrGet<MinionStorage>();
            }
        }
        */

        /*
         * MournChore
         */

        // другой алгоритм поиска могилы - ищем с наибольшим временем захоронения, но пропуская часть, на основе приблизительной оценки общего количества еще не оплаканных смертей
        // todo: метод неточный, поэтому нужно предотвратить начало оплакивания если есть непохороненые трупы. блиин, пока труп несут - он не учитывается. арргх!!!
        // для лечения меланхолии - ищем случайную непустую могилу
        public static Grave FindGraveToMournAt(this MournChore.StatesInstance smi)
        {
            Grave result = null;
            ListPool<Grave, MournChore>.PooledList graves = ListPool<Grave, MournChore>.Allocate(Components.Graves.Items.FindAll(grave => grave.burialTime > 0));
            if (graves.Count > 0)
            {
                EffectInstance effectInstance = smi.sm.mourner.Get(smi)?.GetComponent<Effects>()?.Get(MOURNING);
                if (effectInstance != null)
                {
                    int numdeath = Mathf.FloorToInt(effectInstance.timeRemaining / effectInstance.effect.duration);
                    numdeath = Mathf.Clamp(numdeath, 0, graves.Count - 1);
                    result = graves.OrderByDescending(grave => grave.burialTime).ElementAt(numdeath);
                }
                else
                {
                    int index = UnityEngine.Random.Range(0, graves.Count);
                    result = graves[index];
                }
            }
            graves.Recycle();
            return result;
        }

        [HarmonyPatch(typeof(MournChore.StatesInstance), "CreateLocator")]
        internal static class MournChore_StatesInstance_CreateLocator
        {
            /*
            public void CreateLocator()
            {
	    ---     int cell = Grid.PosToCell(FindGraveToMournAt().transform.GetPosition());
        +++     int cell = Grid.PosToCell(DeathPatches.FindGraveToMournAt(this).transform.GetPosition());
                blablabla
            }
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionsList = instructions.ToList();
                bool flag = true;
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction instruction = instructionsList[i];
                    if (flag && instruction.opcode == OpCodes.Call && (MethodInfo)instruction.operand == typeof(MournChore).GetMethod("FindGraveToMournAt", new System.Type[] { }))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, typeof(DeathPatches).GetMethod("FindGraveToMournAt"));
                        flag = false;
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
            }
        }

        // после похорон траур не проходит. вместо этого добавляем небольшой компенсационный бафф за похороны
        // также лечит меланхолию
        public static void MournChore_OnCompleted(this MournChore.States states, MournChore.StatesInstance smi)
        {
            Effects effects = states.mourner.Get<Effects>(smi);
            Effect effectMourning = Db.Get().effects.Get(MOURNING);
            EffectInstance effectInstanceMourning = effects.Get(effectMourning);
            if (effectInstanceMourning != null)
            {
                EffectInstance effectInstanceFunereal = effects.Add(FUNERAL, true);
                if (effectInstanceMourning.timeRemaining < effectMourning.duration)
                {
                    effectInstanceFunereal.timeRemaining = effectInstanceMourning.timeRemaining;
                }
            }
            effects.Remove(MELANCHOLY);
        }

        // придется патчить и подменять делегат
        [HarmonyPatch(typeof(MournChore.States), "InitializeStates")]
        internal static class MournChore_States_InitializeStates
        {
            /*
            completed
                .PlayAnim("working_pst")
                .OnAnimQueueComplete(null)
                .Exit(
        ---         delegate(StatesInstance smi) { mourner.Get<Effects>(smi).Remove(Db.Get().effects.Get("Mourning")); }
        +++         DeathPatches.MournChore_OnCompleted
                    );
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionsList = instructions.ToList();
                bool flag = false;
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction instruction = instructionsList[i];
                    if (!flag && instruction.opcode == OpCodes.Ldfld && (FieldInfo)instruction.operand == typeof(MournChore.States).GetField("completed"))
                    {
                        flag = true;
                    }
                    if (flag && instruction.opcode == OpCodes.Ldftn)
                    {
                        yield return new CodeInstruction(OpCodes.Ldftn, typeof(DeathPatches).GetMethod("MournChore_OnCompleted"));
                        flag = false;
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
            }
        }

        /*
         * MournMonitor
         */

        // нужно исправить условия начала похоронной службы - есть эффект траура и нет эффекта за похороны
        // или есть меланхолия и прошел кулдаун меланхолии
        private static bool ShouldMourn(MournMonitor.Instance smi)
        {
            Effects effects = smi.master.GetComponent<Effects>();
            return (effects.HasEffect(MOURNING) && !effects.HasEffect(FUNERAL))
                || (effects.HasEffect(MELANCHOLY) && !effects.HasEffect(MELANCHOLY_TRACKING));
        }

        [HarmonyPatch(typeof(MournMonitor), "ShouldMourn")]
        internal static class MournMonitor_ShouldMourn
        {
            private static bool Prefix(MournMonitor.Instance smi, ref bool __result)
            {
                __result = ShouldMourn(smi);
                return false;
            }
        }

        [HarmonyPatch(typeof(MournMonitor), "InitializeStates")]
        internal static class MournMonitor_InitializeStates
        {
            private static void Postfix(MournMonitor __instance, MournMonitor.State ___idle, MournMonitor.State ___needsToMourn)
            {
                ___idle.EventTransition(GameHashes.EffectRemoved, ___needsToMourn, ShouldMourn);
            }
        }
    }
}
