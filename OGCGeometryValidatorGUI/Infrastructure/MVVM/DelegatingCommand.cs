using System;
using System.Windows.Input;

namespace OGCGeometryValidatorGUI.Infrastructure.MVVM
{
    /// <summary>
    /// Provides a command that delegates the execution of the command to a defined method. 
    /// </summary>
    public class DelegatingCommand : ICommand
    {
        /// <summary>
        /// Delegate for command execution 
        /// </summary>
        private readonly Action<object> _delegateExecute;

        /// <summary>
        /// Delegate for the status of the command execution 
        /// </summary>
        private readonly Predicate<object> _delegateCanExecute;

        /// <summary>
        /// Event for checking the status of the command execution. 
        /// </summary>
        /// <remarks>
        /// This event delegates registration of command execution control to the application's command manager. 
        /// </remarks>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Initializes a new instance of the DelegatingCommand class.
        /// </summary>
        /// <param name="delegateExecute">The action that will be executed by the command. Must not be null.</param>
        /// <param name="delegateCanExecute">A predicate that determines whether <paramref name="delegateExecute"/> can be executed.</param>
        /// <param name="name">Name of this command.</param>
        public DelegatingCommand(Action<object> delegateExecute, Predicate<object> delegateCanExecute = null)
        {
            _delegateExecute = delegateExecute ?? throw new ArgumentNullException("delegateExecute", "Der Delegat für die Aktionslogik muss definiert sein.");
            _delegateCanExecute = delegateCanExecute;
        }

        /// <summary>
        /// Defines the method used to determine whether the command in the current state can be executed.
        /// </summary>
        /// <returns>
        /// returns true if the command can be executed; otherwise, false. 
        /// </returns>
        /// <param name="parameter">Parameters to be passed to the delegate. If no data pass is required, it can be null.</param>
        public bool CanExecute(object parameter)
        {
            return _delegateCanExecute == null || _delegateCanExecute(parameter);
        }

        /// <summary>
        /// Execute the command. 
        /// </summary>
        /// <param name="parameter">Parameter for the command execution</param>
        public void Execute(object parameter)
        {
            _delegateExecute(parameter);
        }
    }
}

