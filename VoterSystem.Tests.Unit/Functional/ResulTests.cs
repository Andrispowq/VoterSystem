using VoterSystem.DataAccess.Functional;

namespace VoterSystem.Tests.Unit.Functional;

public class ResultTests
{
    private static readonly ServiceError Err = new BadRequestError("nah");

    [Fact]
    public void Implicit_constructors_yield_expected_variant_flags()
    {
        Result<int, ServiceError> ok = 123;
        Result<int, ServiceError> fail = Err;

        Assert.True(ok.HasValue);
        Assert.False(ok.IsError);

        Assert.False(fail.HasValue);
        Assert.True(fail.IsError);
    }

    [Fact]
    public void Match_invokes_correct_delegate()
    {
        Result<int, ServiceError> ok = 7;
        Result<int, ServiceError> fail = Err;

        bool okHit = false;
        bool errHit = false;

        ok.Match(_ => okHit = true, _ => errHit = true);
        Assert.True(okHit);
        Assert.False(errHit);

        okHit = errHit = false;

        fail.Match(_ => okHit = true, _ => errHit = true);
        Assert.False(okHit);
        Assert.True(errHit);
    }

    [Fact]
    public void Map_and_MapError_transform_correct_side_only()
    {
        Result<int, ServiceError> ok = 10;
        Result<int, ServiceError> fail = Err;

        // Map returns a plain value
        int mappedOk = ok.Map(x => x * 3, _ => -1);
        int mappedFail = fail.Map(x => x * 3, _ => -1);

        Assert.Equal(30, mappedOk);
        Assert.Equal(-1, mappedFail);

        // MapError changes only the error variant
        Result<int, string> mappedFailErr = fail.MapError(e => e.Message + "!!");
        Result<int, string> mappedOkErr = ok.MapError(e => e.Message);

        Assert.True(mappedFailErr.IsError);
        Assert.Equal("nah!!", mappedFailErr.Error);

        Assert.True(mappedOkErr.HasValue);
        Assert.Equal(10, mappedOkErr.Value);
    }

    [Fact]
    public async Task MapAsync_returns_task_with_transformed_value()
    {
        Result<int, ServiceError> ok = 5;
        Result<int, ServiceError> fail = Err;

        int okAsync = await ok.MapAsync(x => Task.FromResult(x + 1), _ => Task.FromResult(-1));
        int failAsync = await fail.MapAsync(x => Task.FromResult(x + 1), _ => Task.FromResult(-1));

        Assert.Equal(6, okAsync);
        Assert.Equal(-1, failAsync);
    }

    [Fact]
    public void Value_and_Error_accessors_work_and_throw_as_documented()
    {
        Result<int, ServiceError> ok = 8;
        Result<int, ServiceError> fail = Err;

        Assert.Equal(8, ok.Value);
        Assert.Throws<InvalidOperationException>(() =>
        {
            var _ = fail.Value;
        });

        Assert.Same(Err, fail.Error);
        Assert.Throws<InvalidOperationException>(() =>
        {
            var _ = ok.Error;
        });
    }

    [Fact]
    public void Equality_and_hashcode_only_compare_same_variant()
    {
        var a1 = Result<int, ServiceError>.Success(3);
        var a2 = Result<int, ServiceError>.Success(3);
        var err1 = Result<int, ServiceError>.Failure(Err);
        var err2 = Result<int, ServiceError>.Failure(Err);

        Assert.Equal(a1, a2);
        Assert.Equal(err1, err2);
        Assert.NotEqual(a1, err1);

        Assert.Equal(a1.GetHashCode(), a2.GetHashCode());
        Assert.Equal(err1.GetHashCode(), err2.GetHashCode());
    }

    [Fact]
    public void ToString_discriminates_between_value_and_error()
    {
        Result<int, ServiceError> ok = 4;
        Result<int, ServiceError> fail = Err;

        Assert.Equal("Value(4)", ok.ToString());
        Assert.Equal($"Error({Err})", fail.ToString());
    }
}