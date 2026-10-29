using System.Windows;
using System.Windows.Input;

namespace RhinoContextAssist;

public partial class CommandSearchWindow : Window
{
    private readonly List<ContextAction> _allActions;
    private readonly Action<ContextAction> _execute;

    // Internal, weil ContextAction ebenfalls ein interner Projekttyp ist.
    // Die Klasse selbst bleibt public, da sie durch WPF-XAML erzeugt wird.
    internal CommandSearchWindow(
        IEnumerable<ContextAction> actions,
        Action<ContextAction> execute)
    {
        InitializeComponent();

        _allActions = actions.ToList();
        _execute = execute;

        RefreshResults();
        Query.Focus();
    }

    private void Query_TextChanged(
        object sender,
        System.Windows.Controls.TextChangedEventArgs e)
    {
        RefreshResults();
    }

    private void RefreshResults()
    {
        string query = Query.Text?.Trim() ?? string.Empty;

        IEnumerable<ContextAction> matches = _allActions.Where(action =>
        {
            if (string.IsNullOrEmpty(query))
                return true;

            string searchableText =
                $"{action.Label} " +
                $"{action.Hint} " +
                $"{action.Macro} " +
                $"{action.Category}";

            return searchableText.Contains(
                query,
                StringComparison.OrdinalIgnoreCase);
        });

        Results.ItemsSource = matches
            .Select(action => new SearchItem(
                action,
                $"{action.Icon} {action.Label}",
                action.Hint,
                GetEnglishCommand(action.Macro)))
            .ToList();
    }

    private void Results_DoubleClick(
        object sender,
        MouseButtonEventArgs e)
    {
        if (Results.SelectedItem is not SearchItem selectedItem)
            return;

        _execute(selectedItem.Action);
        Close();
    }

    private static string GetEnglishCommand(string macro)
    {
        if (string.IsNullOrWhiteSpace(macro))
            return string.Empty;

        string firstToken = macro
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

        return firstToken.TrimStart('_', '-', '!');
    }

    private sealed record SearchItem(
        ContextAction Action,
        string Title,
        string Description,
        string EnglishCommand);
}
