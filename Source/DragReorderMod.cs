using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DragReorder
{
    public class DesignatorOrder : IExposable
    {
        public string category;
        public List<string> keys = new List<string>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref category, "category");
            Scribe_Collections.Look(ref keys, "keys", LookMode.Value);
            if (keys == null)
                keys = new List<string>();
        }
    }

    public class DragReorderSettings : ModSettings
    {
        public List<string> mainButtons = new List<string>();
        public List<string> categories = new List<string>();
        public List<DesignatorOrder> designators = new List<DesignatorOrder>();

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref mainButtons, "mainButtons", LookMode.Value);
            Scribe_Collections.Look(ref categories, "categories", LookMode.Value);
            Scribe_Collections.Look(ref designators, "designators", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                mainButtons ??= new List<string>();
                categories ??= new List<string>();
                designators ??= new List<DesignatorOrder>();
                designators.RemoveAll(d => d == null || d.category.NullOrEmpty());
            }
        }

        public List<string> DesignatorKeys(string category)
        {
            foreach (DesignatorOrder order in designators)
            {
                if (order.category == category)
                    return order.keys;
            }
            return null;
        }

        public void SetDesignatorKeys(string category, List<string> keys)
        {
            designators.RemoveAll(d => d.category == category);
            designators.Add(new DesignatorOrder { category = category, keys = keys });
        }
    }

    public class DragReorderMod : Mod
    {
        public static DragReorderMod Instance;
        public static DragReorderSettings Settings;

        public DragReorderMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<DragReorderSettings>();
            new Harmony("ifchen0.dragreorder").PatchAll();
        }

        public static void Save() => Instance.WriteSettings();

        public override string SettingsCategory() => "Drag Reorder";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.Label("DragReorder_Hint".Translate());
            listing.Gap();
            if (listing.ButtonText("DragReorder_ResetMainButtons".Translate()))
            {
                Settings.mainButtons.Clear();
                Save();
                MainButtonOrdering.ApplyToCurrent();
            }
            if (listing.ButtonText("DragReorder_ResetCategories".Translate()))
            {
                Settings.categories.Clear();
                Save();
                CategoryOrdering.ApplyToCurrent();
            }
            if (listing.ButtonText("DragReorder_ResetDesignators".Translate()))
            {
                Settings.designators.Clear();
                Save();
                DesignatorOrdering.Invalidate();
            }
            listing.End();
        }
    }
}
