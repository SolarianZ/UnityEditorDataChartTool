using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace GBG.EditorDataGraph.Editor
{
    // TODO FIXME: Can't show horizontal linear
    internal class DataGraphDrawer : VisualElement
    {
        public ushort StartIndex { get; set; } = 0;

        public ushort EndIndex { get; set; } = ushort.MaxValue;


        private readonly List<DataList> _dataTable;

        public readonly SliderInt _pointRadius;

        private Rect _graphBounds;

        private Rect _dataBounds;

        private GUIStyle _leftAlignedLabelStyle;

        private GUIStyle _rightAlignedLabelStyle;


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
            var baseColor = GetBaseColor();
            var originalGuiColor = GUI.color;

            // Ranges
            _dataBounds = GetDataBounds();

            // Axes
            PrepareAxisLabelStyles();
            Handles.color = baseColor;

            // X axis
            var xAxisStart = TransformPoint(new Vector2(_dataBounds.xMin, _dataBounds.yMin));
            var xAxisEnd = TransformPoint(new Vector2(_dataBounds.xMax, _dataBounds.yMin));
            Handles.DrawLine(xAxisStart, xAxisEnd);

            // X axis labels
            var axisLabelSize = new Vector2(200, 20);
            GUI.color = baseColor;
            var xStartLabelPos = new Rect(xAxisStart, axisLabelSize);
            GUI.Label(xStartLabelPos, _dataBounds.xMin.ToString(CultureInfo.InvariantCulture), _leftAlignedLabelStyle);
            var xEndLabelPos = new Rect(xAxisEnd - new Vector2(axisLabelSize.x, 0), axisLabelSize);
            GUI.Label(xEndLabelPos, _dataBounds.xMax.ToString(CultureInfo.InvariantCulture), _rightAlignedLabelStyle);
            GUI.color = originalGuiColor;

            // Y axis
            var yAxisStart = TransformPoint(new Vector2(_dataBounds.xMin, _dataBounds.yMin));
            var yAxisEnd = TransformPoint(new Vector2(_dataBounds.xMin, _dataBounds.yMax));
            Handles.DrawLine(yAxisStart, yAxisEnd);

            // Y axis labels
            GUI.color = baseColor;
            var yStartLabelPos = new Rect(yAxisStart - new Vector2(0, axisLabelSize.x), axisLabelSize);
            GUIUtility.RotateAroundPivot(90, yStartLabelPos.position);
            GUI.Label(yStartLabelPos, _dataBounds.yMin.ToString(CultureInfo.InvariantCulture), _rightAlignedLabelStyle);
            GUIUtility.RotateAroundPivot(-90, yStartLabelPos.position);
            var yEndLabelPos = new Rect(yAxisEnd, axisLabelSize);
            GUIUtility.RotateAroundPivot(90, yEndLabelPos.position);
            GUI.Label(yEndLabelPos, _dataBounds.yMax.ToString(CultureInfo.InvariantCulture), _leftAlignedLabelStyle);
            GUIUtility.RotateAroundPivot(-90, yEndLabelPos.position);
            GUI.color = originalGuiColor;

            // Data lines
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

                for (var i = StartIndex + 1; i < dataList.Count && i <= EndIndex; i++)
                {
                    var lineStart = TransformPoint(dataList[i - 1]);
                    var lineEnd = TransformPoint(dataList[i]);
                    Handles.DrawSolidArc(lineEnd, normal, from, angle, radius);
                    Handles.DrawLine(lineStart, lineEnd);
                    Handles.DrawPolyLine();
                }
            }
        }

        private void PrepareAxisLabelStyles()
        {
            if (_leftAlignedLabelStyle == null)
            {
                _leftAlignedLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };
            }

            if (_rightAlignedLabelStyle == null)
            {
                _rightAlignedLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };
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
                var dataCount = dataList.Count;
                Assert.IsTrue(StartIndex <= EndIndex);

                for (var i = StartIndex; i < dataCount && i <= EndIndex; i++)
                {
                    var point = dataList[i];
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
            var xScale = _graphBounds.width / _dataBounds.width;
            var yScale = _graphBounds.height / _dataBounds.height;
            var scale = lockAspect ? Vector2.one * Math.Min(xScale, yScale) : new Vector2(xScale, yScale);
            scale.y *= -1;
            var offset = point - _dataBounds.center;
            offset.Scale(scale);

            // Don't use windowBounds.center, transform point to window center
            var windowCenter = _graphBounds.size / 2;
            var windowSpacePoint = windowCenter + offset;

            return windowSpacePoint;
        }

        private Color GetBaseColor()
        {
            return EditorGUIUtility.isProSkin ? Color.white : Color.black;
        }
    }
}