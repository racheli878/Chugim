using Chugim.API.Authorization;
using Chugim.Application.Common;
using Chugim.Application.DTOs.Courses;
using Chugim.Application.DTOs.Enrollments;
using Chugim.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chugim.API.Controllers;

// כל החוגים מוצגים רק למחוברות. פעולות הניהול מוגבלות בנוסף ל-Admin.
[Route("api/courses")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class CoursesController : ApiControllerBase
{
    private readonly ICourseService _courseService;
    private readonly IEnrollmentService _enrollmentService;

    public CoursesController(ICourseService courseService, IEnrollmentService enrollmentService)
    {
        _courseService = courseService;
        _enrollmentService = enrollmentService;
    }

    /// <summary>רשימת חוגים עם סינון, מיון ועימוד.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CourseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<CourseResponse>>> GetPaged(
        [FromQuery] CourseQuery query, CancellationToken cancellationToken) =>
        Ok(await _courseService.GetPagedAsync(query, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseResponse>> GetById(
        [FromRoute] int id, CancellationToken cancellationToken) =>
        FromResult(await _courseService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CourseResponse>> Create(
        [FromBody] CourseRequest request, CancellationToken cancellationToken) =>
        CreatedFromResult(await _courseService.CreateAsync(request, cancellationToken),
            nameof(GetById), course => new { id = course.Id });

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [FromRoute] int id, [FromBody] CourseRequest request, CancellationToken cancellationToken) =>
        FromResult(await _courseService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken) =>
        FromResult(await _courseService.DeleteAsync(id, cancellationToken));

    /// <summary>המשתתפות הרשומות לחוג (מנהלת בלבד).</summary>
    [HttpGet("{id:int}/enrollments")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(PagedResult<EnrollmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<EnrollmentResponse>>> GetEnrollments(
        [FromRoute] int id, [FromQuery] PaginationQuery query, CancellationToken cancellationToken) =>
        FromResult(await _enrollmentService.GetByCourseAsync(id, query, cancellationToken));
}
