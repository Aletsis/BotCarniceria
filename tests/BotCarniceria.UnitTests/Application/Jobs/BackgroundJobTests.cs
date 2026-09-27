using BotCarniceria.Core.Application.DTOs.WhatsApp;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs.Jobs;
using FluentAssertions;
using Xunit;

namespace BotCarniceria.UnitTests.Application.Jobs;

public class BackgroundJobTests
{
    #region ProcessIncomingMessageJob Tests

    [Fact]
    public void ProcessIncomingMessageJob_WhenMessageHasId_JobIdReturnsMessageId()
    {
        // Arrange
        var message = new WhatsAppMessage
        {
            Id = "wamid.HBgLMTIzNDU2Nzg5MA==",
            From = "5551234567"
        };
        var job = new ProcessIncomingMessageJob
        {
            Message = message
        };

        // Act & Assert
        job.JobId.Should().Be("wamid.HBgLMTIzNDU2Nzg5MA==");
        job.Message.Should().BeSameAs(message);
    }

    [Fact]
    public void ProcessIncomingMessageJob_WhenMessageIsNull_JobIdGeneratesNewGuid()
    {
        // Arrange
        var job = new ProcessIncomingMessageJob
        {
            Message = null!
        };

        // Act & Assert
        job.JobId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(job.JobId, out _).Should().BeTrue();
    }

    [Fact]
    public void ProcessIncomingMessageJob_WhenMessageIdIsNull_JobIdGeneratesNewGuid()
    {
        // Arrange
        var job = new ProcessIncomingMessageJob
        {
            Message = new WhatsAppMessage { Id = null }
        };

        // Act & Assert
        job.JobId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(job.JobId, out _).Should().BeTrue();
    }

    [Fact]
    public void ProcessIncomingMessageJob_DefaultProperties_HaveExpectedValues()
    {
        // Arrange
        var job = new ProcessIncomingMessageJob();

        // Act & Assert
        job.MaxRetries.Should().Be(3);
        job.Priority.Should().Be(0);
    }

    #endregion

    #region EnqueueWhatsAppMessageJob Tests

    [Fact]
    public void EnqueueWhatsAppMessageJob_WithRequiredProperties_HasDefaultValues()
    {
        // Arrange & Act
        var job = new EnqueueWhatsAppMessageJob
        {
            PhoneNumber = "5551234567",
            Message = "Hola mundo"
        };

        // Assert
        job.PhoneNumber.Should().Be("5551234567");
        job.Message.Should().Be("Hola mundo");
        job.MessageType.Should().Be("text");
        job.MaxRetries.Should().Be(5);
        job.Priority.Should().Be(1);
        job.JobId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(job.JobId, out _).Should().BeTrue();
        job.AdditionalData.Should().BeNull();
    }

    [Fact]
    public void EnqueueWhatsAppMessageJob_WithCustomProperties_PreservesAllValues()
    {
        // Arrange
        var customData = new Dictionary<string, object>
        {
            { "template_id", "order_ready" },
            { "order_id", 42 }
        };

        // Act
        var job = new EnqueueWhatsAppMessageJob
        {
            JobId = "custom-job-id-999",
            PhoneNumber = "5559876543",
            Message = "Su pedido está listo",
            MessageType = "template",
            MaxRetries = 10,
            Priority = 2,
            AdditionalData = customData
        };

        // Assert
        job.JobId.Should().Be("custom-job-id-999");
        job.PhoneNumber.Should().Be("5559876543");
        job.Message.Should().Be("Su pedido está listo");
        job.MessageType.Should().Be("template");
        job.MaxRetries.Should().Be(10);
        job.Priority.Should().Be(2);
        job.AdditionalData.Should().BeSameAs(customData);
        job.AdditionalData.Should().ContainKey("order_id");
    }

    [Fact]
    public void EnqueueWhatsAppMessageJob_RecordEquality_ReturnsTrueForEqualValues()
    {
        // Arrange
        var job1 = new EnqueueWhatsAppMessageJob
        {
            JobId = "same-id",
            PhoneNumber = "5551112222",
            Message = "Mensaje idéntico",
            MaxRetries = 3,
            Priority = 1
        };

        var job2 = new EnqueueWhatsAppMessageJob
        {
            JobId = "same-id",
            PhoneNumber = "5551112222",
            Message = "Mensaje idéntico",
            MaxRetries = 3,
            Priority = 1
        };

        // Act & Assert
        job1.Should().Be(job2);
        (job1 == job2).Should().BeTrue();
    }

    #endregion
}
