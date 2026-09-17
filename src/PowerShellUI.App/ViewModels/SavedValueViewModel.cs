using System.Windows.Input;
using PowerShellUI.Core.Services;

namespace PowerShellUI.App.ViewModels;

/// <summary>
/// Ein Eintrag in der Liste "Gespeicherte Werte" im Hauptfenster, mit der Möglichkeit,
/// den Wert wieder zu vergessen.
/// </summary>
public sealed class SavedValueViewModel
{
    private readonly ParameterValueStore _valueStore;
    private readonly Action _onRemoved;

    public SavedValueViewModel(string name, string value, ParameterValueStore valueStore, Action onRemoved)
    {
        Name = name;
        Value = value;
        _valueStore = valueStore;
        _onRemoved = onRemoved;
        RemoveCommand = new RelayCommand(RemoveAsync);
    }

    public string Name { get; }

    public string Value { get; }

    public ICommand RemoveCommand { get; }

    private Task RemoveAsync()
    {
        _valueStore.Remove(Name);
        _onRemoved();
        return Task.CompletedTask;
    }
}
