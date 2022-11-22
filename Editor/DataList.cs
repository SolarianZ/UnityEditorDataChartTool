using System;
using System.Collections.Generic;
using UnityEngine;

namespace GBG.EditorDataGraph.Editor
{
    internal class DataList
    {
        public string Category { get; }

        public bool Enabled { get; set; } = true;

        public Color Color { get; set; } = Color.white;

        public int Count => _dataList.Count;

        public Vector2 this[int index]
        {
            get => _dataList[index];
            set => _dataList[index] = value;
        }

        private readonly List<Vector2> _dataList = new List<Vector2>();

        public Range? Range { get; private set; }


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

        public List<Vector2>.Enumerator GetEnumerator()
        {
            return _dataList.GetEnumerator();
        }
    }
}