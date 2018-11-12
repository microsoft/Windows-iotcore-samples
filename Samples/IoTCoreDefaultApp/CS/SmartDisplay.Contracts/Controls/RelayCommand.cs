// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Windows.Input;

namespace SmartDisplay
{
    /// <summary>
    /// Defines a command that implements System.Windows.ICommand and uses delegates
    /// to handle Execute and CanExecute.
    /// </summary>
    public class RelayCommand : RelayCommand<object>
    {
        public RelayCommand(Action<object> execute)
            : base(execute, p => { return true; })
        {
        }

        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
            : base(execute, canExecute)
        {
        }
    }

    public class RelayCommand<T> : ICommand
    {
        #region Fields

        /// <summary>
        /// The delegate to invoke when executing the command.
        /// </summary>
        private readonly Action<T> _execute;

        /// <summary>
        /// The delegate to invoke to determine if the command can be executed.
        /// </summary>
        private readonly Predicate<T> _canExecute;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The delegate to invoke when the command executes.</param>
        /// <param name="canExecute">The delegate to invoke to determine if the command can execute.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return parameter is T;
            }
            else
            {
                if (parameter is T)
                {
                    return _canExecute((T)parameter);
                }
                else if (parameter == null)
                {
                    return _canExecute(default(T));
                }
            }
            return false;
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        #endregion

        public bool TryExecute(object parameter)
        {
            if (CanExecute(parameter))
            {
                Execute(parameter);
                return true;
            }
            return false;
        }
    }
}
