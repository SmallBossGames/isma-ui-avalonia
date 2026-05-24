global using global::Xunit;
using System.Text.Json;
using FluentAssertions;
using ISMA.Domain.Models;

namespace ISMA.Tests.Domain;

public class PreferencesTests
{
    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesAllValues()
    {
        var original = new Preferences
        {
            WindowPreferences = new WindowPreferences
            {
                X = 100.5,
                Y = 200.75,
                Width = 800,
                Height = 600,
                IsMaximized = true
            },
            DefaultFilesPreferences = new DefaultFilesPreferences
            {
                LastOpenedProjectPath = new[] { "/path/to/project1.isma", "/path/to/project2.isma", "/path/to/project3.isma" }
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
        var json = JsonSerializer.Serialize(original, options);
        var deserialized = JsonSerializer.Deserialize<Preferences>(json, options);

        deserialized.Should().NotBeNull();
        deserialized!.WindowPreferences.X.Should().Be(100.5);
        deserialized.WindowPreferences.Y.Should().Be(200.75);
        deserialized.WindowPreferences.Width.Should().Be(800);
        deserialized.WindowPreferences.Height.Should().Be(600);
        deserialized.WindowPreferences.IsMaximized.Should().BeTrue();
        deserialized.DefaultFilesPreferences.LastOpenedProjectPath.Should().HaveCount(3);
        deserialized.DefaultFilesPreferences.LastOpenedProjectPath[0].Should().Be("/path/to/project1.isma");
        deserialized.DefaultFilesPreferences.LastOpenedProjectPath[1].Should().Be("/path/to/project2.isma");
        deserialized.DefaultFilesPreferences.LastOpenedProjectPath[2].Should().Be("/path/to/project3.isma");
    }

    [Fact]
    public void WindowGeometry_SaveAndRestore_PreservesPositionAndSize()
    {
        var preferences = new Preferences();

        preferences.WindowPreferences = new WindowPreferences
        {
            X = 150,
            Y = 250,
            Width = 1024,
            Height = 768,
            IsMaximized = false
        };

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var json = JsonSerializer.Serialize(preferences, options);
        var restored = JsonSerializer.Deserialize<Preferences>(json, options);

        restored!.WindowPreferences.X.Should().Be(150);
        restored.WindowPreferences.Y.Should().Be(250);
        restored.WindowPreferences.Width.Should().Be(1024);
        restored.WindowPreferences.Height.Should().Be(768);
        restored.WindowPreferences.IsMaximized.Should().BeFalse();
    }

    [Fact]
    public void WindowGeometry_MaximizedState_PersistsCorrectly()
    {
        var preferences = new Preferences
        {
            WindowPreferences = new WindowPreferences
            {
                X = 0,
                Y = 0,
                Width = 1920,
                Height = 1080,
                IsMaximized = true
            }
        };

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var json = JsonSerializer.Serialize(preferences, options);
        var restored = JsonSerializer.Deserialize<Preferences>(json, options);

        restored!.WindowPreferences.IsMaximized.Should().BeTrue();
    }

    [Fact]
    public void LastOpenedFiles_SaveAndRestore_PreservesFilePaths()
    {
        var preferences = new Preferences
        {
            DefaultFilesPreferences = new DefaultFilesPreferences
            {
                LastOpenedProjectPath = new[]
                {
                    "/home/user/projects/isma/project1.isma",
                    "/home/user/projects/isma/project2.isma"
                }
            }
        };

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var json = JsonSerializer.Serialize(preferences, options);
        var restored = JsonSerializer.Deserialize<Preferences>(json, options);

        restored!.DefaultFilesPreferences.LastOpenedProjectPath.Should().HaveCount(2);
        restored.DefaultFilesPreferences.LastOpenedProjectPath[0].Should().Be("/home/user/projects/isma/project1.isma");
        restored.DefaultFilesPreferences.LastOpenedProjectPath[1].Should().Be("/home/user/projects/isma/project2.isma");
    }

    [Fact]
    public void LastOpenedFiles_EmptyArray_SerializesCorrectly()
    {
        var preferences = new Preferences
        {
            DefaultFilesPreferences = new DefaultFilesPreferences
            {
                LastOpenedProjectPath = Array.Empty<string>()
            }
        };

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var json = JsonSerializer.Serialize(preferences, options);
        var restored = JsonSerializer.Deserialize<Preferences>(json, options);

        restored!.DefaultFilesPreferences.LastOpenedProjectPath.Should().BeEmpty();
    }

    [Fact]
    public void DefaultValues_UsesEmptyArrayForLastOpenedFiles()
    {
        var preferences = new Preferences();
        preferences.DefaultFilesPreferences.LastOpenedProjectPath.Should().BeEmpty();
    }

    [Fact]
    public void DefaultValues_WindowPreferences_UsesZeroValues()
    {
        var preferences = new Preferences();
        preferences.WindowPreferences.X.Should().Be(0);
        preferences.WindowPreferences.Y.Should().Be(0);
        preferences.WindowPreferences.Width.Should().Be(0);
        preferences.WindowPreferences.Height.Should().Be(0);
        preferences.WindowPreferences.IsMaximized.Should().BeFalse();
    }

    [Fact]
    public void SerializeDeserialize_WithMixedValues_PreservesAll()
    {
        var original = new Preferences
        {
            WindowPreferences = new WindowPreferences
            {
                X = 50,
                Y = 100,
                Width = 1200,
                Height = 900,
                IsMaximized = false
            },
            DefaultFilesPreferences = new DefaultFilesPreferences
            {
                LastOpenedProjectPath = new[] { "/tmp/test.isma" }
            }
        };

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var json = JsonSerializer.Serialize(original, options);
        var restored = JsonSerializer.Deserialize<Preferences>(json, options);

        restored.Should().NotBeNull();
        restored!.WindowPreferences.X.Should().Be(50);
        restored.WindowPreferences.Y.Should().Be(100);
        restored.WindowPreferences.Width.Should().Be(1200);
        restored.WindowPreferences.Height.Should().Be(900);
        restored.WindowPreferences.IsMaximized.Should().BeFalse();
        restored.DefaultFilesPreferences.LastOpenedProjectPath.Should().ContainSingle()
            .Which.Should().Be("/tmp/test.isma");
    }
}
