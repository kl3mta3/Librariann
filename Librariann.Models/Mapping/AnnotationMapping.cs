using System;
using System.Linq.Expressions;
using Librariann.Models.DTOs.Annotations;
using Librariann.Models.DTOs.Reader;
using Librariann.Models.Entities.User;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for <c>CreateMap&lt;AppUserAnnotation, AnnotationDto&gt;()</c> and
/// <c>CreateMap&lt;AppUserAnnotation, FullAnnotationDto&gt;()</c>.
/// </summary>
public static class AnnotationMapping
{
    public static readonly Expression<Func<AppUserAnnotation, AnnotationDto>> ToAnnotationDtoExpression = a => new AnnotationDto
    {
        Id = a.Id,
        XPath = a.XPath,
        EndingXPath = a.EndingXPath,
        Cfi = a.Cfi,
        SelectedText = a.SelectedText,
        Comment = a.Comment,
        CommentHtml = a.CommentHtml,
        CommentPlainText = a.CommentPlainText,
        ChapterTitle = a.ChapterTitle,
        Context = a.Context,
        HighlightCount = a.HighlightCount,
        ContainsSpoiler = a.ContainsSpoiler,
        PageNumber = a.PageNumber,
        SelectedSlotIndex = a.SelectedSlotIndex,
        Likes = a.Likes,
        SeriesName = a.Series.Name,
        LibraryName = a.Library.Name,
        ChapterId = a.ChapterId,
        VolumeId = a.VolumeId,
        SeriesId = a.SeriesId,
        LibraryId = a.LibraryId,
        OwnerUserId = a.AppUserId,
        OwnerUsername = a.AppUser.UserName!,
        AgeRating = a.Series.Metadata.AgeRating,
        CreatedUtc = a.CreatedUtc,
        LastModifiedUtc = a.LastModifiedUtc,
    };

    public static readonly Expression<Func<AppUserAnnotation, FullAnnotationDto>> ToFullAnnotationDtoExpression = a => new FullAnnotationDto
    {
        Id = a.Id,
        UserId = a.AppUserId,
        SelectedText = a.SelectedText,
        Comment = a.Comment,
        CommentHtml = a.CommentHtml,
        CommentPlainText = a.CommentPlainText,
        Context = a.Context,
        ChapterTitle = a.ChapterTitle,
        PageNumber = a.PageNumber,
        SelectedSlotIndex = a.SelectedSlotIndex,
        ContainsSpoiler = a.ContainsSpoiler,
        CreatedUtc = a.CreatedUtc,
        LastModifiedUtc = a.LastModifiedUtc,
        LibraryId = a.LibraryId,
        LibraryName = a.Library.Name,
        SeriesId = a.SeriesId,
        SeriesName = a.Series.Name,
        VolumeId = a.VolumeId,
        VolumeName = a.Chapter.Volume.Name,
        ChapterId = a.ChapterId,
    };

    private static readonly Func<AppUserAnnotation, AnnotationDto> CompiledToAnnotationDto = ToAnnotationDtoExpression.Compile();
    private static readonly Func<AppUserAnnotation, FullAnnotationDto> CompiledToFullAnnotationDto = ToFullAnnotationDtoExpression.Compile();

    public static AnnotationDto ToAnnotationDto(this AppUserAnnotation a) => CompiledToAnnotationDto(a);
    public static FullAnnotationDto ToFullAnnotationDto(this AppUserAnnotation a) => CompiledToFullAnnotationDto(a);
}
