using Eto.Drawing;
using Eto.Forms;
using Rhino;
using Rhino.UI;

namespace RhinoContextAssist;

internal sealed class ContextToolbarForm : Form
{
    public ContextToolbarForm(SelectionContext context, IReadOnlyList<ContextAction> actions, Action<ContextAction> execute)
    {
        Title = "Context Assist";
        ShowInTaskbar = false;
        Topmost = true;
        Resizable = false;
        WindowStyle = WindowStyle.None;
        Padding = 7;
        this.UseRhinoStyle();

        var buttons = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 4 };
        foreach (var action in actions)
        {
            var button = new Button { Text = action.Label, ToolTip = action.Hint, Height = 28 };
            button.Click += (_, _) => execute(action);
            buttons.Items.Add(button);
        }

        var close = new Button { Text = "x", ToolTip = "Ausblenden", Width = 30, Height = 28 };
        close.Click += (_, _) => Close();
        buttons.Items.Add(close);

        Content = new StackLayout
        {
            Spacing = 4,
            Items =
            {
                new Label { Text = context.Caption, Font = new Font(SystemFont.Default, 9, FontStyle.Bold) },
                buttons
            }
        };
    }

    public void ShowNearMouse(RhinoDoc doc)
    {
        var p = Mouse.Position;
        Location = new Point((int)p.X + 14, (int)p.Y + 18);
        Show(doc);
    }
}
