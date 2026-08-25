using System;
using System.Linq.Expressions;
using Librariann.Models.DTOs;
using Librariann.Models.Entities.User;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for <c>CreateMap&lt;AppUserReadingProfile, UserReadingProfileDto&gt;()</c>. Faithfully
/// leaves <see cref="UserReadingProfileDto.UserId"/> unset (0) since the entity's <c>AppUserId</c> never
/// flat-matched it under AutoMapper's default convention either — the destination DTO has no matching
/// <c>ForMember</c> for it, and no caller sets it after mapping.
/// </summary>
public static class ReadingProfileMapping
{
    public static readonly Expression<Func<AppUserReadingProfile, UserReadingProfileDto>> ToUserReadingProfileDtoExpression = p => new UserReadingProfileDto
    {
        Id = p.Id,
        Name = p.Name,
        Kind = p.Kind,
        DeviceIds = p.DeviceIds,
        SeriesIds = p.SeriesIds,
        LibraryIds = p.LibraryIds,
        ReadingDirection = p.ReadingDirection,
        ScalingOption = p.ScalingOption,
        PageSplitOption = p.PageSplitOption,
        ReaderMode = p.ReaderMode,
        AutoCloseMenu = p.AutoCloseMenu,
        ShowScreenHints = p.ShowScreenHints,
        EmulateBook = p.EmulateBook,
        LayoutMode = p.LayoutMode,
        BackgroundColor = p.BackgroundColor,
        SwipeToPaginate = p.SwipeToPaginate,
        AllowAutomaticWebtoonReaderDetection = p.AllowAutomaticWebtoonReaderDetection,
        WidthOverride = p.WidthOverride,
        DisableWidthOverride = p.DisableWidthOverride,
        BookReaderMargin = p.BookReaderMargin,
        BookReaderLineSpacing = p.BookReaderLineSpacing,
        BookReaderFontSize = p.BookReaderFontSize,
        BookReaderFontFamily = p.BookReaderFontFamily,
        BookReaderTapToPaginate = p.BookReaderTapToPaginate,
        BookReaderReadingDirection = p.BookReaderReadingDirection,
        BookReaderWritingStyle = p.BookReaderWritingStyle,
        BookReaderThemeName = p.BookThemeName,
        BookReaderBackgroundColor = p.BookReaderBackgroundColor,
        BookReaderBackgroundOpacity = p.BookReaderBackgroundOpacity,
        BookReaderLayoutMode = p.BookReaderLayoutMode,
        BookReaderImmersiveMode = p.BookReaderImmersiveMode,
        BookReaderDisableBookmarkIcon = p.BookReaderDisableBookmarkIcon,
        PdfTheme = p.PdfTheme,
        PdfScrollMode = p.PdfScrollMode,
        PdfSpreadMode = p.PdfSpreadMode,
    };

    private static readonly Func<AppUserReadingProfile, UserReadingProfileDto> CompiledToUserReadingProfileDto = ToUserReadingProfileDtoExpression.Compile();

    public static UserReadingProfileDto ToUserReadingProfileDto(this AppUserReadingProfile p) => CompiledToUserReadingProfileDto(p);
}
