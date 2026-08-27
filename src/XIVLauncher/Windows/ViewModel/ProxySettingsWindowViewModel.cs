using CommunityToolkit.Mvvm.ComponentModel;
using XIVLauncher.Common.Constant;
using XIVLauncher.Settings;

namespace XIVLauncher.Windows.ViewModel;

public sealed partial class ProxySettingsWindowViewModel : ObservableObject
{
    public sealed record ProxyTypeOption(ProxyType Type, string Display);

    public IReadOnlyList<ProxyTypeOption> ProxyTypeOptions { get; } =
    [
        new(ProxyType.None, "不使用代理"),
        new(ProxyType.Http, "HTTP"),
        new(ProxyType.Https, "HTTPS"),
        new(ProxyType.Socks5, "SOCKS5")
    ];

    [ObservableProperty]
    public partial ProxyTypeOption SelectedProxyTypeOption { get; set; }

    [ObservableProperty]
    public partial string ProxyHost { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int ProxyPort { get; set; } = 1080;

    [ObservableProperty]
    public partial string ProxyUsername { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PasswordHint))]
    public partial bool HasExistingPassword { get; set; }

    public string PasswordHint =>
        HasExistingPassword ? "留空保持当前密码" : "未设置";

    [ObservableProperty]
    public partial string ConfigFilePath { get; set; } = string.Empty;

    private readonly ProxySettings settings;

    public ProxySettingsWindowViewModel(ProxySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        this.settings     = settings;
        ConfigFilePath    = Paths.GetProxyConfigPath();
        SelectedProxyTypeOption = ProxyTypeOptions.First(option => option.Type == settings.ProxyType);
        ProxyHost         = settings.ProxyHost;
        ProxyPort         = settings.ProxyPort is >= 1 and <= 65535 ? settings.ProxyPort : 1080;
        ProxyUsername     = settings.ProxyUsername;
        HasExistingPassword = !string.IsNullOrWhiteSpace(settings.ProxyPasswordEncrypted);
    }

    public void Save(string? passwordInput)
    {
        var type = SelectedProxyTypeOption?.Type ?? ProxyType.None;

        if (type != ProxyType.None)
        {
            if (string.IsNullOrWhiteSpace(ProxyHost))
                throw new InvalidOperationException("代理主机不能为空。");

            if (ProxyPort is < 1 or > 65535)
                throw new InvalidOperationException("代理端口必须在 1 - 65535 之间。");
        }

        settings.ProxyType     = type;
        settings.ProxyHost     = type == ProxyType.None ? string.Empty : ProxyHost.Trim();
        settings.ProxyPort     = type == ProxyType.None ? 0 : ProxyPort;
        settings.ProxyUsername = ProxyUsername?.Trim() ?? string.Empty;

        if (!string.IsNullOrEmpty(passwordInput))
            settings.SetPassword(passwordInput);

        ProxySettingsStore.Save(Paths.GetProxyConfigPath(), settings);
    }
}
