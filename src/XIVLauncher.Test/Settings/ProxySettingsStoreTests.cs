using System.IO;
using System.Linq;
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

        Assert.Empty(settings.Profiles);
        Assert.Null(settings.ActiveProfileId);
        Assert.Null(settings.ToSnapshot());
    }

    [Fact]
    public void SaveLoad_RoundTrip_PreservesProfilesAndActive()
    {
        var path = CreateTempPath();

        try
        {
            var profileA = new ProxyProfile
            {
                Name         = "A",
                ProxyType    = ProxyType.Socks5,
                ProxyHost    = "127.0.0.1",
                ProxyPort    = 1080,
                ProxyUsername = "user"
            };
            profileA.SetPassword("secret");

            var profileB = new ProxyProfile
            {
                Name      = "B",
                ProxyType = ProxyType.Http,
                ProxyHost = "proxy.example.com",
                ProxyPort = 8080
            };

            var settings = new ProxySettings
            {
                Profiles        = [profileA, profileB],
                ActiveProfileId = profileA.Id
            };

            ProxySettingsStore.Save(path, settings);

            var loaded = ProxySettingsStore.Load(path);

            Assert.Equal(2, loaded.Profiles.Count);
            Assert.Equal(profileA.Id, loaded.ActiveProfileId);

            var loadedA = loaded.GetActiveProfile();
            Assert.NotNull(loadedA);
            Assert.Equal(ProxyType.Socks5, loadedA.ProxyType);
            Assert.Equal("127.0.0.1", loadedA.ProxyHost);
            Assert.Equal(1080, loadedA.ProxyPort);
            Assert.Equal("user", loadedA.ProxyUsername);
            Assert.Equal("secret", loadedA.GetPassword());

            Assert.NotNull(loaded.ToSnapshot());
        }
        finally
        {
            File.Delete(path);
            File.Delete(path + ".bak");
        }
    }

    [Fact]
    public void ToSnapshot_NoActiveProfile_ReturnsNull()
    {
        var settings = new ProxySettings
        {
            Profiles =
            [
                new ProxyProfile
                {
                    Name      = "未启用",
                    ProxyType = ProxyType.Socks5,
                    ProxyHost = "127.0.0.1",
                    ProxyPort = 1080
                }
            ],
            ActiveProfileId = null
        };

        Assert.Null(settings.ToSnapshot());
    }

    [Fact]
    public void ToSnapshot_ActiveProfile_MapsFields()
    {
        var profile = new ProxyProfile
        {
            Name         = "A",
            ProxyType    = ProxyType.Socks5,
            ProxyHost    = "127.0.0.1",
            ProxyPort    = 1080,
            ProxyUsername = "user"
        };
        profile.SetPassword("pass");

        var settings = new ProxySettings
        {
            Profiles        = [profile],
            ActiveProfileId = profile.Id
        };

        var snapshot = settings.ToSnapshot();

        Assert.NotNull(snapshot);
        Assert.Equal("Socks5", snapshot.Type);
        Assert.Equal("127.0.0.1", snapshot.Host);
        Assert.Equal(1080, snapshot.Port);
        Assert.Equal("user", snapshot.Username);
        Assert.Equal("pass", snapshot.Password);
    }

    [Fact]
    public void Load_CorruptedFile_IsolatesAndReturnsDefault()
    {
        var path = CreateTempPath();

        try
        {
            File.WriteAllText(path, "{ not valid json !!");

            var settings = ProxySettingsStore.Load(path);

            Assert.Empty(settings.Profiles);
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
    public void Load_LegacyFlatFormat_MigratesToSingleProfile()
    {
        var path = CreateTempPath();

        try
        {
            File.WriteAllText
            (
                path,
                """
                {
                  "ProxyType": 3,
                  "ProxyHost": "127.0.0.1",
                  "ProxyPort": 1080,
                  "ProxyUsername": "user",
                  "ProxyPasswordEncrypted": ""
                }
                """
            );

            var settings = ProxySettingsStore.Load(path);

            var profile = Assert.Single(settings.Profiles);
            Assert.Equal(ProxyType.Socks5, profile.ProxyType);
            Assert.Equal("127.0.0.1", profile.ProxyHost);
            Assert.Equal(1080, profile.ProxyPort);
            Assert.Equal(profile.Id, settings.ActiveProfileId);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SetPassword_Empty_ClearsPassword()
    {
        var profile = new ProxyProfile();
        profile.SetPassword("secret");

        Assert.Equal("secret", profile.GetPassword());
        Assert.NotEmpty(profile.ProxyPasswordEncrypted);

        profile.SetPassword(null);

        Assert.Null(profile.GetPassword());
        Assert.Empty(profile.ProxyPasswordEncrypted);
    }

    [Fact]
    public void ToSnapshot_InvalidPort_ReturnsNull()
    {
        var profile = new ProxyProfile { ProxyType = ProxyType.Http, ProxyHost = "127.0.0.1", ProxyPort = 70000 };

        Assert.Null(profile.ToSnapshot());
    }
}
