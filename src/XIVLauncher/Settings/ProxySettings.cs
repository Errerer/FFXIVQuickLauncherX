using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using XIVLauncher.Common.Http;

namespace XIVLauncher.Settings;

/// <summary>
///     启动器网络代理配置 (独立存储于启动器目录下的 proxyConfigV3.json)
/// </summary>
public sealed class ProxySettings
{
    private static readonly byte[] PasswordEntropy = "XIVLauncherCN.ProxyPassword"u8.ToArray();

    public ProxyType ProxyType { get; set; }

    public string ProxyHost { get; set; } = string.Empty;

    public int ProxyPort { get; set; }

    public string ProxyUsername { get; set; } = string.Empty;

    /// <summary>
    ///     DPAPI (CurrentUser) 加密后的密码, Base64;空表示无密码
    /// </summary>
    public string ProxyPasswordEncrypted { get; set; } = string.Empty;

    /// <summary>
    ///     明文密码, 仅驻留内存, 不序列化
    /// </summary>
    [JsonIgnore]
    public string? ProxyPasswordPlain { get; set; }

    public void SetPassword(string? plainPassword)
    {
        if (string.IsNullOrEmpty(plainPassword))
        {
            ProxyPasswordEncrypted = string.Empty;
            ProxyPasswordPlain     = null;
            return;
        }

        ProxyPasswordPlain     = plainPassword;
        ProxyPasswordEncrypted = EncryptPassword(plainPassword);
    }

    public string? GetPassword()
    {
        if (ProxyPasswordPlain != null)
            return ProxyPasswordPlain;

        if (string.IsNullOrWhiteSpace(ProxyPasswordEncrypted))
            return null;

        try
        {
            ProxyPasswordPlain = DecryptPassword(ProxyPasswordEncrypted);
            return ProxyPasswordPlain;
        }
        catch
        {
            ProxyPasswordEncrypted = string.Empty;
            return null;
        }
    }

    public ProxyConfigSnapshot? ToSnapshot()
    {
        if (ProxyType == ProxyType.None)
            return null;

        var host = ProxyHost.Trim();
        if (string.IsNullOrWhiteSpace(host) || ProxyPort is < 1 or > 65535)
            return null;

        var username = ProxyUsername.Trim();
        var password = GetPassword();

        return new ProxyConfigSnapshot
        (
            ProxyType.ToString(),
            host,
            ProxyPort,
            string.IsNullOrWhiteSpace(username) ? null : username,
            string.IsNullOrEmpty(password) ? null : password
        );
    }

    private static string EncryptPassword(string plainPassword) =>
        Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(plainPassword), PasswordEntropy, DataProtectionScope.CurrentUser));

    private static string DecryptPassword(string encryptedPassword) =>
        Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(encryptedPassword), PasswordEntropy, DataProtectionScope.CurrentUser));
}
