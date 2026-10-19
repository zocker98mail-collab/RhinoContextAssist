using Eto.Drawing;
using Eto.Forms;
using Rhino.UI;

namespace RhinoContextAssist;

internal sealed class ContextToolbarForm : Form
{
    private readonly Label _caption;
    private readonly StackLayout _buttons;
    private readonly Action<ContextAction> _execute;
    private readonly Action _dismiss;

    public ContextToolbarForm(Action<ContextAction> execute, Action dismiss)
    {
        _execute = execute;
        _dismiss = dismiss;
        Title = "Context Assist";
        ShowInTaskbar = false;
        Topmost = true;
        Resizable = false;
        WindowStyle = WindowStyle.None;
        Padding = 7;
        this.UseRhinoStyle();

        _caption = new Label();
        _buttons = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 4 };
        Content = new StackLayout { Spacing = 4, Items = { _caption, _buttons } };
    }

    public void UpdateActions(SelectionContext context, IReadOnlyList<ContextAction> actions)
    {
        _caption.Text = context.Caption;
        _buttons.Items.Clear();
        foreach (var action in actions)
        {
            var button = new Button { Text = action.Label, ToolTip = action.Hint, Height = 28 };
            button.Click += (_, _) => _execute(action);
            _buttons.Items.Add(button);
        }
        var close = new Button { Text = "×", ToolTip = "Ausblenden", Width = 30, Height = 28 };
        close.Click += (_, _) => _dismiss();
        _buttons.Items.Add(close);
    }

    public void ShowSafelyNearMouse()
    {
        var mouse = Mouse.Position;
        Location = new Point((int)mouse.X + 14, (int)mouse.Y + 18);
        if (!Visible) Show();

        // Nach dem Anzeigen ist die tatsächliche Fenstergröße bekannt.
        var screen = Screen.FromPoint(mouse);
        var work = screen.WorkingArea;
        var left = (int)work.Left;
        var top = (int)work.Top;
        var right = (int)work.Right;
        var bottom = (int)work.Bottom;
        var x = Location.X;
        var y = Location.Y;
        if (x + Width > right) x = Math.Max(left, right - Width);
        if (y + Height > bottom) y = Math.Max(top, bottom - Height);
        if (x < left) x = left;
        if (y < top) y = top;
        Location = new Point(x, y);
    }
}
