using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Nugget.Api.Controllers;
using Nugget.Api.Models.Scim;
using Nugget.Core.Entities;
using Nugget.Core.Interfaces;
using Xunit;

namespace Nugget.Api.Tests.Controllers;

public class ScimControllerTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IGroupRepository> _mockGroupRepo;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<ILogger<ScimController>> _mockLogger;
    private readonly ScimController _controller;

    public ScimControllerTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockGroupRepo = new Mock<IGroupRepository>();
        _mockConfig = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<ScimController>>();

        _mockConfig.Setup(c => c["Scim:ApiToken"]).Returns("test-token");

        _controller = new ScimController(
            _mockUserRepo.Object,
            _mockGroupRepo.Object,
            _mockConfig.Object,
            _mockLogger.Object);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task GetUsers_NoToken_ReturnsUnauthorized()
    {
        // Act
        var result = await _controller.GetUsers(null);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var error = Assert.IsType<ScimError>(unauthorizedResult.Value);
        Assert.Equal("401", error.Status);
    }

    [Fact]
    public async Task GetUsers_ValidToken_ReturnsUsers()
    {
        // Arrange
        _controller.Request.Headers["Authorization"] = "Bearer test-token";
        var users = new List<User>
        {
            new User { Id = Guid.NewGuid(), Name = "User 1", Email = "user1@example.com" },
            new User { Id = Guid.NewGuid(), Name = "User 2", Email = "user2@example.com" }
        };
        _mockUserRepo.Setup(r => r.GetAllActiveUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var result = await _controller.GetUsers(null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ScimListResponse<ScimUser>>(okResult.Value);
        Assert.Equal(2, response.TotalResults);
    }

    [Fact]
    public async Task CreateGroup_WithMembers_CallsUpdateMembers()
    {
        // Arrange
        _controller.Request.Headers["Authorization"] = "Bearer test-token";
        var memberId = Guid.NewGuid();
        var scimGroup = new ScimGroup
        {
            DisplayName = "Test Group",
            Members = new List<ScimMember>
            {
                new ScimMember { Value = memberId.ToString() }
            }
        };

        var createdGroup = new Group { Id = Guid.NewGuid(), DisplayName = "Test Group" };


        
        _mockGroupRepo.Setup(r => r.AddAsync(It.IsAny<Group>(), It.IsAny<CancellationToken>()))
            .Callback<Group, CancellationToken>((g, t) => g.Id = createdGroup.Id)
            .ReturnsAsync(createdGroup);

        _mockGroupRepo.Setup(r => r.UpdateMembersAsync(It.IsAny<Group>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
            
        _mockGroupRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdGroup);

        // Act
        var result = await _controller.CreateGroup(scimGroup);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        _mockGroupRepo.Verify(r => r.UpdateMembersAsync(
            It.Is<Group>(g => g.DisplayName == "Test Group"),
            It.Is<IEnumerable<Guid>>(ids => ids.Contains(memberId)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task UpdateGroup_UpdatesNameAndMembers()
    {
        // Arrange
        _controller.Request.Headers["Authorization"] = "Bearer test-token";
        var groupId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var existingGroup = new Group { Id = groupId, DisplayName = "Old Name" };
        
        var scimGroup = new ScimGroup 
        { 
            DisplayName = "New Name",
            Members = new List<ScimMember> { new ScimMember { Value = memberId.ToString() } }
        };

        _mockGroupRepo.Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGroup);

        // Act
        var result = await _controller.UpdateGroup(groupId.ToString(), scimGroup);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("New Name", existingGroup.DisplayName);
        
        _mockGroupRepo.Verify(r => r.UpdateMembersAsync(
            existingGroup,
            It.Is<IEnumerable<Guid>>(ids => ids.Single() == memberId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
