using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GBG.EditorDataChart.Editor.LineChart2D
{
    internal class DataList
    {
        public string Category { get; }

        public bool Enabled { get; set; } = true;

        public Color Color { get; set; } = EditorGUIUtility.isProSkin ? Color.white : Color.black;

        public int Count => _dataList.Count;

        public Vector2 this[int index]
        {
            get => _dataList[index];
            set => _dataList[index] = value;
        }

        private readonly List<Vector2> _dataList = new List<Vector2>();


        public DataList(string category, Color? color = null)
        {
            Category = category;
            if (color != null)
            {
                Color = color.Value;
            }
        }

        public void Add(Vector2 data)
        {
            _dataList.Add(data);
        }

        public void RemoveAt(int index)
        {
            _dataList.RemoveAt(index);
        }

        public void Clear()
        {
            _dataList.Clear();
        }

        public int FindIndex(Predicate<Vector2> match)
        {
            return _dataList.FindIndex(match);
        }

        public int FindLastIndex(Predicate<Vector2> match)
        {
            return _dataList.FindLastIndex(match);
        }

        public List<Vector2>.Enumerator GetEnumerator()
        {
            return _dataList.GetEnumerator();
        }
    }
}