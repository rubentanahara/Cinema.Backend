using NSubstitute;

using Shouldly;

namespace TestCommon.TestUtilities.NSubstitute;

public static class Must
{
    public static List<T> BeEmptyList<T>() =>
        Arg.Do<List<T>>(x => x.ShouldBeEmpty());

    public static List<T> BeListWith<T>(List<T> value) =>
        Arg.Do<List<T>>(x => x.ShouldBe(value));
}