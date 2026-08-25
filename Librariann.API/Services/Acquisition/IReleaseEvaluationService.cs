using Librariann.Models.DTOs.Acquisition;

namespace Librariann.API.Services.Acquisition;

public interface IReleaseEvaluationService
{
    ReleaseDecision Evaluate(ReleaseCandidate candidate, ReleaseEvaluationContext context);
}

