using System.Windows;
using System.Windows.Controls;

namespace PowerShellUI.App.ViewModels;

public sealed class ParameterTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextTemplate { get; set; }

    public DataTemplate? SwitchTemplate { get; set; }

    public DataTemplate? ChoiceTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        if (item is ScriptParameterViewModel parameter)
        {
            return parameter.Kind switch
            {
                ParameterInputKind.Switch => SwitchTemplate,
                ParameterInputKind.Choice => ChoiceTemplate,
                _ => TextTemplate,
            };
        }

        return base.SelectTemplate(item, container);
    }
}
