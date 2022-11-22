using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GBG.EditorDataGraph.Editor
{
    internal class DataListLabel : VisualElement
    {
        private DataList _target;

        private readonly Toggle _enabled;

        private readonly ColorField _colorField;

        private readonly Label _countLabel;

        private readonly Label _categoryLabel;


        public DataListLabel()
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            _enabled = new Toggle();
            _enabled.RegisterValueChangedCallback(evt =>
            {
                if (_target != null) { _target.Enabled = evt.newValue; }
            });
            Add(_enabled);

            _colorField = new ColorField
            {
                style = { flexShrink = 1, width = 16, height = 16, }, showEyeDropper = false,
            };
            _colorField.RegisterValueChangedCallback(evt =>
            {
                if (_target != null) { _target.Color = evt.newValue; }
            });
            Add(_colorField);

            _countLabel = new Label();
            Add(_countLabel);

            _categoryLabel = new Label();
            Add(_categoryLabel);
        }

        public void SetTarget(DataList target)
        {
            _target = target;
            Refresh();
        }

        public void Refresh()
        {
            if (_target == null)
            {
                _enabled.SetValueWithoutNotify(true);
                _colorField.SetValueWithoutNotify(default);
                _categoryLabel.text = null;
                _countLabel.text = null;
            }
            else
            {
                _enabled.SetValueWithoutNotify(_target.Enabled);
                _colorField.SetValueWithoutNotify(_target.Color);
                _countLabel.text = $"[{_target.Count.ToString()}]\t";
                _categoryLabel.text = _target.Category;
            }
        }
    }
}