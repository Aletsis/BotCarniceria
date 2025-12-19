using BotCarniceria.Core.Application.Interfaces.BackgroundJobs;
using BotCarniceria.Infrastructure.BackgroundJobs;
using BotCarniceria.Infrastructure.BackgroundJobs.Services;
using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using Moq;

namespace BotCarniceria.Infrastructure.Tests.BackgroundJobs.Services;

public class HangfireBackgroundJobServiceTests
{
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock;
    private readonly Mock<IRecurringJobManager> _recurringJobManagerMock;
    private readonly Mock<ILogger<HangfireBackgroundJobService>> _loggerMock;
    private readonly HangfireBackgroundJobService _sut;

    public HangfireBackgroundJobServiceTests()
    {
        _backgroundJobClientMock = new Mock<IBackgroundJobClient>();
        _recurringJobManagerMock = new Mock<IRecurringJobManager>();
        _loggerMock = new Mock<ILogger<HangfireBackgroundJobService>>();

        _sut = new HangfireBackgroundJobService(
            _backgroundJobClientMock.Object,
            _recurringJobManagerMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task EnqueueAsync_ShouldEnqueueJobAndReturnId()
    {
        // Arrange
        var job = new TestJob { Data = "Test" };
        var expectedJobId = "job-123";

        _backgroundJobClientMock
            .Setup(x => x.Create(
                It.IsAny<Job>(),
                It.IsAny<EnqueuedState>()))
            .Returns(expectedJobId);

        // Act
        var result = await _sut.EnqueueAsync(job);

        // Assert
        result.Should().Be(expectedJobId);
        _backgroundJobClientMock.Verify(x => x.Create(
            It.Is<Job>(j => j.Type == typeof(IJobHandler<TestJob>) && j.Method.Name == "ExecuteAsync"),
            It.IsAny<EnqueuedState>()), Times.Once);
    }

    [Fact]
    public async Task ScheduleAsync_ShouldScheduleJobAndReturnId()
    {
        // Arrange
        var job = new TestJob { Data = "Test" };
        var delay = TimeSpan.FromMinutes(5);
        var expectedJobId = "job-456";

        _backgroundJobClientMock
            .Setup(x => x.Create(
                It.IsAny<Job>(),
                It.IsAny<ScheduledState>()))
            .Returns(expectedJobId);

        // Act
        var result = await _sut.ScheduleAsync(job, delay);

        // Assert
        result.Should().Be(expectedJobId);
        _backgroundJobClientMock.Verify(x => x.Create(
            It.Is<Job>(j => j.Type == typeof(IJobHandler<TestJob>)),
            It.Is<ScheduledState>(s => s.EnqueueAt > DateTime.UtcNow)), Times.Once);
    }

    [Fact]
    public async Task AddRecurringJobAsync_ShouldAddRecurringJob()
    {
        // Arrange
        var job = new TestJob { Data = "Test" };
        var jobId = "recurring-1";
        var cron = "* * * * *";

        // Act
        await _sut.AddRecurringJobAsync(jobId, job, cron);

        // Assert
        _recurringJobManagerMock.Verify(x => x.AddOrUpdate(
            jobId,
            It.Is<Job>(j => j.Type == typeof(IJobHandler<TestJob>)),
            cron,
            It.IsAny<RecurringJobOptions>()), Times.Once);
    }

    [Fact]
    public async Task DeleteJobAsync_ShouldDeleteJob()
    {
        // Arrange
        var jobId = "job-to-delete";
        _backgroundJobClientMock
            .Setup(x => x.ChangeState(jobId, It.IsAny<DeletedState>(), It.IsAny<string>()))
            .Returns(true);

        // Act
        var result = await _sut.DeleteJobAsync(jobId);

        // Assert
        result.Should().BeTrue();
        _backgroundJobClientMock.Verify(x => x.ChangeState(
            jobId, 
            It.IsAny<DeletedState>(), 
            It.IsAny<string>()), Times.Once);
    }

    public class TestJob : IJob
    {
        public string Data { get; set; } = string.Empty;
        public string JobId => Guid.NewGuid().ToString();
        public int MaxRetries => 3;
        public int Priority => 0;
    }
}
