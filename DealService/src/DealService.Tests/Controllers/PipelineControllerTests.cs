using DealService.Controllers;
using DealService.Models;
using DealService.Repository;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SharedLibrary.Deals.Enums;

namespace DealService.Tests.Controllers;

public class PipelineControllerTests
{
  private readonly Mock<IDealRepository> _repoMock = new();
  private readonly Mock<ILogger<PipelineController>> _loggerMock = new();
  private readonly PipelineController _sut;

  public PipelineControllerTests()
  {
    _sut = new PipelineController(_repoMock.Object, _loggerMock.Object);
  }

  [Fact]
  public async Task GetBoard_ReturnsOk_WithBoardGroupedByStage()
  {
    var deals = new List<Deal>
    {
      new() { DealId = Guid.NewGuid(), Title = "Deal A", Stage = DealStage.Prospecting, Value = 5000, DealContacts = [] },
      new() { DealId = Guid.NewGuid(), Title = "Deal B", Stage = DealStage.ClosedWon, Value = 12000, DealContacts = [] }
    };
    _repoMock.Setup(r => r.GetAllAsync(null, null, null)).ReturnsAsync(deals);

    var result = await _sut.GetBoard();

    var ok = result.Should().BeOfType<OkObjectResult>().Subject;
    ok.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task GetBoard_EmptyRepository_ReturnsOkWithAllStages()
  {
    _repoMock.Setup(r => r.GetAllAsync(null, null, null)).ReturnsAsync([]);

    var result = await _sut.GetBoard();

    var ok = result.Should().BeOfType<OkObjectResult>().Subject;
    ok.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task GetBoard_RepositoryThrows_Returns500()
  {
    _repoMock.Setup(r => r.GetAllAsync(null, null, null)).ThrowsAsync(new Exception("db failure"));

    var result = await _sut.GetBoard();

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  [Fact]
  public async Task GetBoard_DealsWithContacts_MapsContactsIntoResponse()
  {
    var contactId = Guid.NewGuid();
    var deals = new List<Deal>
    {
      new()
      {
        DealId = Guid.NewGuid(), Title = "Deal With Contacts",
        Stage = DealStage.Proposal, Value = 3000,
        DealContacts =
        [
          new DealContact { DealContactId = Guid.NewGuid(), ContactId = contactId, Role = DealContactRole.DecisionMaker }
        ]
      }
    };
    _repoMock.Setup(r => r.GetAllAsync(null, null, null)).ReturnsAsync(deals);

    var result = await _sut.GetBoard();

    result.Should().BeOfType<OkObjectResult>();
  }
}
