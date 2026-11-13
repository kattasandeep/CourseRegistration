using CourseRegistration.Models;

namespace CourseRegistration.Tests.Models;

public class UserNamesTests
{
    [Theory]
    [InlineData("Alice", "Alice", true)]
    [InlineData("Alice", "alice", true)]
    [InlineData("Alice", "ALICE", true)]
    [InlineData("Alice", "Alicia", false)]
    public void Comparer_IgnoresCase(string a, string b, bool expected)
    {
        Assert.Equal(expected, UserNames.Comparer.Equals(a, b));
    }

    [Fact]
    public void Comparer_GivesSameHashCodeForNamesDifferingOnlyInCase()
    {
        Assert.Equal(UserNames.Comparer.GetHashCode("Alice"), UserNames.Comparer.GetHashCode("aLiCe"));
    }
}
