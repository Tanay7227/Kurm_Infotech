using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Application;

namespace UserManagementAPI.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = "AdminOnly")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<UserResponse>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100) return BadRequest(new { error = "page must be positive and pageSize must be between 1 and 100." });
        return Ok(await userService.ListAsync(page, pageSize, search, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request, CancellationToken cancellationToken) => StatusCode(201, await userService.CreateAsync(request, cancellationToken));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponse>> Update(int id, UpdateUserRequest request, CancellationToken cancellationToken) => Ok(await userService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) { await userService.DeleteAsync(id, cancellationToken); return NoContent(); }
}
