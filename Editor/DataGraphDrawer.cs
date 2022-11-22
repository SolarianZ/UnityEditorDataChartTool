using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GBG.EditorDataGraph.Editor
{
    internal class DataGraphDrawer : VisualElement
    {
        private readonly List<DataList> _dataTable;

        public readonly SliderInt _pointRadius;

        private Rect _graphBounds;

        private Rect _DataBounds;


        public DataGraphDrawer(List<DataList> dataTable)
        {
            _dataTable = dataTable;

            style.flexGrow = 1;
            style.flexShrink = 0;

            _pointRadius = new SliderInt("Radius", 1, 10) { value = 2, showInputField = true, };
            Add(_pointRadius);

            var graphContainer = new IMGUIContainer(DrawDataGraph)
            {
                style =
                {
                    flexGrow = 1,
                    marginLeft = 20,
                    marginRight = 20,
                    marginTop = 20,
                    marginBottom = 20,
                }
            };
            graphContainer.RegisterCallback<GeometryChangedEvent>(evt => { _graphBounds = evt.newRect; });
            Add(graphContainer);
        }

        private void DrawDataGraph()
        {
            // Ranges
            _DataBounds = GetDataBounds();

            // Draw lines
            foreach (var dataList in _dataTable)
            {
                if (!dataList.Enabled || dataList.Count == 0)
                {
                    continue;
                }

                var normal = Vector3.forward;
                var from = Vector3.up;
                var angle = 360f;
                var radius = _pointRadius.value;
                Handles.color = dataList.Color;

                var firstPoint = TransformPoint(dataList[0]);
                Handles.DrawSolidArc(firstPoint, normal, from, angle, radius);

                for (var i = 1; i < dataList.Count; i++)
                {
                    var lineStart = TransformPoint(dataList[i - 1]);
                    var lineEnd = TransformPoint(dataList[i]);
                    Handles.DrawSolidArc(lineEnd, normal, from, angle, radius);
                    Handles.DrawLine(lineStart, lineEnd);
                    Handles.DrawPolyLine();
                }
            }
        }

        private Rect GetDataBounds()
        {
            var hasPoint = false;
            float? xMin = null;
            float? xMax = null;
            float? yMin = null;
            float? yMax = null;

            foreach (var dataList in _dataTable)
            {
                foreach (var point in dataList)
                {
                    hasPoint = true;
                    if (xMin == null || xMin > point.x) xMin = point.x;
                    if (xMax == null || xMax < point.x) xMax = point.x;
                    if (yMin == null || yMin > point.y) yMin = point.y;
                    if (yMax == null || yMax < point.y) yMax = point.y;
                }
            }

            if (!hasPoint)
            {
                return Rect.zero;
            }

            return Rect.MinMaxRect(xMin.Value, yMin.Value, xMax.Value, yMax.Value);
        }

        private Vector2 TransformPoint(Vector2 point, bool lockAspect = false)
        {
            var xScale = _graphBounds.width / _DataBounds.width;
            var yScale = _graphBounds.height / _DataBounds.height;
            var scale = lockAspect ? Vector2.one * Math.Min(xScale, yScale) : new Vector2(xScale, yScale);
            scale.y *= -1;
            var offset = point - _DataBounds.center;
            offset.Scale(scale);

            // Don't use windowBounds.center, transform point to window center
            var windowCenter = _graphBounds.size / 2;
            var windowSpacePoint = windowCenter + offset;

            return windowSpacePoint;
        }
    }
}