using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Admin.DTOs;
using WorkScholarship.Application.Features.Admin.Queries.GetDashboardState;
using WorkScholarship.Application.Features.Cycles.Commands.CloseApplications;
using WorkScholarship.Application.Features.Cycles.Commands.ConfigureCycle;
using WorkScholarship.Application.Features.Cycles.Commands.CreateCycle;
using WorkScholarship.Application.Features.Cycles.Commands.OpenApplications;
using WorkScholarship.Application.Features.Cycles.Commands.ReopenApplications;
using WorkScholarship.Application.Features.Cycles.DTOs;
using WorkScholarship.Application.Features.Cycles.Queries.GetActiveCycle;
using WorkScholarship.Application.Features.Cycles.Queries.GetCycleById;
using WorkScholarship.Application.Features.Cycles.Queries.ListCycles;
using WorkScholarship.Domain.Enums;
using WorkScholarship.WebAPI.Controllers;

namespace WorkScholarship.WebAPI.Tests.Controllers;

[Trait("Category", "WebAPI")]
[Trait("Component", "CyclesController")]
public class CyclesControllerTests
{
    private readonly ISender _sender;
    private readonly CyclesController _controller;
    private readonly AdminController _adminController;

    private static readonly DateTime _now = DateTime.UtcNow;

    public CyclesControllerTests()
    {
        _sender = Substitute.For<ISender>();
        _controller = new CyclesController(_sender);
        _adminController = new AdminController(_sender);

        SetupHttpContext(_controller);
        SetupHttpContext(_adminController);
    }

