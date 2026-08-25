using System.Collections.Generic;
using Librariann.Common.Extensions;
using Librariann.Models.Entities;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.ReadingLists;

namespace Librariann.Models.Builders;

public class ReadingListBuilder : IEntityBuilder<ReadingList>
{
    private readonly ReadingList _readingList;
    public ReadingList Build() => _readingList;

    public ReadingListBuilder(string title)
    {
        title = title.Trim();
        _readingList = new ReadingList()
        {
            Title = title,
            NormalizedTitle = title.ToNormalized(),
            Summary = string.Empty,
            Promoted = false,
            Items = new List<ReadingListItem>(),
            AgeRating = AgeRating.Unknown
        };
    }

    public ReadingListBuilder WithSummary(string summary)
    {
        _readingList.Summary = summary ?? string.Empty;
        return this;
    }

    public ReadingListBuilder WithItem(ReadingListItem item)
    {
        _readingList.Items ??= new List<ReadingListItem>();
        _readingList.Items.Add(item);
        return this;
    }

    public ReadingListBuilder WithRating(AgeRating rating)
    {
        _readingList.AgeRating = rating;
        return this;
    }

    public ReadingListBuilder WithPromoted(bool promoted)
    {
        _readingList.Promoted = promoted;
        return this;
    }

    public ReadingListBuilder WithCoverImage(string coverImage)
    {
        _readingList.CoverImage = coverImage;
        return this;
    }

    public ReadingListBuilder WithAppUserId(int userId)
    {
        _readingList.AppUserId = userId;
        return this;
    }

    public ReadingListBuilder WithStartingYear(int year)
    {
        _readingList.StartingYear = year;
        return this;
    }

    public ReadingListBuilder WithTag(ReadingListTag tag)
    {
        _readingList.Tags ??= new List<ReadingListTag>();
        _readingList.Tags.Add(tag);
        return this;
    }
}
