using System;
using System.Linq.Expressions;
using Librariann.Models.DTOs.Collection;
using Librariann.Models.Entities.User;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;AppUserCollection, AppUserCollectionDto&gt;()</c>.</summary>
public static class AppUserCollectionMapping
{
    public static readonly Expression<Func<AppUserCollection, AppUserCollectionDto>> ToAppUserCollectionDtoExpression = c => new AppUserCollectionDto
    {
        Id = c.Id,
        Title = c.Title,
        Summary = c.Summary,
        Promoted = c.Promoted,
        AgeRating = c.AgeRating,
        CoverImage = c.CoverImage,
        PrimaryColor = c.PrimaryColor,
        SecondaryColor = c.SecondaryColor,
        CoverImageLocked = c.CoverImageLocked,
        ItemCount = c.Items.Count,
        Owner = c.AppUser.UserName,
        LastSyncUtc = c.LastSyncUtc,
        Source = c.Source,
        SourceUrl = c.SourceUrl,
        TotalSourceCount = c.TotalSourceCount,
        MissingSeriesFromSource = c.MissingSeriesFromSource,
    };

    private static readonly Func<AppUserCollection, AppUserCollectionDto> CompiledToAppUserCollectionDto = ToAppUserCollectionDtoExpression.Compile();

    public static AppUserCollectionDto ToAppUserCollectionDto(this AppUserCollection c) => CompiledToAppUserCollectionDto(c);
}
