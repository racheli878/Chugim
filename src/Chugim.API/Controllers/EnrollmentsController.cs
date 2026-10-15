using Chugim.API.Authorization;
using Chugim.Application.Common;
using Chugim.Application.DTOs.Enrollments;
using Chugim.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chugim.API.Controllers;

[Route("api/enrollments")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class EnrollmentsController : ApiControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService) => _enrollmentService = enrollmentService;

    /// <summary>הרשמה לחוג. כאן מתרחשת התחרות על המקומות — 409 כשהחוג מלא או כשמישהי תפסה את המקום.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(EnrollmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrollmentResponse>> Enroll(
        [FromBody] EnrollRequest request, CancellationToken cancellationToken) =>
        CreatedFromResult(await _enrollmentService.EnrollAsync(CurrentUser, request, cancellationToken),
            nameof(GetById), enrollment => new { id = enrollment.Id });

    /// <summary>משתתפת: ההרשמות שלה. מנהלת: כל ההרשמות.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EnrollmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EnrollmentResponse>>> GetPaged(
        [FromQuery] PaginationQuery query, CancellationToken cancellationToken) =>
        Ok(await _enrollmentService.GetPagedAsync(CurrentUser, query, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EnrollmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrollmentResponse>> GetById(
        [FromRoute] int id, CancellationToken cancellationToken) =>
        FromResult(await _enrollmentService.GetByIdAsync(id, CurrentUser, cancellationToken));

    /// <summary>ביטול הרשמה — משחרר את המקום בחוג.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel([FromRoute] int id, CancellationToken cancellationToken) =>
        FromResult(await _enrollmentService.CancelAsync(id, CurrentUser, cancellationToken));
}
