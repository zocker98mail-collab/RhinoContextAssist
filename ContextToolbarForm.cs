using Eto.Drawing;
using Eto.Forms;
using Rhino;
using Rhino.UI;

namespace RhinoContextAssist;

internal sealed class ContextToolbarForm : Form
{
    private readonly Label _caption = new();
    private readonly StackLayout _primary = new()
    {
        Orientation = Orientation.Horizontal,
        Spacing = 4
    };
    private readonly StackLayout _more = new()
    {
        Orientation = Orientation.Horizontal,
        Spacing = 4,
        Visible = false
    };

    private readonly Action<ContextAction> _execute;
    private readonly Action _dismiss;

    public ContextToolbarForm(
        Action<ContextAction> execute,
        Action dismiss)
    {
        _execute = execute;
        _dismiss = dismiss;

        Title = "Context Assist";
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        Resizable = false;
        WindowStyle = WindowStyle.None;
        Padding = 7;
        this.UseRhinoStyle();

        Content = new StackLayout
        {
            Spacing = 4,
            Items = { _caption, _primary, _more }
        };
    }

    private Button Make(
        ContextAction action,
        bool favorite,
        long usage)
    {
        var text = $"{action.Icon} {(favorite ? "★ " : "")}{action.Label}";
        var button = new Button
        {
            Text = text,
            ToolTip = $"{action.Hint}\nVerwendet: {usage}×",
            Height = 30
        };

        button.Click += (_, _) => _execute(action);
        return button;
    }

    public void UpdateActions(
        SelectionContext context,
        IReadOnlyList<ContextAction> actions,
        ContextAssistSettings settings)
    {
        _caption.Text = context.Caption;
        _primary.Items.Clear();
        _more.Items.Clear();
        _more.Visible = false;

        var primaryCount = Math.Min(settings.PrimaryCount, actions.Count);

        foreach (var action in actions.Take(primaryCount))
        {
            _primary.Items.Add(Make(
                action,
                settings.IsFavorite(context.Kind, action.Id),
                settings.UsageCount(action.Id)));
        }

        var remaining = actions.Skip(primaryCount).ToList();

        if (remaining.Count > 0)
        {
            var moreButton = new Button
            {
                Text = $"⋯ Mehr ({remaining.Count})",
                Height = 30
            };

            moreButton.Click += (_, _) =>
            {
                var expand = !_more.Visible;
                _more.Visible = expand;

                // Das Form wird bei dynamischen Inhalten nicht automatisch
                // neu bemessen. Nach dem Layout-Pass ClientSize anpassen.
                Application.Instance.AsyncInvoke(
                    () => ResizeForMore(expand));
            };

            _primary.Items.Add(moreButton);

            foreach (var action in remaining)
            {
                _more.Items.Add(Make(
                    action,
                    settings.IsFavorite(context.Kind, action.Id),
                    settings.UsageCount(action.Id)));
            }
        }

        var close = new Button
        {
            Text = "×",
            ToolTip = "Ausblenden",
            Width = 30,
            Height = 30
        };

        close.Click += (_, _) => _dismiss();
        _primary.Items.Add(close);
    }

    private void ResizeForMore(bool expanded)
    {
        if (!Visible)
            return;

        const int padding = 14;
        const int spacing = 4;

        var desiredWidth =
            Math.Max(_primary.Width, expanded ? _more.Width : 0) + padding;

        var desiredHeight =
            _caption.Height + _primary.Height + padding;

        if (expanded)
            desiredHeight += _more.Height + spacing;

        ClientSize = new Size(
            Math.Max(220, desiredWidth),
            Math.Max(58, desiredHeight));

        KeepInsideCurrentScreen();
        Invalidate();
    }

    private void KeepInsideCurrentScreen()
    {
        var center = new PointF(
            Location.X + ClientSize.Width / 2f,
            Location.Y + ClientSize.Height / 2f);

        var work = Screen.FromPoint(center).WorkingArea;
        var left = (int)work.Left;
        var top = (int)work.Top;
        var right = (int)work.Right;
        var bottom = (int)work.Bottom;

        Location = new Point(
            Math.Clamp(Location.X, left, Math.Max(left, right - Width)),
            Math.Clamp(Location.Y, top, Math.Max(top, bottom - Height)));
    }

    public void ShowAt(
        SelectionContext context,
        PopupPositionMode mode)
    {
        PointF position = Mouse.Position;

        if (mode == PopupPositionMode.Geometry && context.Anchor.IsValid)
        {
            var view = RhinoDoc.ActiveDoc?.Views.ActiveView;
            if (view is not null)
            {
                var client = view.ActiveViewport.WorldToClient(context.Anchor);
                var screen = view.ScreenRectangle;
                position = new PointF(
                    (float)(screen.Left + client.X),
                    (float)(screen.Top + client.Y));
            }
        }

        Location = new Point(
            (int)position.X + 14,
            (int)position.Y + 18);

        Show();
        KeepInsideCurrentScreen();
    }
}
