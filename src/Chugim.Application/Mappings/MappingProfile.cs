using AutoMapper;
using Chugim.Application.DTOs.Auth;
using Chugim.Application.DTOs.Categories;
using Chugim.Application.DTOs.Courses;
using Chugim.Application.DTOs.Enrollments;
using Chugim.Domain.Entities;

namespace Chugim.Application.Mappings;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Category, CategoryResponse>();

        // CategoryName ממולא אוטומטית מ-Category.Name (flattening לפי שם).
        CreateMap<Course, CourseResponse>();

        // משמש גם ליצירה וגם לעדכון: _mapper.Map(request, existingCourse).
        // השדות המוגנים לא ממופים — Capacity נקבע רק דרך course.TryChangeCapacity.
        CreateMap<CourseRequest, Course>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Capacity, o => o.Ignore())
            .ForMember(d => d.EnrolledCount, o => o.Ignore())
            .ForMember(d => d.Version, o => o.Ignore())
            .ForMember(d => d.Category, o => o.Ignore())
            .ForMember(d => d.Enrollments, o => o.Ignore());

        // CourseTitle ממולא אוטומטית מ-Course.Title.
        CreateMap<Enrollment, EnrollmentResponse>()
            .ForMember(d => d.ParticipantName, o => o.MapFrom(s => s.User.FullName));

        // ה-token ותוקפו מגיעים מ-IJwtTokenGenerator ומשולבים ב-service.
        CreateMap<User, AuthResponse>()
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Token, o => o.Ignore())
            .ForMember(d => d.ExpiresAtUtc, o => o.Ignore());
    }
}
