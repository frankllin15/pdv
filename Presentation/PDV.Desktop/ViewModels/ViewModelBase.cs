using CommunityToolkit.Mvvm.ComponentModel;
using PDV.Desktop.I18n;

namespace PDV.Desktop.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    protected void NotifyOnCultureChanged(params string[] propertyNames)
    {
        LocalizationManager.Instance.PropertyChanged += (_, _) =>
        {
            foreach (var name in propertyNames)
                OnPropertyChanged(name);
        };
    }
}
