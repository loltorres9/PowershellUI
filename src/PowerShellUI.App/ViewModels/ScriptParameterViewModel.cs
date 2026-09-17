using PowerShellUI.Core.Models;
using PowerShellUI.Core.Services;

namespace PowerShellUI.App.ViewModels;

public enum ParameterInputKind
{
    Text,
    Switch,
    Choice,
}

/// <summary>
/// UI-Wrapper um einen aus dem Skript geparsten Parameter. Stellt je nach Parametertyp
/// (ValidateSet -> Dropdown, switch/bool -> Checkbox, sonst -> Textfeld) den passenden
/// bearbeitbaren Wert für die WPF-Bindings bereit. Ist der Parameter als "merken" markiert,
/// wird sein Wert über <see cref="ParameterValueStore"/> lokal persistiert und beim nächsten
/// Laden eines Skripts mit gleichnamigem Parameter automatisch vorausgefüllt.
/// </summary>
public sealed class ScriptParameterViewModel : ObservableObject
{
    private readonly ParameterValueStore _valueStore;

    private string? _textValue;
    private bool _switchValue;
    private string? _selectedChoice;
    private bool _isRemembered;

    public ScriptParameterViewModel(ScriptParameterInfo info, ParameterValueStore valueStore)
    {
        Info = info;
        _valueStore = valueStore;

        Kind = info.ValidateSet.Count > 0
            ? ParameterInputKind.Choice
            : info.IsSwitch
                ? ParameterInputKind.Switch
                : ParameterInputKind.Text;

        var savedValue = valueStore.Get(info.Name);
        _isRemembered = savedValue is not null;

        _textValue = savedValue ?? info.DefaultValue;
        _selectedChoice = savedValue ?? (info.ValidateSet.Count > 0 ? info.ValidateSet[0] : null);
        _switchValue = savedValue is not null && bool.TryParse(savedValue, out var savedSwitch) && savedSwitch;
    }

    public ScriptParameterInfo Info { get; }

    public ParameterInputKind Kind { get; }

    public string Name => Info.Name;

    public string DisplayLabel => Info.Mandatory ? $"{Info.Name} *" : Info.Name;

    public string? HelpMessage => Info.HelpMessage;

    public IReadOnlyList<string> Choices => Info.ValidateSet;

    public string? TextValue
    {
        get => _textValue;
        set
        {
            if (SetField(ref _textValue, value) && IsRemembered)
            {
                _valueStore.Set(Name, value);
            }
        }
    }

    public bool SwitchValue
    {
        get => _switchValue;
        set
        {
            if (SetField(ref _switchValue, value) && IsRemembered)
            {
                _valueStore.Set(Name, value ? "true" : "false");
            }
        }
    }

    public string? SelectedChoice
    {
        get => _selectedChoice;
        set
        {
            if (SetField(ref _selectedChoice, value) && IsRemembered)
            {
                _valueStore.Set(Name, value);
            }
        }
    }

    /// <summary>
    /// Wenn aktiviert, wird der aktuelle Wert sofort gespeichert und bei jeder Änderung
    /// aktualisiert; beim Deaktivieren wird der gespeicherte Wert wieder entfernt.
    /// </summary>
    public bool IsRemembered
    {
        get => _isRemembered;
        set
        {
            if (!SetField(ref _isRemembered, value))
            {
                return;
            }

            if (value)
            {
                _valueStore.Set(Name, GetRawValue());
            }
            else
            {
                _valueStore.Remove(Name);
            }
        }
    }

    public string? GetRawValue() => Kind switch
    {
        ParameterInputKind.Switch => SwitchValue ? "true" : "false",
        ParameterInputKind.Choice => SelectedChoice,
        _ => TextValue,
    };
}
