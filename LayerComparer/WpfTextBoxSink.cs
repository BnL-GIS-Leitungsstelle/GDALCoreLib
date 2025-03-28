using Serilog.Core;
using Serilog.Events;
using System.Windows.Controls;

namespace LayerComparer
{
    internal class WpfTextBoxSink : ILogEventSink
    {
        private readonly RichTextBox textBox;

        public WpfTextBoxSink(RichTextBox textBox)
        {
            this.textBox = textBox;
        }
        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage();
            textBox.AppendText(message + Environment.NewLine);
        }
    }
}