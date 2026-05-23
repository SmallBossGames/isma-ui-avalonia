using System.Collections.Immutable;
using ISMA.Domain.Contracts;

namespace ISMA.Domain.Results;

public abstract class LismaPdeTranslationResult
{
    private LismaPdeTranslationResult() { }

    public static LismaPdeTranslationResult Success(ISMA.Domain.Models.LismaTextModel model)
        => new SuccessTranslation(model);

    public static LismaPdeTranslationResult Failed(ImmutableArray<string> errors)
        => new FailedTranslation(errors);

    public abstract bool IsSuccess { get; }

    private sealed class SuccessTranslation(ISMA.Domain.Models.LismaTextModel model) : LismaPdeTranslationResult
    {
        public override bool IsSuccess => true;
        public ISMA.Domain.Models.LismaTextModel Model { get; } = model;
    }

    private sealed class FailedTranslation(ImmutableArray<string> errors) : LismaPdeTranslationResult
    {
        public override bool IsSuccess => false;
        public ImmutableArray<string> Errors { get; } = errors;
    }
}
