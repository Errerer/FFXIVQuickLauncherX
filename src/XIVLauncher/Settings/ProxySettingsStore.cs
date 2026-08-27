using System.IO;
using System.Text;
using System.Text.Json;
using Serilog;

namespace XIVLauncher.Settings;

/// <summary>
///     代理配置独立存储: 启动器目录下的 proxyConfigV3.json
/// </summary>
public static class ProxySettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented               = true,
        PropertyNameCaseInsensitive = true
    };

    public static ProxySettings Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new ProxySettings();

        try
        {
            var json     = File.ReadAllText(path, Encoding.UTF8);
            var settings = JsonSerializer.Deserialize<ProxySettings>(json, JsonOptions) ?? new ProxySettings();

            if (!string.IsNullOrWhiteSpace(settings.ProxyPasswordEncrypted) && settings.GetPassword() == null)
            {
                Log.Warning("[ProxySettingsStore] 代理密码解密失败, 已清空密码字段");
                settings.ProxyPasswordEncrypted = string.Empty;
            }

            return settings;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[ProxySettingsStore] 读取代理配置失败: {Path}", path);
            IsolateBrokenFile(path);
            return new ProxySettings();
        }
    }

    public static void Save(string path, ProxySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("代理配置路径不能为空", nameof(path));

        var directoryPath = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directoryPath))
            Directory.CreateDirectory(directoryPath);

        var tempPath   = path + ".tmp";
        var backupPath = path + ".bak";
        var json       = JsonSerializer.Serialize(settings, JsonOptions);

        try
        {
            File.WriteAllText(tempPath, json, Encoding.UTF8);

            if (File.Exists(path))
                File.Replace(tempPath, path, backupPath, true);
            else
                File.Move(tempPath, path);
        }
        catch
        {
            TryDeleteTempFile(tempPath);
            throw;
        }
    }

    private static void IsolateBrokenFile(string path)
    {
        try
        {
            var brokenPath = $"{path}.broken-{DateTime.Now:yyyyMMddHHmmssfff}";
            File.Move(path, brokenPath);
            Log.Warning("[ProxySettingsStore] 已隔离损坏的代理配置文件: {BrokenPath}", brokenPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[ProxySettingsStore] 隔离损坏的代理配置文件失败: {Path}", path);
        }
    }

    private static void TryDeleteTempFile(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
        catch
        {
            // ignored
        }
    }
}
