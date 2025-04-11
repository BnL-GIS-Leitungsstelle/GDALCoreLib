using Serilog.Core;
using Serilog.Events;
using System.Windows.Controls;

namespace LayerComparer
{
    internal class WpfTextBoxSink : ILogEventSink
    {
        private readonly RichTextBox textBox;
        private readonly ScrollViewer scrollContainer;

        public WpfTextBoxSink(RichTextBox textBox, ScrollViewer scrollContainer)
        {
            this.textBox = textBox;
            this.scrollContainer = scrollContainer;
        }
        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage();
            textBox.Dispatcher.Invoke(() => textBox.AppendText(message + Environment.NewLine));
            scrollContainer.Dispatcher.Invoke(scrollContainer.ScrollToBottom);
        }
    }
}