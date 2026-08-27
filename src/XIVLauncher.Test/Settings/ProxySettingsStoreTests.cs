using System.IO;
using XIVLauncher.Settings;
using Xunit;

namespace XIVLauncher.Test.Settings;

public sealed class ProxySettingsStoreTests
{
    private static string CreateTempPath() =>
        Path.Combine(Path.GetTempPath(), $"xltest-proxy-{Guid.NewGuid():N}.json");

    [Fact]
    public void Load_MissingFile_ReturnsDefault()
    {
        var settings = ProxySettingsStore.Load(CreateTempPath());

        Assert.Equal(ProxyType.None, settings.ProxyType);
        Assert.Empty(settings.ProxyHost);
        Assert.Equal(0, settings.ProxyPort);
    }

    [Fact]
    public void SaveLoad_RoundTrip_PreservesFields()
    {
        var path = CreateTempPath();

        try
        {
            var settings = new ProxySettings
            {
                ProxyType     = ProxyType.Socks5,
                ProxyHost     = "127.0.0.1",
                ProxyPort     = 1080,
                ProxyUsername = "user"
            };
            settings.SetPassword("secret");

            ProxySettingsStore.Save(path, settings);

            var loaded = ProxySettingsStore.Load(path);

            Assert.Equal(ProxyType.Socks5, loaded.ProxyType);
            Assert.Equal("127.0.0.1", loaded.ProxyHost);
            Assert.Equal(1080, loaded.ProxyPort);
            Assert.Equal("user", loaded.ProxyUsername);
            Assert.Equal("secret", loaded.GetPassword());
        }
        finally
        {
            File.Delete(path);
            File.Delete(path + ".bak");
        }
    }

    [Fact]
    public void Load_CorruptedFile_IsolatesAndReturnsDefault()
    {
        var path = CreateTempPath();

        try
        {
            File.WriteAllText(path, "{ not valid json !!");

            var settings = ProxySettingsStore.Load(path);

            Assert.Equal(ProxyType.None, settings.ProxyType);
            Assert.False(File.Exists(path));
            Assert.Contains(Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(path) + ".broken-*"), file => true);
        }
        finally
        {
            foreach (var leftover in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(path) + "*"))
                File.Delete(leftover);
        }
    }

    [Fact]
    public void SetPassword_Empty_ClearsPassword()
    {
        var settings = new ProxySettings();
        settings.SetPassword("secret");

        Assert.Equal("secret", settings.GetPassword());
        Assert.NotEmpty(settings.ProxyPasswordEncrypted);

        settings.SetPassword(null);

        Assert.Null(settings.GetPassword());
        Assert.Empty(settings.ProxyPasswordEncrypted);
    }

    [Fact]
    public void ToSnapshot_None_ReturnsNull()
    {
        var settings = new ProxySettings { ProxyType = ProxyType.None, ProxyHost = "127.0.0.1", ProxyPort = 1080 };

        Assert.Null(settings.ToSnapshot());
    }

    [Fact]
    public void ToSnapshot_Socks5WithPassword_MapsFields()
    {
        var settings = new ProxySettings
        {
            ProxyType     = ProxyType.Socks5,
            ProxyHost     = "127.0.0.1",
            ProxyPort     = 1080,
            ProxyUsername = "user"
        };
        settings.SetPassword("pass");

        var snapshot = settings.ToSnapshot();

        Assert.NotNull(snapshot);
        Assert.Equal("Socks5", snapshot.Type);
        Assert.Equal("127.0.0.1", snapshot.Host);
        Assert.Equal(1080, snapshot.Port);
        Assert.Equal("user", snapshot.Username);
        Assert.Equal("pass", snapshot.Password);
    }

    [Fact]
    public void ToSnapshot_InvalidPort_ReturnsNull()
    {
        var settings = new ProxySettings { ProxyType = ProxyType.Http, ProxyHost = "127.0.0.1", ProxyPort = 70000 };

        Assert.Null(settings.ToSnapshot());
    }
}
