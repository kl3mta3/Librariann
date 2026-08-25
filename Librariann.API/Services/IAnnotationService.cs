using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Reader;

namespace Librariann.API.Services;

public interface IAnnotationService
{
    Task<AnnotationDto> CreateAnnotation(int userId, AnnotationDto dto, CancellationToken ct = default);
    Task<AnnotationDto> UpdateAnnotation(int userId, AnnotationDto dto, CancellationToken ct = default);

    /// <summary>
    /// Backfills the CFI position for an existing (pre-foliate-js) annotation, computed client-side by the v2
    /// reader from the annotation's existing XPath/EndingXPath/SelectedText. Deliberately separate from
    /// <see cref="UpdateAnnotation"/>, which only touches user-editable fields (spoiler/slot/comment) - this is
    /// a one-time positional backfill, not user content.
    /// </summary>
    Task SetAnnotationCfi(int userId, int annotationId, string cfi, CancellationToken ct = default);

    /// <summary>
    /// Export all annotations for a user, or optionally specify which annotation exactly
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="annotationIds"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<string> ExportAnnotations(int userId, IList<int>? annotationIds = null, CancellationToken ct = default);
}
