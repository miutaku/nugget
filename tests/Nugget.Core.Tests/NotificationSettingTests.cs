using Nugget.Core.Entities;

namespace Nugget.Core.Tests;

public class NotificationSettingTests
{
    [Fact]
    public void NotificationSetting_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var setting = new NotificationSetting();

        // Assert
        Assert.Equal([3, 1, 0], setting.DaysBeforeDue);
        Assert.True(setting.SlackNotificationEnabled);
        Assert.Equal(9, setting.NotificationHour);
    }

    [Fact]
    public void NotificationSetting_ShouldSetCustomValues()
    {
        // Arrange & Act
        var setting = new NotificationSetting
        {
            DaysBeforeDue = [7, 3, 1, 0],
            SlackNotificationEnabled = false,
            NotificationHour = 10
        };

        // Assert
        Assert.Equal([7, 3, 1, 0], setting.DaysBeforeDue);
        Assert.False(setting.SlackNotificationEnabled);
        Assert.Equal(10, setting.NotificationHour);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    [InlineData(23)]
    public void NotificationSetting_ShouldSupportDifferentHours(int hour)
    {
        // Arrange & Act
        var setting = new NotificationSetting { NotificationHour = hour };

        // Assert
        Assert.Equal(hour, setting.NotificationHour);
    }
}
