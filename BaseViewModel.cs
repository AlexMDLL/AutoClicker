using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoClicker
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }
    }

    /// <summary>Элемент списка-переключателя (кнопка мыши, режим, язык, интервал).</summary>
    public class SelectableItem : BaseViewModel
    {
        private string _label;
        public string Label { get => _label; set => SetField(ref _label, value); }

        private string _description;
        public string Description { get => _description; set => SetField(ref _description, value); }

        private string _icon;
        public string Icon { get => _icon; set => SetField(ref _icon, value); }

        private object _tag;
        public object Tag { get => _tag; set => SetField(ref _tag, value); }

        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set => SetField(ref _isSelected, value); }
    }
}
