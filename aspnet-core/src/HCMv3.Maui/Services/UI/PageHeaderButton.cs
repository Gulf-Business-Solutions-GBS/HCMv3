namespace HCMv3.Maui.Services.UI;

public class PageHeaderButton
{
    public string Text { get; set; }

    public Func<Task> ClickAction { get; set; }

    public string Icon { get; set; }

    public PageHeaderButton(string text, string icon, Func<Task> clickAction)
    {
        Text = text;
        Icon = icon;
        ClickAction = clickAction;
    }
}