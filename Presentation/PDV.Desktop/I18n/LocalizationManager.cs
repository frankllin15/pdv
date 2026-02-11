using System.ComponentModel;
using System.Globalization;

namespace PDV.Desktop.I18n;

public sealed class LocalizationManager : INotifyPropertyChanged
{
    private static readonly Lazy<LocalizationManager> _instance = new(() => new LocalizationManager());

    public static LocalizationManager Instance => _instance.Value;

    public event PropertyChangedEventHandler? PropertyChanged;

    private LocalizationManager() { }

    public string this[string key]
    {
        get
        {
            var value = Resources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
            return value ?? $"[{key}]";
        }
    }

    public void SetCulture(string cultureName)
    {
        var culture = new CultureInfo(cultureName);
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        Resources.Culture = culture;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    public static string Format(string key, params object[] args)
    {
        var template = Resources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? $"[{key}]";
        return string.Format(template, args);
    }
}
