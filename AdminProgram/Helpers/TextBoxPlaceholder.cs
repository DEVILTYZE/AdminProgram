using System.Windows;
using System.Windows.Controls;

namespace AdminProgram.Helpers
{
    public class TextBoxPlaceholder : TextBox
    {
        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(nameof(Placeholder), 
            typeof(string), typeof(TextBoxPlaceholder), new PropertyMetadata(""));

        public TextBoxPlaceholder() => DefaultStyleKey = typeof(TextBoxPlaceholder);
    }
}