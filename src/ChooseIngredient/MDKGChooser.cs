using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SanchozzONIMods.Lib;

namespace ChooseIngredient
{
    using static Patches;

    [SkipSaveFileSerialization]
    public class MDKGChooser : KMonoBehaviour
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private ManualDeliveryKG mdkg;

        [MyCmpReq]
        private TreeFilterable filterable;

        [MyCmpReq]
        private FlatTagFilterable flat;
#pragma warning restore CS0649

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            filterable.OnFilterChanged += OnFilterChanged;
        }

        protected override void OnCleanUp()
        {
            if (filterable != null)
                filterable.OnFilterChanged -= OnFilterChanged;
            base.OnCleanUp();
        }

        private void OnFilterChanged(HashSet<Tag> allowed_tags)
        {
            var forbidden_tags = ListPool<Tag, MDKGChooser>.Allocate();
            foreach (var tag in flat.tagOptions)
            {
                if (!allowed_tags.Contains(tag))
                    forbidden_tags.Add(tag);
            }
            mdkg.ForbiddenTags = forbidden_tags.ToArray();
            forbidden_tags.Recycle();
            SetPipedEverythingConsumer();
        }

        internal void SetPipedEverythingConsumer()
        {
            if (PipedEverythingConsumerS != null)
            {
                try
                {
                    foreach (var consumer in GetComponents(PipedEverythingConsumerS))
                    {
                        var traverse = Traverse.Create(consumer);
                        if (traverse.Property<Storage>("Storage").Value == mdkg.DebugStorage)
                        {
                            traverse.Field<Tag[]>("tagFilter").Value = filterable.AcceptedTags.ToArray();
                            traverse.Field<float>("capacityKG").Value = mdkg.capacity;
                        }
                    }
                }
                catch (Exception e)
                {
                    Utils.LogExcWarn(e);
                }
            }
        }
    }
}
