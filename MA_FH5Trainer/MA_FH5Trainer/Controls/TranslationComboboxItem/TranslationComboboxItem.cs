using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace HorizonTuner.Controls.TranslationComboboxItem;

public class TranslationComboboxItem : ComboBoxItem
{
    public static readonly DependencyProperty TranslatorsProperty
        = DependencyProperty.Register(nameof(Translators),
            typeof(string),
            typeof(TranslationComboboxItem),
            new PropertyMetadata(default(string)));
    
    [Bindable(true)]
    [Category("HorizonTuner")]
    public string Translators
    {
        get => (string)GetValue(TranslatorsProperty);
        set => SetValue(TranslatorsProperty, value);
    }

    static TranslationComboboxItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TranslationComboboxItem), new FrameworkPropertyMetadata(typeof(TranslationComboboxItem)));
    }
}