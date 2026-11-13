using CourseRegistration.Models;

namespace CourseRegistration.Tests.Models;

public class CourseOfferingTests
{
    [Fact]
    public void Constructor_WithValidValues_SetsProperties()
    {
        var offering = new CourseOffering("CS201", 30, "CS101");

        Assert.Equal("CS201", offering.CourseCode);
        Assert.Equal(30, offering.Capacity);
        Assert.Equal("CS101", offering.PrerequisiteCourseCode);
        Assert.True(offering.HasPrerequisite);
    }

    [Fact]
    public void Constructor_WithoutPrerequisite_PrerequisiteIsNull()
    {
        var offering = new CourseOffering("CS101", 25);

        Assert.Null(offering.PrerequisiteCourseCode);
        Assert.False(offering.HasPrerequisite);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankPrerequisite_TreatsAsNoPrerequisite(string prerequisite)
    {
        var offering = new CourseOffering("CS101", 25, prerequisite);

        Assert.Null(offering.PrerequisiteCourseCode);
        Assert.False(offering.HasPrerequisite);
    }

    [Fact]
    public void Constructor_TrimsWhitespaceFromCodes()
    {
        var offering = new CourseOffering("  CS201 ", 30, " CS101  ");

        Assert.Equal("CS201", offering.CourseCode);
        Assert.Equal("CS101", offering.PrerequisiteCourseCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingCourseCode_Throws(string? courseCode)
    {
        var ex = Assert.Throws<ArgumentException>(() => new CourseOffering(courseCode!, 30));

        Assert.Equal("courseCode", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveCapacity_Throws(int capacity)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new CourseOffering("CS101", capacity));

        Assert.Equal("capacity", ex.ParamName);
    }

    [Theory]
    [InlineData("CS101")]
    [InlineData("cs101")]
    [InlineData(" CS101 ")]
    public void Constructor_WithSelfAsPrerequisite_Throws(string prerequisite)
    {
        var ex = Assert.Throws<ArgumentException>(() => new CourseOffering("CS101", 30, prerequisite));

        Assert.Equal("prerequisiteCourseCode", ex.ParamName);
    }
}
