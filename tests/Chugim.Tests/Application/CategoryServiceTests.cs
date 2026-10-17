using Chugim.Application.Common;
using Chugim.Application.DTOs.Categories;
using Chugim.Application.Interfaces;
using Chugim.Application.Services;
using Chugim.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static Chugim.Tests.TestSupport.TestData;

namespace Chugim.Tests.Application;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CategoryService _service;

    public CategoryServiceTests() =>
        _service = new CategoryService(_categories.Object, _unitOfWork.Object, Mapper, NullLogger<CategoryService>.Instance);

    [Fact]
    public async Task Create_NewName_AddsTrimmedNameAndSaves()
    {
        Category? added = null;
        _categories.Setup(r => r.Add(It.IsAny<Category>())).Callback<Category>(c => added = c);

        var result = await _service.CreateAsync(new CategoryRequest { Name = "  רקמה  " }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("רקמה", added!.Name);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ExistingName_ReturnsConflictAndDoesNotSave()
    {
        _categories.Setup(r => r.NameExistsAsync("אומנות", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.CreateAsync(new CategoryRequest { Name = "אומנות" }, CancellationToken.None);

        Assert.Equal(AppErrors.CategoryNameTaken, result.Error);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
