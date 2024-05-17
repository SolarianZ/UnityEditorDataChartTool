using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GBG.EditorDataChart.Editor.LineChart2D
{
    public class LineChart2DWindow : EditorWindow
    {
        // ReSharper disable once Unity.IncorrectMethodSignature
        [MenuItem("Tools/Bamboo/Editor Data Chart/Line Chart 2D")]
        public static LineChart2DWindow Open()
        {
            return GetWindow<LineChart2DWindow>("Line Chart 2D");
        }

        public static LineChart2DWindow Open(string title)
        {
            var window = Open();
            window.titleContent = new GUIContent(title);
            return window;
        }


        private readonly List<DataList> _dataTable = new();

        private ToolbarToggle _lockScaleToggle;

        private SliderInt _chartPointRadiusSlider;

        private ListView _categoryListView;

        private LineChart2DDrawer _chartDrawer;

        private MinMaxSlider _dataRangeSlider;


        #region Data Management

        public void SetColor(string category, Color color)
        {
            DataList dataList;
            var newDataList = false;
            var dataListIndex = _dataTable.FindIndex(list => list.Category == category);
            if (dataListIndex < 0)
            {
                newDataList = true;
                dataListIndex = _dataTable.Count;
                dataList = new DataList(category);
                _dataTable.Add(dataList);
            }
            else
            {
                dataList = _dataTable[dataListIndex];
            }

            dataList.Color = color;

            if (!newDataList)
            {
                _categoryListView.RefreshItem(dataListIndex);
            }
        }

        public void AddData(string category, Vector2 data)
        {
            DataList dataList;
            var newDataList = false;
            var dataListIndex = _dataTable.FindIndex(list => list.Category == category);
            if (dataListIndex < 0)
            {
                newDataList = true;
                dataListIndex = _dataTable.Count;
                dataList = new DataList(category);
                _dataTable.Add(dataList);
            }
            else
            {
                dataList = _dataTable[dataListIndex];
            }

            dataList.Add(data);
            if (dataList.Count > 1 && dataList[dataList.Count - 2].x >= data.x)
            {
                Debug.LogWarning($"X value not in ascending order, category={category}, " +
                                 $"index={dataList.Count - 1}, value=({data.x:F5}, {data.y:F5}).");
            }

            if (newDataList)
            {
                _categoryListView.RefreshItems();
            }
            else
            {
                _categoryListView.RefreshItem(dataListIndex);
            }

            OnDataListChanged(dataList.Count, false);
        }

        public void AddData(string category, float x, float y)
        {
            AddData(category, new Vector2(x, y));
        }

        public bool RemoveData(string category, int index)
        {
            var dataListIndex = _dataTable.FindIndex(list => list.Category == category);
            if (dataListIndex < 0)
            {
                return false;
            }

            var dataList = _dataTable[dataListIndex];
            dataList.RemoveAt(index);
            if (_dataTable[dataListIndex].Count == 0)
            {
                _dataTable.RemoveAt(dataListIndex);
                _categoryListView.RefreshItems();
            }
            else
            {
                _categoryListView.RefreshItem(dataListIndex);
            }

            OnDataListChanged(dataList.Count, true);

            return true;
        }

        public bool ClearData(string category)
        {
            var dataListIndex = _dataTable.FindIndex(list => list.Category == category);
            if (dataListIndex < 0)
            {
                return false;
            }

            _dataTable.RemoveAt(dataListIndex);
            OnDataListChanged(0, true);

            return true;
        }

        public void ClearAllData()
        {
            _dataTable.Clear();
            OnDataListChanged(0, true);
        }

        public int FindDataIndex(string category, Predicate<Vector2> match)
        {
            var dataList = _dataTable.FirstOrDefault(list => list.Category == category);
            return dataList?.FindIndex(match) ?? -1;
        }

        public int FindDataLastIndex(string category, Predicate<Vector2> match)
        {
            var dataList = _dataTable.FirstOrDefault(list => list.Category == category);
            return dataList?.FindLastIndex(match) ?? -1;
        }

        private void OnDataListChanged(int dataCount, bool isDataRemoved)
        {
            var oldHighLimit = (int)_dataRangeSlider.highLimit;
            var newHighLimit = oldHighLimit;
            if (isDataRemoved || dataCount < 1)
            {
                var maxCount = 0;
                foreach (var dataList in _dataTable)
                {
                    if (maxCount < dataList.Count)
                    {
                        maxCount = dataList.Count;
                    }
                }

                newHighLimit = maxCount > 0 ? maxCount - 1 : 0;
            }
            else if (oldHighLimit < dataCount - 1)
            {
                newHighLimit = dataCount - 1;
            }

            var maxValue = _dataRangeSlider.maxValue;
            _dataRangeSlider.highLimit = newHighLimit;

            if (maxValue > newHighLimit)
            {
                _dataRangeSlider.maxValue = newHighLimit;
            }
            else if (Mathf.RoundToInt(maxValue) == oldHighLimit)
            {
                _dataRangeSlider.maxValue = newHighLimit;
            }
        }

        #endregion


        #region Chart Management

        public void SetChartScale(float xValueLength, float yMinValue, float yMaxValue)
        {
            _chartDrawer.FixedScale = new LineChartScale2D(xValueLength, yMinValue, yMaxValue, true);
            _lockScaleToggle.SetValueWithoutNotify(true);
        }

        public void RemoveChartScale()
        {
            _chartDrawer.FixedScale.Enabled = false;
            _lockScaleToggle.SetValueWithoutNotify(false);
        }

        #endregion


        #region Lifecycle

        private void OnEnable()
        {
            rootVisualElement.style.paddingBottom = 8;

            var toolbar = CreateToolbar();
            rootVisualElement.Add(toolbar);

            _categoryListView = new ListView
            {
                showBorder = true,
                reorderable = false,
                fixedItemHeight = 20,
                itemsSource = _dataTable,
                makeItem = MakeDataListViewItem,
                bindItem = BindDataListViewItem,
            };
            // rootVisualElement.Add(_dataListView);

            _chartDrawer = new LineChart2DDrawer(_dataTable);
            _chartDrawer.PointRadius = (byte)_chartPointRadiusSlider.value;
            rootVisualElement.Add(_chartDrawer);

            _dataRangeSlider = new MinMaxSlider("Range", 0, 1, 0, 1);
            _dataRangeSlider.labelElement.style.minWidth = 55;
            _dataRangeSlider.RegisterValueChangedCallback(OnDataRangeChanged);
            rootVisualElement.Add(_dataRangeSlider);
        }

        private void Update()
        {
            Repaint();
        }

        private void ShowButton(Rect position)
        {
            if (GUI.Button(position, EditorGUIUtility.IconContent("_Help"), GUI.skin.FindStyle("IconButton")))
            {
                Application.OpenURL("https://github.com/SolarianZ/UnityEditorDataChartTool");
            }
        }

        private VisualElement MakeDataListViewItem()
        {
            return new DataListLabel();
        }

        private void BindDataListViewItem(VisualElement element, int index)
        {
            var dataListLabel = (DataListLabel)element;
            dataListLabel.SetTarget(_dataTable[index]);
        }

        private void OnDataRangeChanged(ChangeEvent<Vector2> evt)
        {
            var oldStartIndex = _chartDrawer.StartIndex;
            var oldEndIndex = _chartDrawer.EndIndex;
            //var isLenghtChanged =
            CalcIndices(oldStartIndex, oldEndIndex, evt.newValue.x, evt.newValue.y,
                out var newStartIndex, out var newEndIndex);
            _dataRangeSlider.SetValueWithoutNotify(new Vector2(newStartIndex, newEndIndex));

            if (newStartIndex == oldStartIndex && newEndIndex == oldEndIndex)
            {
                return;
            }

            // Let users choose when to lock state from toolbar
            // if (isLenghtChanged)
            // {
            //     _chartDrawer.FixedScale.Enabled = false;
            // }
            // else
            // {
            //     // Translate indices, should lock scale
            //     var dataRange = _chartDrawer.GetDataBounds();
            //     _chartDrawer.FixedScale = new LineChartScale2D(dataRange.width, dataRange.yMin, dataRange.yMax, true);
            // }

            _chartDrawer.StartIndex = (ushort)newStartIndex;
            _chartDrawer.EndIndex = (ushort)newEndIndex;
            _dataRangeSlider.label = $"Range[{newStartIndex.ToString()},{newEndIndex.ToString()}]";


            // When x and y close to 0.5f, Mathf.RoundToInt() may floors one of them and ceils another 
            // ReSharper disable once UnusedLocalFunctionReturnValue
            bool CalcIndices(int internalOldStartIndex, int internalOldEndIndex, float newStartValue, float newEndValue,
                out int internalNewStartIndex, out int internalNewEndIndex)
            {
                internalNewStartIndex = (ushort)Mathf.RoundToInt(newStartValue);
                internalNewEndIndex = (ushort)Mathf.RoundToInt(newEndValue);

                var oldLength = internalOldEndIndex - internalOldStartIndex;
                var newLength = newEndValue - newStartValue;
                var isLenghtChanged = Mathf.Abs(oldLength - newLength) > 0.6f;
                if (!isLenghtChanged) // Translate indices
                {
                    internalNewEndIndex = (ushort)(internalNewStartIndex + oldLength);
                }

                return isLenghtChanged;
            }
        }

        #endregion


        #region Toolbar

        private Toolbar CreateToolbar()
        {
            var toolbar = new Toolbar();

            var categoryToggle = new ToolbarToggle { text = "Categories", };
            categoryToggle.RegisterValueChangedCallback(OnToolbarCategoryToggleChanged);
            toolbar.Add(categoryToggle);

            _lockScaleToggle = new ToolbarToggle { text = "Lock Scale" };
            _lockScaleToggle.RegisterValueChangedCallback(OnToolbarLockScaleToggleChanged);
            toolbar.Add(_lockScaleToggle);

            _chartPointRadiusSlider = new SliderInt("Radius", 1, 10) { value = 2, };
            _chartPointRadiusSlider.labelElement.style.minWidth = 45;
            _chartPointRadiusSlider.Q<VisualElement>(className: "unity-base-slider__drag-container").style.width = 40;
            // _chartPointRadiusSlider.Q<VisualElement>(className: "unity-base-slider__input").style.width = 40;
            // _chartPointRadiusSlider.Q<TextField>(className: "unity-base-slider__text-field").style.width = 22;
            _chartPointRadiusSlider.RegisterValueChangedCallback(OnToolbarChartPointRadiusSliderChanged);
            toolbar.Add(_chartPointRadiusSlider);
            UpdateToolbarChartPointRadiusSliderTitle();

            return toolbar;
        }

        private void OnToolbarCategoryToggleChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                rootVisualElement.Insert(1, _categoryListView);
            }
            else
            {
                rootVisualElement.Remove(_categoryListView);
            }
        }

        private void OnToolbarLockScaleToggleChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                var dataRange = _chartDrawer.GetDataBounds();
                _chartDrawer.FixedScale = new LineChartScale2D(dataRange.width, dataRange.yMin, dataRange.yMax, true);
            }
            else
            {
                _chartDrawer.FixedScale.Enabled = false;
            }
        }

        private void OnToolbarChartPointRadiusSliderChanged(ChangeEvent<int> evt)
        {
            _chartDrawer.PointRadius = (byte)evt.newValue;
            UpdateToolbarChartPointRadiusSliderTitle();
        }

        private void UpdateToolbarChartPointRadiusSliderTitle()
        {
            _chartPointRadiusSlider.label = $"Radius({_chartPointRadiusSlider.value})";
        }

        #endregion
    }
}