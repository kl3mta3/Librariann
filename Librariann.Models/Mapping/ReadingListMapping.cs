using System;
using System.Linq;
using System.Linq.Expressions;
using Librariann.Models.DTOs.ReadingLists;
using Librariann.Models.Entities.ReadingLists;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for the merged <c>CreateMap&lt;ReadingList, ReadingListDto&gt;()</c> registrations in
/// <c>AutoMapperProfiles.cs</c> and <c>AutoMapperReadingListProfile.cs</c> (both registered the same pair;
/// AutoMapper merges the ForMembers from both into one config — <c>Tags</c> came from the former,
/// <c>ItemCount</c>/<c>OwnerUserName</c> from the latter). No per-user parameterization needed here (unlike
/// <see cref="ReadingListItemMapping"/>) — nothing on <see cref="ReadingListDto"/> is user-scoped.
/// </summary>
public static class ReadingListMapping
{
    public static readonly Expression<Func<ReadingList, ReadingListDto>> ToReadingListDtoExpression = rl => new ReadingListDto
    {
        Id = rl.Id,
        Title = rl.Title,
        Summary = rl.Summary!,
        Promoted = rl.Promoted,
        CoverImageLocked = rl.CoverImageLocked,
        CoverImage = rl.CoverImage,
        PrimaryColor = rl.PrimaryColor,
        SecondaryColor = rl.SecondaryColor,
        ItemCount = rl.Items.Count,
        StartingYear = rl.StartingYear,
        StartingMonth = rl.StartingMonth,
        EndingYear = rl.EndingYear,
        EndingMonth = rl.EndingMonth,
        AgeRating = rl.AgeRating,
        OwnerUserName = rl.AppUser.UserName!,
        SourcePath = rl.SourcePath,
        DownloadUrl = rl.DownloadUrl,
        ShaHash = rl.ShaHash,
        Provider = rl.Provider,
        LastSyncCheckUtc = rl.LastSyncCheckUtc,
        LastSyncedUtc = rl.LastSyncedUtc,
        TotalItemsAtImport = rl.TotalItemsAtImport,
        Tags = rl.Tags.AsQueryable().Select(ReadingListTagMapping.ToReadingListTagDtoExpression).ToList(),
        CanSync = rl.CanSync,
    };

    private static readonly Func<ReadingList, ReadingListDto> CompiledToReadingListDto = ToReadingListDtoExpression.Compile();

    public static ReadingListDto ToReadingListDto(this ReadingList rl) => CompiledToReadingListDto(rl);
}
