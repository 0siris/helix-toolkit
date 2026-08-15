using System.Windows.Input;

namespace CoreWpfTest;

public class DelegateCommand : ICommand {
    private Action? execute;

    public void Dispose() {
        execute = null;
        canExecute = null;
    }

    private Func<bool>? canExecute;

    public DelegateCommand(Action execute)
        : this(execute, null) { }

    public DelegateCommand(Action execute, Func<bool>? canExecute) {
        ArgumentNullException.ThrowIfNull(execute);

        this.execute = execute;
        this.canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged {
        add {
            if (canExecute != null) {
                CommandManager.RequerySuggested += value;
            }
        }

        remove {
            if (canExecute != null) {
                CommandManager.RequerySuggested -= value;
            }
        }
    }

    public void RaiseCanExecuteChanged() {
        CommandManager.InvalidateRequerySuggested();
    }

    public bool CanExecute(object? parameter) => canExecute == null || canExecute();

    public void Execute(object? parameter) {
        execute?.Invoke();
    }
}
