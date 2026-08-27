using System.Windows;
using Serilog;
using XIVLauncher.Common.Constant;
using XIVLauncher.Settings;
using XIVLauncher.Windows.ViewModel;

namespace XIVLauncher.Windows;

public partial class ProxySettingsWindow
{
    private ProxySettingsWindowViewModel ViewModel =>
        (ProxySettingsWindowViewModel)DataContext;

    public ProxySettingsWindow()
    {
        InitializeComponent();

        var settings  = ProxySettingsStore.Load(Paths.GetProxyConfigPath());
        DataContext   = new ProxySettingsWindowViewModel(settings);
    }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel.Save(ProxyPasswordBox.Password);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存代理配置失败");
            CustomMessageBox.Show
            (
                $"保存代理配置失败：{ex.Message}",
                "代理配置",
                MessageBoxButton.OK,
                MessageBoxImage.Warning,
                parentWindow: this
            );
        }
    }
}
