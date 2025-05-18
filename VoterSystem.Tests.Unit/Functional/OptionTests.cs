using VoterSystem.Shared.Functional;

namespace VoterSystem.Tests.Unit.Functional;

public class OptionTests
{
    [Fact]
    public void Implicit_conversion_produces_Some()
    {
        Option<string> opt = "hiya";

        Assert.True(opt.IsSome);
        Assert.False(opt.IsNone);
        Assert.Equal("hiya", opt.AsSome.Value);
    }

    [Fact]
    public void Map_projects_value_when_Some_and_executes_noneFunc_when_None()
    {
        Option<int> some = 10;
        Option<int>.None none = new();

        var mappedSome = some.Map(x => x * 2, () => -1);
        var mappedNone = none.Map(x => x * 2, () => -1);

        Assert.Equal(20, mappedSome);
        Assert.Equal(-1, mappedNone);
    }

    [Fact]
    public void MapOption_lifts_result_into_new_Option()
    {
        Option<int> some = 5;
        Option<int>.None none = new();

        Option<int> mappedSome = some.MapOption(x => new Option<int>.Some(x * 3));
        Option<int> mappedNone = none.MapOption(x => new Option<int>.Some(x * 3));

        Assert.True(mappedSome.IsSome);
        Assert.Equal(15, mappedSome.AsSome.Value);

        Assert.True(mappedNone.IsNone);
    }

    [Fact]
    public void AsSome_and_AsNone_throw_if_wrong_variant()
    {
        Option<int> some = 1;
        Option<int>.None none = new();

        Assert.Throws<InvalidOperationException>(() => { _ = none.AsSome; });
        Assert.Throws<InvalidOperationException>(() => { _ = some.AsNone; });
    }

    [Fact]
    public void ThrowIfNone_and_ThrowIfSome_behave_correctly()
    {
        Option<int> some = 1;
        Option<int>.None none = new();

        // wrong-variant calls throw
        Assert.Throws<InvalidOperationException>(() => some.ThrowIfSome());
        Assert.Throws<InvalidOperationException>(() => none.ThrowIfNone());

        // right-variant calls do not throw
        some.ThrowIfNone();
        none.ThrowIfSome();
    }

    [Fact]
    public void ToString_reflects_variant_and_value()
    {
        Option<int> some = 42;
        Option<int>.None none = new();

        Assert.Equal("Some(42)", some.ToString());
        Assert.Equal("None", none.ToString());
    }
}