using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GBG.EditorDataGraph.Editor
{
    public class DataGraphWindow2D : EditorWindow
    {
        // ReSharper disable once Unity.IncorrectMethodSignature
        [MenuItem("Tools/Bamboo/Editor Data Graph/2D Graph")]
        public static DataGraphWindow2D Open()
        {
            return GetWindow<DataGraphWindow2D>("Data Graph 2D");
        }

        public static DataGraphWindow2D Open(string title)
        {
            var window = Open();
            window.titleContent = new GUIContent(title);
            return window;
        }


        private readonly List<DataList> _dataTable = new List<DataList>();

        private ToolbarToggle _lockScaleToggle;

        private SliderInt _graphPointRadiusSlider;

        private ListView _categoryListView;

        private DataGraphDrawer _graphDrawer;

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


        #region Graph Management

        public void SetGraphScale(float xValueLength, float yMinValue, float yMaxValue)
        {
            _graphDrawer.FixedScale = new DataGraphScale(xValueLength, yMinValue, yMaxValue, true);
            _lockScaleToggle.SetValueWithoutNotify(true);
        }

        public void RemoveGraphScale()
        {
            _graphDrawer.FixedScale.Enabled = false;
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
                itemsSource = _dataTable,
                fixedItemHeight = 20,
                makeItem = MakeDataListViewItem,
                bindItem = BindDataListViewItem,
            };
            // rootVisualElement.Add(_dataListView);

            _graphDrawer = new DataGraphDrawer(_dataTable);
            _graphDrawer.PointRadius = (byte)_graphPointRadiusSlider.value;
            rootVisualElement.Add(_graphDrawer);

            _dataRangeSlider = new MinMaxSlider("Range", 0, 1, 0, 1);
            _dataRangeSlider.labelElement.style.minWidth = 55;
            _dataRangeSlider.RegisterValueChangedCallback(OnDataRangeChanged);
            rootVisualElement.Add(_dataRangeSlider);
        }

        private void Update()
        {
            Repaint();
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
            var oldStartIndex = _graphDrawer.StartIndex;
            var oldEndIndex = _graphDrawer.EndIndex;
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
            //     _graphDrawer.FixedScale.Enabled = false;
            // }
            // else
            // {
            //     // Translate indices, should lock scale
            //     var dataRange = _graphDrawer.GetDataBounds();
            //     _graphDrawer.FixedScale = new DataGraphScale(dataRange.width, dataRange.yMin, dataRange.yMax, true);
            // }

            _graphDrawer.StartIndex = (ushort)newStartIndex;
            _graphDrawer.EndIndex = (ushort)newEndIndex;
            _dataRangeSlider.label = $"Range[{newStartIndex.ToString()},{newEndIndex.ToString()}]";


            // When x and y close to 0.5f, Mathf.RoundToInt() may floors one of them and ceils another 
            // ReSharper disable once UnusedLocalFunctionReturnValue
            static bool CalcIndices(int oldStartIndex, int oldEndIndex, float newStartValue, float newEndValue,
                out int newStartIndex, out int newEndIndex)
            {
                newStartIndex = (ushort)Mathf.RoundToInt(newStartValue);
                newEndIndex = (ushort)Mathf.RoundToInt(newEndValue);

                var oldLength = oldEndIndex - oldStartIndex;
                var newLength = newEndValue - newStartValue;
                var isLenghtChanged = Mathf.Abs(oldLength - newLength) > 0.6f;
                if (!isLenghtChanged) // Translate indices
                {
                    newEndIndex = (ushort)(newStartIndex + oldLength);
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

            _graphPointRadiusSlider = new SliderInt("Radius", 1, 10) { value = 2, showInputField = true, };
            _graphPointRadiusSlider.labelElement.style.minWidth = 45;
            _graphPointRadiusSlider.Q<VisualElement>(className: "unity-base-slider__drag-container").style.width = 40;
            _graphPointRadiusSlider.Q<TextField>(className: "unity-base-slider__text-field").style.width = 22;
            _graphPointRadiusSlider.RegisterValueChangedCallback(OnToolbarGraphPointRadiusSliderChanged);
            toolbar.Add(_graphPointRadiusSlider);

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
                var dataRange = _graphDrawer.GetDataBounds();
                _graphDrawer.FixedScale = new DataGraphScale(dataRange.width, dataRange.yMin, dataRange.yMax, true);
            }
            else
            {
                _graphDrawer.FixedScale.Enabled = false;
            }
        }

        private void OnToolbarGraphPointRadiusSliderChanged(ChangeEvent<int> evt)
        {
            _graphDrawer.PointRadius = (byte)evt.newValue;
        }

        #endregion
    }
}