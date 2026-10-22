using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Application;
using UserManagementAPI.Domain;

namespace UserManagementAPI.Controllers;

[ApiController]
[Route("api/v1/projects")]
[Authorize]
public sealed class ProjectsController(IProjectService projectService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProjectResponse>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] ProjectStatus? status = null, CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100) return BadRequest(new { error = "page must be positive and pageSize must be between 1 and 100." });
        return Ok(await projectService.ListAsync(page, pageSize, status, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return StatusCode(201, await projectService.CreateAsync(userId, request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProjectResponse>> Update(int id, UpdateProjectRequest request, CancellationToken cancellationToken) => Ok(await projectService.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/members")]
    public async Task<IActionResult> AssignUser(int id, AssignUserRequest request, CancellationToken cancellationToken) { await projectService.AssignUserAsync(id, request.UserId, cancellationToken); return NoContent(); }
}
