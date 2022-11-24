using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GBG.EditorDataGraph.Editor
{
    public class DataGraphWindow2D : EditorWindow
    {
        [MenuItem("Tools/Bamboo/Editor Data Graph/2D Graph")]
        public static DataGraphWindow2D Open()
        {
            return GetWindow<DataGraphWindow2D>("Data Graph 2D");
        }


        private readonly List<DataList> _dataTable = new List<DataList>();

        private ListView _dataListView;

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
                _dataListView.RefreshItem(dataListIndex);
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

            if (newDataList)
            {
                _dataListView.RefreshItems();
            }
            else
            {
                _dataListView.RefreshItem(dataListIndex);
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
                _dataListView.RefreshItems();
            }
            else
            {
                _dataListView.RefreshItem(dataListIndex);
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


        #region Lifecycle

        private void OnEnable()
        {
            _dataListView = new ListView
            {
                headerTitle = "Categories",
                showBorder = true,
                showFoldoutHeader = true,
                showBoundCollectionSize = false,
                reorderable = false,
                itemsSource = _dataTable,
                fixedItemHeight = 20,
                makeItem = MakeDataListViewItem,
                bindItem = BindDataListViewItem,
            };
            rootVisualElement.Add(_dataListView);

            _graphDrawer = new DataGraphDrawer(_dataTable);
            rootVisualElement.Add(_graphDrawer);

            _dataRangeSlider = new MinMaxSlider("Range", 0, 1, 0, 1);
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
            _graphDrawer.StartIndex = (ushort)Mathf.RoundToInt(evt.newValue.x);
            _graphDrawer.EndIndex = (ushort)Mathf.RoundToInt(evt.newValue.y);
            _dataRangeSlider.label = GetDataRangeSliderLabel();
        }

        private string GetDataRangeSliderLabel()
        {
            var minValue = Mathf.RoundToInt(_dataRangeSlider.minValue);
            var maxValue = Mathf.RoundToInt(_dataRangeSlider.maxValue);
            return $"Range[{minValue.ToString()},{maxValue.ToString()}]";
        }

        #endregion
    }
}