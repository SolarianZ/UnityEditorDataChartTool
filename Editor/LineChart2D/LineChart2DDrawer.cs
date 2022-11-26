using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace GBG.EditorDataChart.Editor.LineChart2D
{
    internal struct LineChartScale2D
    {
        public bool Enabled;

        public float XLength;

        public float YMin;

        public float YMax;


        public LineChartScale2D(float xLength, float yMin, float yMax, bool enabled)
        {
            XLength = xLength;
            YMin = yMin;
            YMax = yMax;
            Enabled = enabled;
        }

        public void OverrideDataRange(ref float xMin, out float xMax, out float yMin, out float yMax)
        {
            Assert.IsTrue(XLength > 0, $"{nameof(XLength)}({XLength:F5}) <= 0.");
            Assert.IsTrue(YMin < YMax, $"{nameof(YMin)}({YMin:F5}) >= {nameof(YMax)}({YMax:F5}).");

            xMax = xMin + XLength;
            yMin = YMin;
            yMax = YMax;
        }

        public override string ToString()
        {
            var state = Enabled ? "Enabled" : "Disabled";
            return $"{state}, {nameof(XLength)}={XLength:F5}, {nameof(YMin)}={YMin:F5}, {nameof(YMax)}={YMax:F5}.";
        }
    }

    internal class LineChart2DDrawer : VisualElement
    {
        public byte PointRadius { get; set; } = 2;

        public ushort StartIndex { get; set; } = 0;

        public ushort EndIndex { get; set; } = ushort.MaxValue;

        public ref LineChartScale2D FixedScale => ref _fixedScale;

        private LineChartScale2D _fixedScale;

        private readonly List<DataList> _dataTable;

        private Rect _chartBounds;

        private Rect _dataBounds;

        private GUIStyle _leftAlignedLabelStyle;

        private GUIStyle _rightAlignedLabelStyle;

        private GUIStyle _centerAlignedLabelStyle;


        public LineChart2DDrawer(List<DataList> dataTable)
        {
            _dataTable = dataTable;

            style.flexGrow = 1;
            style.flexShrink = 0;

            var chartContainer = new IMGUIContainer(DrawLineChart2D)
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
            chartContainer.RegisterCallback<GeometryChangedEvent>(evt => { _chartBounds = evt.newRect; });
            Add(chartContainer);
        }

        public Rect GetDataBounds()
        {
            var hasPoint = false;
            float xMin = default;
            float xMax = default;
            float yMin = default;
            float yMax = default;

            foreach (var dataList in _dataTable)
            {
                var dataCount = dataList.Count;
                Assert.IsTrue(StartIndex <= EndIndex);

                for (var i = StartIndex; i < dataCount && i <= EndIndex; i++)
                {
                    var point = dataList[i];
                    if (!hasPoint || xMin > point.x) xMin = point.x;
                    if (!hasPoint || xMax < point.x) xMax = point.x;
                    if (!hasPoint || yMin > point.y) yMin = point.y;
                    if (!hasPoint || yMax < point.y) yMax = point.y;
                    hasPoint = true;
                }
            }

            if (!hasPoint)
            {
                return Rect.zero;
            }

            // Ensure the width of bounds is not zero
            if (Mathf.Approximately(xMin, xMax))
            {
                xMax = xMin + 1;
            }

            // Ensure the height of bounds is not zero
            if (Mathf.Approximately(yMin, yMax))
            {
                yMax = yMin + 1;
            }

            if (FixedScale.Enabled)
            {
                FixedScale.OverrideDataRange(ref xMin, out xMax, out yMin, out yMax);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }


        private void DrawLineChart2D()
        {
            var baseColor = GetBaseColor();
            var originalGuiColor = GUI.color;
            PrepareLabelStyles();

            // Find min and max values of x and y
            _dataBounds = GetDataBounds();

            // Draw axes
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

            // Draw data lines
            var hover = false;
            var hoverData = Vector2.zero;
            var hoverColor = Color.black;
            foreach (var dataList in _dataTable)
            {
                if (!dataList.Enabled || dataList.Count == 0)
                {
                    continue;
                }

                var normal = Vector3.forward;
                Handles.color = dataList.Color;

                var mousePos = Event.current.mousePosition;
                var firstPoint = TransformPoint(dataList[0]);
                var expandedPointRadius = PointRadius * 1.5f;
                if (!hover && (mousePos - firstPoint).sqrMagnitude <= expandedPointRadius * expandedPointRadius)
                {
                    hover = true;
                    hoverData = dataList[0];
                    hoverColor = dataList.Color;
                    Handles.DrawSolidDisc(firstPoint, normal, expandedPointRadius);
                }
                else
                {
                    Handles.DrawSolidDisc(firstPoint, normal, PointRadius);
                }

                for (var i = StartIndex + 1; i < dataList.Count && i <= EndIndex; i++)
                {
                    // Line segment
                    var lineStart = TransformPoint(dataList[i - 1]);
                    var lineEnd = TransformPoint(dataList[i]);
                    Handles.DrawLine(lineStart, lineEnd);

                    // Expand the size of mouse hovered point
                    if (!hover && (mousePos - lineEnd).sqrMagnitude <= expandedPointRadius * expandedPointRadius)
                    {
                        hover = true;
                        hoverData = dataList[i];
                        hoverColor = dataList.Color;
                        Handles.DrawSolidDisc(lineEnd, normal, expandedPointRadius);
                    }
                    else
                    {
                        Handles.DrawSolidDisc(lineEnd, normal, PointRadius);
                    }
                }
            }

            // Draw value of mouse hovered point
            if (hover)
            {
                var hoverLabelPos = (xAxisStart + xAxisEnd) / 2 - new Vector2(axisLabelSize.x / 2, 0);
                var hoverLabelRectPos = new Rect(hoverLabelPos, axisLabelSize);
                GUI.color = hoverColor;
                GUI.Label(hoverLabelRectPos, $"({hoverData.x:F5}, {hoverData.y:F5})", _centerAlignedLabelStyle);
                GUI.color = originalGuiColor;
            }
        }

        private void PrepareLabelStyles()
        {
            if (_leftAlignedLabelStyle == null)
            {
                _leftAlignedLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };
            }

            if (_rightAlignedLabelStyle == null)
            {
                _rightAlignedLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };
            }

            if (_centerAlignedLabelStyle == null)
            {
                _centerAlignedLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            }
        }

        /// <summary>
        /// Transform data point to window gui position.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="lockAspect"></param>
        /// <returns></returns>
        private Vector2 TransformPoint(Vector2 point, bool lockAspect = false)
        {
            var xScale = _chartBounds.width / _dataBounds.width;
            var yScale = _chartBounds.height / _dataBounds.height;
            var scale = lockAspect ? Vector2.one * Math.Min(xScale, yScale) : new Vector2(xScale, yScale);
            scale.y *= -1;
            var offset = point - _dataBounds.center;
            offset.Scale(scale);

            // Don't use windowBounds.center, transform point to window center
            var windowCenter = _chartBounds.size / 2;
            var windowSpacePoint = windowCenter + offset;

            return windowSpacePoint;
        }

        private Color GetBaseColor()
        {
            return EditorGUIUtility.isProSkin ? Color.white : Color.black;
        }
    }
}