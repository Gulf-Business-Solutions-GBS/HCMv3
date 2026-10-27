using Abp.Dependency;

namespace HCMv3.Maui.Services.UI;

public class HeaderButtonInfo
{
    public Func<Task> OnClick { get; }

    public string Text { get; }

    public string Icon { get; set; }

    public HeaderButtonInfo(string text, string icon, Func<Task> onClick)
    {
        Text = text;
        Icon = icon;
        OnClick = onClick;
    }
}

public class PageHeaderService : ISingletonDependency
{
    private string _title;

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            TitleChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler TitleChanged;

    private string _subTitle;

    public string SubTitle
    {
        get => _subTitle;
        set
        {
            _subTitle = value;
            SubTitleChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler SubTitleChanged;

    public List<HeaderButtonInfo> HeaderButtonInfos { get; private set; }

    public event EventHandler HeaderButtonChanged;

    public void SetButtons(List<PageHeaderButton> buttons)
    {
        HeaderButtonInfos = new List<HeaderButtonInfo>();
        foreach (var button in buttons)
        {
            HeaderButtonInfos.Add(new HeaderButtonInfo(button.Text, button.Icon, button.ClickAction));
        }

        HeaderButtonChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearButton()
    {
        HeaderButtonInfos = new List<HeaderButtonInfo>();
        HeaderButtonChanged?.Invoke(this, EventArgs.Empty);
    }
}