using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Events;

namespace CSharpIntermediate.Code
{
    public class UIDemo : StartupScript
    {
        private TextBlock textBlock;
        private EditText editText;

        public override void Start()
        {
            // Retrieve the page property from the UI component
            var page = Entity.Get<UIComponent>().Page;

            // Retrieve UI elements by Type and name
            textBlock = page.RootElement.FindVisualChildOfType<TextBlock>("MyTextBlock");
            editText = page.RootElement.FindVisualChildOfType<EditText>("MyEditText");

            // When the text changes, update the textblock
            editText.TextChanged += (s, e) =>
            {
                textBlock.Text = "My name is: " + editText.Text;
            };

            // When the button is clicked, we execute a method that clears the textbox
            var button = page.RootElement.FindVisualChildOfType<Button>("MyButton");
            button.Click += ButtonClicked;
        }

        private void ButtonClicked(object sender, RoutedEventArgs e)
        {
            // Chaning the text triggers the TextChanged event again
            editText.Text = "";
            
            // We also want to reset the text in the textblock
            textBlock.Text = "...";
        }
    }
}
