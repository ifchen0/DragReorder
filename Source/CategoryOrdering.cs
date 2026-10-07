using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DragReorder
{
    /// <summary>Right-drag reordering of the architect category buttons.</summary>
    [HarmonyPatch]
    public static class CategoryOrdering
    {
        private const float ButHeight = 32f;

        private static readonly AccessTools.FieldRef<MainTabWindow_Architect, List<ArchitectCategoryTab>> PanelsRef =
            AccessTools.FieldRefAccess<MainTabWindow_Architect, List<ArchitectCategoryTab>>("desPanelsCached");

        private static readonly DragController<ArchitectCategoryTab> Drag = new DragController<ArchitectCategoryTab>();
        private static readonly List<(ArchitectCategoryTab panel, Rect rect)> Rects = new List<(ArchitectCategoryTab, Rect)>();

        public static void Apply(MainTabWindow_Architect window)
        {
            List<ArchitectCategoryTab> panels = PanelsRef(window);
            List<ArchitectCategoryTab> byDefault = panels.OrderByDescending(p => p.def.order).ToList();
            List<string> merged = DragUtil.Merge(byDefault.Select(p => p.def.defName).ToList(), DragReorderMod.Settings.categories);
            panels.Clear();
            panels.AddRange(merged.Select(name => byDefault.First(p => p.def.defName == name)));
        }

        public static void ApplyToCurrent()
        {
            if (Current.ProgramState == ProgramState.Playing && MainButtonDefOf.Architect.TabWindow is MainTabWindow_Architect window)
                Apply(window);
        }

        [HarmonyPatch(typeof(MainTabWindow_Architect), MethodType.Constructor)]
        [HarmonyPostfix]
        private static void Constructor_Postfix(MainTabWindow_Architect __instance) => Apply(__instance);

        [HarmonyPatch(typeof(MainTabWindow_Architect), nameof(MainTabWindow_Architect.DoWindowContents))]
        [HarmonyPrefix]
        private static void DoWindowContents_Prefix(MainTabWindow_Architect __instance, Rect inRect)
        {
            if (Event.current.type == EventType.Layout)
                return;
            List<ArchitectCategoryTab> panels = PanelsRef(__instance);
            CacheRects(panels, inRect.width / 2f);
            if (Drag.HandleEvent(HitTest, out ArchitectCategoryTab source, out ArchitectCategoryTab target))
            {
                DragUtil.Move(panels, source, target);
                DragReorderMod.Settings.categories = panels.Select(p => p.def.defName).ToList();
                DragReorderMod.Save();
            }
        }

        [HarmonyPatch(typeof(MainTabWindow_Architect), nameof(MainTabWindow_Architect.DoWindowContents))]
        [HarmonyPostfix]
        private static void DoWindowContents_Postfix()
        {
            ArchitectCategoryTab dragging = Drag.Dragging;
            if (dragging == null)
                return;
            ArchitectCategoryTab target = HitTest(Event.current.mousePosition);
            DragUtil.DrawFeedback(RectOf(dragging), target != null && target != dragging ? RectOf(target) : null, null);
        }

        // Same layout as MainTabWindow_Architect.DoWindowContents, including the preferredColumn swap.
        private static void CacheRects(List<ArchitectCategoryTab> panels, float butWidth)
        {
            Rects.Clear();
            float row = 0f;
            for (int i = 0; i < panels.Count; i += 2)
            {
                ArchitectCategoryTab a = panels[i];
                ArchitectCategoryTab b = i + 1 < panels.Count ? panels[i + 1] : null;
                bool swap = (a.PreferredColumn == 1 || (b != null && b.PreferredColumn == 0))
                    && a.PreferredColumn != 0 && (b == null || b.PreferredColumn != 1);
                ArchitectCategoryTab left = swap ? b : a;
                ArchitectCategoryTab right = swap ? a : b;
                if (left != null)
                    Rects.Add((left, new Rect(0f, row * ButHeight, butWidth + 1f, ButHeight + 1f)));
                if (right != null)
                    Rects.Add((right, new Rect(butWidth, row * ButHeight, butWidth, ButHeight + 1f)));
                row += 1f;
            }
        }

        private static ArchitectCategoryTab HitTest(Vector2 pos)
        {
            foreach (var (panel, rect) in Rects)
            {
                if (rect.Contains(pos))
                    return panel;
            }
            return null;
        }

        private static Rect? RectOf(ArchitectCategoryTab panel)
        {
            foreach (var entry in Rects)
            {
                if (entry.panel == panel)
                    return entry.rect;
            }
            return null;
        }
    }
}
