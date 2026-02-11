using System.ComponentModel;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace PDV.Desktop.I18n;

public class LocalizeExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public LocalizeExtension() { }

    public LocalizeExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding
        {
            Source = new LocalizedValue(Key),
            Path = nameof(LocalizedValue.Value),
            Mode = BindingMode.OneWay
        };
    }
}

internal sealed class LocalizedValue : INotifyPropertyChanged
{
    private readonly string _key;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Value => LocalizationManager.Instance[_key];

    public LocalizedValue(string key)
    {
        _key = key;
        LocalizationManager.Instance.PropertyChanged += OnCultureChanged;
    }

    private void OnCultureChanged(object? sender, PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }
}
