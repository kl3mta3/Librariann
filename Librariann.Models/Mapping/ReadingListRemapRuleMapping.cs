using System;
using System.Linq.Expressions;
using Librariann.Models.DTOs.ReadingLists.CBL.RemapRules;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.Enums.ReadingList;
using Librariann.Models.Entities.ReadingLists;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for <c>CreateMap&lt;ReadingListRemapRule, RemapRuleDto&gt;()</c> (<c>AutoMapperProfiles.cs</c>).
/// <see cref="RemapRuleDto.Kind"/> was originally <c>.MapFrom(src => src.GetKind())</c>, a call to
/// <see cref="ReadingListRemapRule.GetKind"/> — EF Core can't translate an arbitrary method call inside
/// <c>.Select()</c>, so its trivial three-way null-check logic is inlined directly here instead (verified
/// identical to the method body).
/// </summary>
public static class ReadingListRemapRuleMapping
{
    public static readonly Expression<Func<ReadingListRemapRule, RemapRuleDto>> ToRemapRuleDtoExpression = r => new RemapRuleDto
    {
        Id = r.Id,
        NormalizedCblSeriesName = r.NormalizedCblSeriesName,
        CblSeriesName = r.CblSeriesName,
        CblVolume = r.CblVolume,
        CblNumber = r.CblNumber,
        SeriesId = r.SeriesId,
        VolumeId = r.VolumeId,
        VolumeNumber = r.Volume != null ? r.Volume.Name : string.Empty,
        ChapterId = r.ChapterId,
        Kind = r.ChapterId != null ? CblRemapRuleKind.Chapter : r.VolumeId != null ? CblRemapRuleKind.Volume : CblRemapRuleKind.Series,
        ChapterRange = r.Chapter != null ? r.Chapter.Range : string.Empty,
        ChapterTitleName = r.Chapter != null ? r.Chapter.TitleName : string.Empty,
        ChapterIsSpecial = r.Chapter != null && r.Chapter.IsSpecial,
        LibraryType = r.Series.Library != null ? r.Series.Library.Type : LibraryType.Comic,
        SeriesNameAtMapping = r.SeriesNameAtMapping,
        AppUserId = r.AppUserId,
        IsGlobal = r.IsGlobal,
        CreatedByUserName = r.AppUser != null ? r.AppUser.UserName! : string.Empty,
        CreatedUtc = r.CreatedUtc,
    };

    private static readonly Func<ReadingListRemapRule, RemapRuleDto> CompiledToRemapRuleDto = ToRemapRuleDtoExpression.Compile();

    public static RemapRuleDto ToRemapRuleDto(this ReadingListRemapRule r) => CompiledToRemapRuleDto(r);
}
