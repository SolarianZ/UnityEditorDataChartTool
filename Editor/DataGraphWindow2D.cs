using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GBG.EditorDataGraph.Editor
{
    public class DataGraphWindow2D : EditorWindow
    {
        [MenuItem("Tools/Editor Data Graph/2D Graph")]
        public static DataGraphWindow2D Open()
        {
            return GetWindow<DataGraphWindow2D>("Data Graph 2D");
        }


        private readonly List<DataList> _dataTable = new List<DataList>();


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

            if (!newDataList)
            {
                _dataListView.RefreshItem(dataListIndex);
            }
        }

        public void AddData(string category, float x, float y)
        {
            AddData(category, new Vector2(x, y));
        }

        public bool RemoveData(string category, int index, bool keepEmptyCategory)
        {
            var dataListIndex = _dataTable.FindIndex(list => list.Category == category);
            if (dataListIndex < 0)
            {
                return false;
            }

            _dataTable[dataListIndex].RemoveAt(index);
            if (!keepEmptyCategory && _dataTable[dataListIndex].Count == 0)
            {
                _dataTable.RemoveAt(dataListIndex);
                return true;
            }

            _dataListView.RefreshItem(dataListIndex);

            return true;
        }

        public bool ClearData(string category, bool removeCategory)
        {
            var dataListIndex = _dataTable.FindIndex(list => list.Category == category);
            if (dataListIndex < 0)
            {
                return false;
            }

            if (removeCategory)
            {
                _dataTable.RemoveAt(dataListIndex);
                return true;
            }

            _dataTable[dataListIndex].Clear();
            _dataListView.RefreshItem(dataListIndex);

            return true;
        }

        public void ClearAllData(bool clearCategories)
        {
            if (clearCategories)
            {
                _dataTable.Clear();
                return;
            }

            foreach (var dataList in _dataTable)
            {
                dataList.Clear();
            }
        }


        private ListView _dataListView;

        private DataGraphDrawer _graphDrawer;


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
    }
}