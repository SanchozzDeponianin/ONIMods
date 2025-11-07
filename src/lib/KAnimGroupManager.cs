using System;
using System.Collections.Generic;
using HarmonyLib;
using PeterHan.PLib.Core;

namespace SanchozzONIMods.Lib
{
    public sealed class KAnimGroupManager : PForwardedComponent
    {
        // хеши названий некоторых аним групп. сами названия хз куда заныканы
        private const int SWAPS = -77805842;
        private const int INTERACTS = -1371425853;

        private static readonly Version VERSION = new(1, 1, 0, 0);
        internal static KAnimGroupManager Instance { get; private set; }

        private readonly Dictionary<HashedString, HashSet<string>> group_anims_table = new(1);
        private readonly Dictionary<HashedString, HashSet<string>> together_anims_table = new(1);

        public override Version Version => VERSION;

        public void RegisterInteractAnims(params string[] anims)
        {
            RegisterAnims(new HashedString(INTERACTS), anims);
        }

        public void RegisterSwapAnims(params string[] anims)
        {
            RegisterAnims(new HashedString(SWAPS), anims);
        }

        public void RegisterAnims(HashedString target_group, params string[] anims)
        {
            if (!target_group.IsValid)
                throw new ArgumentNullException(nameof(target_group));
            if (anims == null)
                throw new ArgumentNullException(nameof(anims));
            RegisterForForwarding();
            if (!group_anims_table.ContainsKey(target_group))
                group_anims_table[target_group] = new HashSet<string>();
            foreach (var anim_name in anims)
            {
                if (group_anims_table[target_group].Add(anim_name))
                {
#if DEBUG
                    PUtil.LogDebug("Registered anim '{0}' to add into group '{1}'."
                        .F(anim_name, target_group));
#endif
                }
                else
                {
                    PUtil.LogWarning("Anim '{0}' already registered to add into group '{1}', skip."
                        .F(anim_name, target_group));
                }
            }
        }

        public void RegisterAnimsTogether(HashedString ingame_anim, params string[] anims)
        {
            if (!ingame_anim.IsValid)
                throw new ArgumentNullException(nameof(ingame_anim));
            if (anims == null)
                throw new ArgumentNullException(nameof(anims));
            RegisterForForwarding();
            if (!together_anims_table.ContainsKey(ingame_anim))
                together_anims_table[ingame_anim] = new HashSet<string>();
            foreach (var anim_name in anims)
            {
                if (together_anims_table[ingame_anim].Add(anim_name))
                {
#if DEBUG
                    PUtil.LogDebug("Registered anim '{0}' to add into same group as anim '{1}({2})'"
                        .F(anim_name, HashCache.Get().Get(ingame_anim), ingame_anim));
#endif
                }
                else
                {
                    PUtil.LogWarning("Anim '{0}' already registered to add into same group as anim '{1}({2})', skip."
                        .F(anim_name, HashCache.Get().Get(ingame_anim), ingame_anim));
                }
            }
        }

        public override void Initialize(Harmony plibInstance)
        {
            Instance = this;
            plibInstance.Patch(typeof(KAnimGroupFile), nameof(KAnimGroupFile.LoadAll), prefix: PatchMethod(nameof(KAnimGroupFile_LoadAll_Prefix)));
        }

        private static void KAnimGroupFile_LoadAll_Prefix()
        {
            Instance?.InvokeAllProcess(0, null);
        }

        public override void Process(uint operation, object args)
        {
            if (operation == 0)
                ProcessAnims();
        }

        private void ProcessAnims()
        {
            var groups = KAnimGroupFile.GetGroupFile().GetData();
            foreach (var ingame_anim in together_anims_table.Keys)
            {
                var targetGroup = groups.Find(group => group.animNames.Contains(ingame_anim));
                if (targetGroup == null)
                {
                    PUtil.LogWarning("Group for '{0}({1})' anim not found."
                        .F(HashCache.Get().Get(ingame_anim), ingame_anim));
                }
                else
                {
#if DEBUG
                    PUtil.LogDebug("Group '{0}' found for '{1}({2})' anim."
                        .F(targetGroup.id, HashCache.Get().Get(ingame_anim), ingame_anim));
#endif
                    if (!group_anims_table.ContainsKey(targetGroup.id))
                        group_anims_table[targetGroup.id] = new HashSet<string>();
                    group_anims_table[targetGroup.id].UnionWith(together_anims_table[ingame_anim]);
                }
            }
            foreach (var group_id in group_anims_table.Keys)
            {
                var targetGroup = KAnimGroupFile.GetGroup(group_id);
                if (targetGroup == null)
                {
                    PUtil.LogWarning("Group '{0}' not found.".F(group_id));
                }
                else
                {
                    foreach (var anim_name in group_anims_table[group_id])
                    {
                        if (!Assets.TryGetAnim(anim_name, out var kanim))
                        {
                            PUtil.LogWarning("Missing Anim '{0}'.".F(anim_name));
                        }
                        else
                        {
                            if (kanim.mod == null)
                            {
                                PUtil.LogWarning("Anim '{0}' probably is not modded anim, skip.".F(anim_name));
                            }
                            else
                            {
                                // удаляем группу, созданную игрой автоматически во время загрузки моддовой анимации
                                groups.RemoveAll(g => (g.animNames.Count == 1 && g.animNames[0] == kanim.name));

                                if (!targetGroup.animNames.Contains(kanim.name))
                                {
                                    targetGroup.animNames.Add(kanim.name);
                                }
                                if (!targetGroup.animFiles.Contains(kanim))
                                {
                                    targetGroup.animFiles.Add(kanim);
#if DEBUG
                                    PUtil.LogDebug("Added anim '{0}' into group '{1}'."
                                        .F(anim_name, group_id));
#endif
                                }
                                else
                                {
                                    PUtil.LogDebug("Anim '{0}' is already in group '{1}'! Are two mods adding anims with the same name ?"
                                        .F(anim_name, group_id));
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}