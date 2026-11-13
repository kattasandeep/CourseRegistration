using CourseRegistration.Models;

namespace CourseRegistration.Tests.Models;

public class UserTests
{
    [Fact]
    public void Constructor_WithNameAndCompletedCourses_SetsProperties()
    {
        var user = new User("Alice", ["CS101", "MATH100"]);

        Assert.Equal("Alice", user.Name);
        Assert.Equal(2, user.CompletedCourseCodes.Count);
        Assert.True(user.HasCompleted("CS101"));
        Assert.True(user.HasCompleted("MATH100"));
    }

    [Fact]
    public void Constructor_WithoutCompletedCourses_HasEmptySet()
    {
        var user = new User("Alice");

        Assert.Empty(user.CompletedCourseCodes);
    }

    [Fact]
    public void Constructor_TrimsNameAndCourseCodes()
    {
        var user = new User("  Alice ", [" CS101  "]);

        Assert.Equal("Alice", user.Name);
        Assert.Contains("CS101", user.CompletedCourseCodes);
    }

    [Fact]
    public void Constructor_IgnoresBlankAndDuplicateCourseCodes()
    {
        var user = new User("Alice", ["CS101", "", "  ", "cs101"]);

        Assert.Single(user.CompletedCourseCodes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankName_Throws(string? name)
    {
        var ex = Assert.Throws<ArgumentException>(() => new User(name!));

        Assert.Equal("name", ex.ParamName);
    }

    [Theory]
    [InlineData("CS101", true)]
    [InlineData("cs101", true)]
    [InlineData(" CS101 ", true)]
    [InlineData("CS201", false)]
    [InlineData("", false)]
    public void HasCompleted_MatchesIgnoringCaseAndSpaces(string courseCode, bool expected)
    {
        var user = new User("Alice", ["CS101"]);

        Assert.Equal(expected, user.HasCompleted(courseCode));
    }

    [Fact]
    public void MeetsPrerequisiteFor_CourseWithoutPrerequisite_IsTrue()
    {
        var user = new User("Alice");

        Assert.True(user.MeetsPrerequisiteFor(new CourseOffering("CS101", 10)));
    }

    [Fact]
    public void MeetsPrerequisiteFor_WhenPrerequisiteCompleted_IsTrue()
    {
        var user = new User("Alice", ["CS101"]);

        Assert.True(user.MeetsPrerequisiteFor(new CourseOffering("CS201", 10, "CS101")));
    }

    [Fact]
    public void MeetsPrerequisiteFor_WhenPrerequisiteNotCompleted_IsFalse()
    {
        var user = new User("Alice", ["MATH100"]);

        Assert.False(user.MeetsPrerequisiteFor(new CourseOffering("CS201", 10, "CS101")));
    }

    [Fact]
    public void MeetsPrerequisiteFor_NullCourse_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new User("Alice").MeetsPrerequisiteFor(null!));
    }
}