    private static void SetupHttpContext(ControllerBase controller)
    {
        var httpContext = new DefaultHttpContext
        {
            Request = { Scheme = "https", Host = new HostString("localhost", 7001) }
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private static CycleDto CreateCycleDto(
        Guid? id = null,
        string name = "2024-2",
        string department = "Biblioteca",
        CycleStatus status = CycleStatus.Configuration) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = name,
        Department = department,
        Status = status,
        StartDate = _now.AddDays(30),
        EndDate = _now.AddDays(180),
        ApplicationDeadline = _now.AddDays(40),
        InterviewDate = _now.AddDays(50),
        SelectionDate = _now.AddDays(60),
        TotalScholarshipsAvailable = 10,
        TotalScholarshipsAssigned = 0,
        RenewalProcessCompleted = false,
        CreatedAt = DateTime.UtcNow
    };

    private static CycleDetailDto CreateCycleDetailDto(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = "2024-2",
        Department = "Biblioteca",
        Status = CycleStatus.Configuration,
        StartDate = _now.AddDays(30),
        EndDate = _now.AddDays(180),
        ApplicationDeadline = _now.AddDays(40),
        InterviewDate = _now.AddDays(50),
        SelectionDate = _now.AddDays(60),
        TotalScholarshipsAvailable = 10,
        TotalScholarshipsAssigned = 0,
        RenewalProcessCompleted = false,
        CreatedAt = DateTime.UtcNow
    };

    // =====================================================================
    // POST /api/cycles
    // =====================================================================

    [Fact]
    public async Task Create_WithSuccessResult_Returns201Created()
    {
        // Arrange
        var cycleDto = CreateCycleDto();
        _sender.Send(Arg.Any<CreateCycleCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDto>.Success(cycleDto));

        var command = new CreateCycleCommand
        {
            Name = "2024-2",
            Department = "Biblioteca",
            StartDate = _now.AddDays(30),
            EndDate = _now.AddDays(180),
            ApplicationDeadline = _now.AddDays(40),
            InterviewDate = _now.AddDays(50),
            SelectionDate = _now.AddDays(60),
            TotalScholarshipsAvailable = 10
        };

        // Act
        var actionResult = await _controller.Create(command, CancellationToken.None);

        // Assert
        var createdResult = actionResult.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);

        var response = createdResult.Value.Should().BeOfType<ApiResponse<CycleDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_WithDuplicateCycleFailure_Returns400BadRequest()
    {
        // Arrange
        _sender.Send(Arg.Any<CreateCycleCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDto>.Failure(
                $"{CycleAppError.DUPLICATE_CYCLE}",
                "Ya existe un ciclo activo para este departamento."));

        var command = new CreateCycleCommand { Name = "2024-2", Department = "Biblioteca" };

        // Act
        var actionResult = await _controller.Create(command, CancellationToken.None);

        // Assert
        var badRequestResult = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    // =====================================================================
    // GET /api/cycles
    // =====================================================================

    [Fact]
    public async Task GetAll_WithSuccessResult_Returns200Ok()
    {
        // Arrange
        var pagedList = new PaginatedList<CycleListItemDto>([], 0, 1, 10);
        _sender.Send(Arg.Any<ListCyclesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaginatedList<CycleListItemDto>>.Success(pagedList));

        // Act
        var actionResult = await _controller.GetAll(null, null, null, 1, 10, CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task GetAll_PassesFiltersToQuery()
    {
        // Arrange
        var pagedList = new PaginatedList<CycleListItemDto>([], 0, 1, 10);
        _sender.Send(Arg.Any<ListCyclesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaginatedList<CycleListItemDto>>.Success(pagedList));

        // Act
        await _controller.GetAll("Biblioteca", 2024, CycleStatus.Configuration, 2, 5, CancellationToken.None);

        // Assert
        await _sender.Received(1).Send(
            Arg.Is<ListCyclesQuery>(q =>
                q.Department == "Biblioteca" &&
                q.Year == 2024 &&
                q.Status == CycleStatus.Configuration &&
                q.Page == 2 &&
                q.PageSize == 5),
            Arg.Any<CancellationToken>());
    }

    // =====================================================================
    // GET /api/cycles/active
    // =====================================================================

    [Fact]
    public async Task GetActive_WithExistingActiveCycle_Returns200OkWithCycleDto()
    {
        // Arrange
        var cycleDto = CreateCycleDto();
        _sender.Send(Arg.Any<GetActiveCycleQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDto?>.Success(cycleDto));

        // Act
        var actionResult = await _controller.GetActive("Biblioteca", CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var response = okResult.Value.Should().BeOfType<ApiResponse<CycleDto?>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetActive_WithNoCycleForDepartment_Returns200OkWithNullData()
    {
        // Arrange
        _sender.Send(Arg.Any<GetActiveCycleQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDto?>.Success(null));

        // Act
        var actionResult = await _controller.GetActive("Biblioteca", CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<CycleDto?>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeNull();
    }

    [Fact]
    public async Task GetActive_WithEmptyDepartment_Returns400BadRequest()
    {
        // Act
        var actionResult = await _controller.GetActive("", CancellationToken.None);

        // Assert
        var badRequestResult = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    // =====================================================================
    // GET /api/cycles/{id}
    // =====================================================================

    [Fact]
    public async Task GetById_WithExistingCycle_Returns200OkWithDetailDto()
    {
        // Arrange
        var cycleId = Guid.NewGuid();
        var detailDto = CreateCycleDetailDto(cycleId);
        _sender.Send(Arg.Any<GetCycleByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDetailDto>.Success(detailDto));

        // Act
        var actionResult = await _controller.GetById(cycleId, CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task GetById_WithNonExistentCycle_Returns404NotFound()
    {
        // Arrange
        _sender.Send(Arg.Any<GetCycleByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDetailDto>.Failure(
                $"{CycleAppError.CYCLE_NOT_FOUND}",
                "El ciclo solicitado no fue encontrado."));

        // Act
        var actionResult = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        var notFoundResult = actionResult.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    // =====================================================================
    // PUT /api/cycles/{id}/configure
    // =====================================================================

    [Fact]
    public async Task Configure_WithSuccessResult_Returns200Ok()
    {
        // Arrange
        var cycleId = Guid.NewGuid();
        var cycleDto = CreateCycleDto(cycleId);
        _sender.Send(Arg.Any<ConfigureCycleCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDto>.Success(cycleDto));

        var command = new ConfigureCycleCommand { CycleId = cycleId };

        // Act
        var actionResult = await _controller.Configure(cycleId, command, CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var response = okResult.Value.Should().BeOfType<ApiResponse<CycleDto>>().Subject;
        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Configure_WhenCycleNotFound_Returns404NotFound()
    {
        // Arrange
        var cycleId = Guid.NewGuid();
        _sender.Send(Arg.Any<ConfigureCycleCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDto>.Failure(
                $"{CycleAppError.CYCLE_NOT_FOUND}",
                "Ciclo no encontrado."));

        var command = new ConfigureCycleCommand { CycleId = cycleId };

        // Act
        var actionResult = await _controller.Configure(cycleId, command, CancellationToken.None);

        // Assert
        var statusCodeResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Configure_WhenCycleNotInConfiguration_Returns400BadRequest()
    {
        // Arrange
        var cycleId = Guid.NewGuid();
        _sender.Send(Arg.Any<ConfigureCycleCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDto>.Failure(
                $"{CycleAppError.NOT_IN_CONFIGURATION}",
                "El ciclo no está en estado Configuration."));

        var command = new ConfigureCycleCommand { CycleId = cycleId };

        // Act
        var actionResult = await _controller.Configure(cycleId, command, CancellationToken.None);

        // Assert
        var statusCodeResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Configure_OverridesCycleIdFromRoute()
    {
        // Arrange — el route id debe sobrescribir el CycleId del body
        var routeId = Guid.NewGuid();
        var bodyId = Guid.NewGuid();

        _sender.Send(Arg.Any<ConfigureCycleCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDto>.Success(CreateCycleDto(routeId)));

        var command = new ConfigureCycleCommand { CycleId = bodyId };

        // Act
        await _controller.Configure(routeId, command, CancellationToken.None);

        // Assert — el command enviado debe tener el routeId
        await _sender.Received(1).Send(
            Arg.Is<ConfigureCycleCommand>(c => c.CycleId == routeId),
            Arg.Any<CancellationToken>());
    }

    // =====================================================================
    // POST /api/cycles/{id}/open-applications
    // =====================================================================

    [Fact]
    public async Task OpenApplications_WithSuccessResult_Returns200Ok()
    {
        // Arrange
        var cycleId = Guid.NewGuid();
        var cycleDto = CreateCycleDto(cycleId, status: CycleStatus.ApplicationsOpen);
        _sender.Send(Arg.Any<OpenApplicationsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDto>.Success(cycleDto));

        // Act
        var actionResult = await _controller.OpenApplications(cycleId, CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var response = okResult.Value.Should().BeOfType<ApiResponse<CycleDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.Status.Should().Be(CycleStatus.ApplicationsOpen);
    }

    [Fact]
    public async Task OpenApplications_WhenCycleNotFound_Returns404NotFound()
    {
        // Arrange
        var cycleId = Guid.NewGuid();
        _sender.Send(Arg.Any<OpenApplicationsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDto>.Failure(
                $"{CycleAppError.CYCLE_NOT_FOUND}",
                "El ciclo solicitado no fue encontrado."));

        // Act
        var actionResult = await _controller.OpenApplications(cycleId, CancellationToken.None);

        // Assert
        var statusCodeResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task OpenApplications_WhenInvalidTransition_Returns409Conflict()
    {
        // Arrange
        var cycleId = Guid.NewGuid();
        _sender.Send(Arg.Any<OpenApplicationsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDto>.Failure(
                "INVALID_TRANSITION",
                "Solo se puede abrir postulaciones desde el estado Configuration."));

        // Act
        var actionResult = await _controller.OpenApplications(cycleId, CancellationToken.None);

        // Assert
        var statusCodeResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task OpenApplications_WhenNoPreconditionMet_Returns400BadRequest()
    {
        // Arrange
        var cycleId = Guid.NewGuid();
        _sender.Send(Arg.Any<OpenApplicationsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDto>.Failure(
                "NO_LOCATIONS",
                "Debe haber al menos una ubicación activa configurada para abrir postulaciones."));

        // Act
        var actionResult = await _controller.OpenApplications(cycleId, CancellationToken.None);

        // Assert
        var statusCodeResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task OpenApplications_SendsCorrectCommandWithCycleId()
    {
        // Arrange
        var cycleId = Guid.NewGuid();
        _sender.Send(Arg.Any<OpenApplicationsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDto>.Success(CreateCycleDto(cycleId)));

        // Act
        await _controller.OpenApplications(cycleId, CancellationToken.None);

        // Assert
        await _sender.Received(1).Send(
            Arg.Is<OpenApplicationsCommand>(c => c.CycleId == cycleId),
            Arg.Any<CancellationToken>());
    }

    // =====================================================================
    // POST /api/cycles/{id}/close-applications
    // =====================================================================

    [Fact]
    public async Task CloseApplications_WithSuccessResult_Returns200Ok()
    {
        // Arrange
        var cycleId = Guid.NewGuid();
        var cycleDto = CreateCycleDto(cycleId, status: CycleStatus.ApplicationsClosed);
        _sender.Send(Arg.Any<CloseApplicationsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDto>.Success(cycleDto));

        // Act
        var actionResult = await _controller.CloseApplications(cycleId, CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var response = okResult.Value.Should().BeOfType<ApiResponse<CycleDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.Status.Should().Be(CycleStatus.ApplicationsClosed);
    }

    [Fact]
    public async Task CloseApplications_WhenCycleNotFound_Returns404NotFound()
    {
        // Arrange
        _sender.Send(Arg.Any<CloseApplicationsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDto>.Failure(
                $"{CycleAppError.CYCLE_NOT_FOUND}",
                "El ciclo solicitado no fue encontrado."));

        // Act
        var actionResult = await _controller.CloseApplications(Guid.NewGuid(), CancellationToken.None);

        // Assert
        var statusCodeResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task CloseApplications_WhenInvalidTransition_Returns409Conflict()
    {
        // Arrange
        _sender.Send(Arg.Any<CloseApplicationsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDto>.Failure(
                "INVALID_TRANSITION",
                "Solo se puede cerrar postulaciones desde el estado ApplicationsOpen."));

        // Act
        var actionResult = await _controller.CloseApplications(Guid.NewGuid(), CancellationToken.None);

        // Assert
        var statusCodeResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task CloseApplications_SendsCorrectCommandWithCycleId()
    {
        // Arrange
        var cycleId = Guid.NewGuid();
        _sender.Send(Arg.Any<CloseApplicationsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDto>.Success(CreateCycleDto(cycleId)));

        // Act
        await _controller.CloseApplications(cycleId, CancellationToken.None);

        // Assert
        await _sender.Received(1).Send(
            Arg.Is<CloseApplicationsCommand>(c => c.CycleId == cycleId),
            Arg.Any<CancellationToken>());
    }

    // =====================================================================
    // POST /api/cycles/{id}/reopen-applications
    // =====================================================================

    [Fact]
    public async Task ReopenApplications_WithSuccessResult_Returns200Ok()
    {
        // Arrange
        var cycleId = Guid.NewGuid();
        var cycleDto = CreateCycleDto(cycleId, status: CycleStatus.ApplicationsOpen);
        _sender.Send(Arg.Any<ReopenApplicationsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDto>.Success(cycleDto));

        // Act
        var actionResult = await _controller.ReopenApplications(cycleId, CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var response = okResult.Value.Should().BeOfType<ApiResponse<CycleDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.Status.Should().Be(CycleStatus.ApplicationsOpen);
    }

    [Fact]
    public async Task ReopenApplications_WhenCycleNotFound_Returns404NotFound()
    {
        // Arrange
        _sender.Send(Arg.Any<ReopenApplicationsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDto>.Failure(
                $"{CycleAppError.CYCLE_NOT_FOUND}",
                "El ciclo solicitado no fue encontrado."));

        // Act
        var actionResult = await _controller.ReopenApplications(Guid.NewGuid(), CancellationToken.None);

        // Assert
        var statusCodeResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task ReopenApplications_WhenInvalidTransition_Returns409Conflict()
    {
        // Arrange
        _sender.Send(Arg.Any<ReopenApplicationsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDto>.Failure(
                "INVALID_TRANSITION",
                "Solo se puede reabrir postulaciones desde el estado ApplicationsClosed."));

        // Act
        var actionResult = await _controller.ReopenApplications(Guid.NewGuid(), CancellationToken.None);

        // Assert
        var statusCodeResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task ReopenApplications_SendsCorrectCommandWithCycleId()
    {
        // Arrange
        var cycleId = Guid.NewGuid();
        _sender.Send(Arg.Any<ReopenApplicationsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CycleDto>.Success(CreateCycleDto(cycleId)));

        // Act
        await _controller.ReopenApplications(cycleId, CancellationToken.None);

        // Assert
        await _sender.Received(1).Send(
            Arg.Is<ReopenApplicationsCommand>(c => c.CycleId == cycleId),
            Arg.Any<CancellationToken>());
    }

    // =====================================================================
    // GET /api/admin/dashboard-state
    // =====================================================================

    [Fact]
    public async Task GetDashboardState_WithValidDepartment_Returns200Ok()
    {
        // Arrange
        var dashboardDto = new AdminDashboardStateDto
        {
            HasLocations = true,
            LocationsCount = 3,
            HasSupervisors = true,
            SupervisorsCount = 2,
            PendingActions = []
        };

        _sender.Send(Arg.Any<GetAdminDashboardStateQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<AdminDashboardStateDto>.Success(dashboardDto));

        // Act
        var actionResult = await _adminController.GetDashboardState("Biblioteca", CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var response = okResult.Value.Should().BeOfType<ApiResponse<AdminDashboardStateDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.HasLocations.Should().BeTrue();
    }

    [Fact]
    public async Task GetDashboardState_WithEmptyDepartment_Returns400BadRequest()
    {
        // Act
        var actionResult = await _adminController.GetDashboardState("   ", CancellationToken.None);

        // Assert
        var badRequestResult = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task GetDashboardState_PassesDepartmentToQuery()
    {
        // Arrange
        _sender.Send(Arg.Any<GetAdminDashboardStateQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<AdminDashboardStateDto>.Success(new AdminDashboardStateDto()));

        // Act
        await _adminController.GetDashboardState("Informatica", CancellationToken.None);

        // Assert
        await _sender.Received(1).Send(
            Arg.Is<GetAdminDashboardStateQuery>(q => q.Department == "Informatica"),
            Arg.Any<CancellationToken>());
    }
}
