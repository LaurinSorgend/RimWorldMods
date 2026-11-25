using System;
using UnityEngine;
using Verse;

namespace AnimalFamilyTree
{
    /// <summary>Pannable, zoomable view that draws a laid-out family tree.</summary>
    public class TreeCanvas
    {
        private const float MinZoom = 0.3f;
        private const float MaxZoom = 2f;
        private const float ScrollPanSpeed = 24f;

        public static readonly Color LineColor = new Color(0.75f, 0.75f, 0.75f, 0.8f);
        public static readonly Color MateLineColor = new Color(0.9f, 0.55f, 0.4f, 0.9f);

        private Vector2 offset;
        private float zoom = 1f;
        private bool dragging;
        private bool needsReset = true;

        public string SelectedId { get; set; }

        public Action<string> OnReroot;
        public Action<FamilyNode> OnContextMenu;

        public void ResetView()
        {
            needsReset = true;
        }

        public void Draw(Rect rect, FamilyGraph graph, TreeLayout layout)
        {
            Widgets.BeginGroup(rect);
            Rect local = rect.AtZero();
            if (needsReset)
                CenterOn(local, layout.rects[graph.rootId]);
            DrawLines(layout);
            DrawNodes(local, graph, layout);
            HandleCanvasInput(local);
            Widgets.EndGroup();
        }

        private void CenterOn(Rect local, Rect target)
        {
            zoom = 1f;
            offset = local.center - target.center * zoom;
            needsReset = false;
        }

        private Rect ToScreen(Rect r)
        {
            return new Rect(offset.x + r.x * zoom, offset.y + r.y * zoom, r.width * zoom, r.height * zoom);
        }

        private Vector2 ToScreen(Vector2 p) => offset + p * zoom;

        private void DrawLines(TreeLayout layout)
        {
            float width = Mathf.Max(1f, 2f * zoom);
            foreach (TreeLine line in layout.lines)
                Widgets.DrawLine(ToScreen(line.a), ToScreen(line.b), line.mateBar ? MateLineColor : LineColor, width);
        }

        private void DrawNodes(Rect local, FamilyGraph graph, TreeLayout layout)
        {
            var tracker = LineageTracker.Instance;
            foreach (FamilyNode node in graph.nodes.Values)
            {
                Rect r = ToScreen(layout.rects[node.id]);
                if (!r.Overlaps(local))
                    continue;
                Pawn pawn = tracker.TryGetLivePawn(node.id);
                NodeDrawer.Draw(r, node, pawn, node.id == graph.rootId, node.id == SelectedId, zoom);
                TooltipHandler.TipRegion(r, new TipSignal(() => NodeDrawer.Tooltip(node, pawn, graph),
                    node.id.GetHashCode()));
                HandleNodeClick(r, node);
            }
        }

        private void HandleNodeClick(Rect r, FamilyNode node)
        {
            Event e = Event.current;
            if (e.type != EventType.MouseDown || !r.Contains(e.mousePosition))
                return;
            if (e.button == 0)
            {
                SelectedId = node.id;
                if (e.clickCount >= 2)
                    OnReroot?.Invoke(node.id);
                e.Use();
            }
            else if (e.button == 1)
            {
                SelectedId = node.id;
                OnContextMenu?.Invoke(node);
                e.Use();
            }
        }

        private void HandleCanvasInput(Rect local)
        {
            Event e = Event.current;
            bool over = local.Contains(e.mousePosition);
            if (dragging && !Input.GetMouseButton(0) && !Input.GetMouseButton(2))
                dragging = false;
            switch (e.type)
            {
                case EventType.MouseDown when over && (e.button == 0 || e.button == 2):
                    dragging = true;
                    e.Use();
                    break;
                case EventType.MouseDrag when dragging:
                    offset += e.delta;
                    e.Use();
                    break;
                case EventType.MouseUp:
                case EventType.Ignore when dragging:
                    dragging = false;
                    break;
                case EventType.ScrollWheel when over:
                    HandleScroll(e);
                    e.Use();
                    break;
                case EventType.KeyDown:
                    HandleZoomKeys(e, local);
                    break;
            }
        }

        private void HandleScroll(Event e)
        {
            if (e.control || e.command)
                ZoomAround(e.mousePosition, e.delta.y > 0 ? 0.9f : 1.1f);
            else if (e.shift)
                offset.x -= e.delta.y * ScrollPanSpeed;
            else
                offset.y -= e.delta.y * ScrollPanSpeed;
        }

        private void HandleZoomKeys(Event e, Rect local)
        {
            if (e.keyCode == KeyCode.Equals || e.keyCode == KeyCode.Plus || e.keyCode == KeyCode.KeypadPlus)
            {
                ZoomAround(local.center, 1.1f);
                e.Use();
            }
            else if (e.keyCode == KeyCode.Minus || e.keyCode == KeyCode.KeypadMinus)
            {
                ZoomAround(local.center, 0.9f);
                e.Use();
            }
        }

        private void ZoomAround(Vector2 focus, float factor)
        {
            float newZoom = Mathf.Clamp(zoom * factor, MinZoom, MaxZoom);
            offset = focus - (focus - offset) * (newZoom / zoom);
            zoom = newZoom;
        }
    }
}
