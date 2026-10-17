using AutoMapper;
using Chugim.Application.Mappings;

namespace Chugim.Tests.Application;

public class MappingProfileTests
{
    // נכשלת אם יש שדה ב-DTO או בישות שאף אחד לא ממפה ולא הוגדר במפורש כ-Ignore —
    // למשל אם יתווסף שדה ל-CourseResponse וישכחו לטפל בו.
    [Fact]
    public void MappingProfile_ConfigurationIsValid() =>
        new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).AssertConfigurationIsValid();
}
