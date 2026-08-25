using System.Collections.Generic;
using Librariann.Common.Extensions;
using Librariann.Models.Entities;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.User;

namespace Librariann.Models.Builders;

public class AppUserCollectionBuilder : IEntityBuilder<AppUserCollection>
{
    private readonly AppUserCollection _collection;
    public AppUserCollection Build() => _collection;

    public AppUserCollectionBuilder(string title, bool promoted = false)
    {
        title = title.Trim();
        _collection = new AppUserCollection()
        {
            Id = 0,
            NormalizedTitle = title.ToNormalized(),
            Title = title,
            Promoted = promoted,
            Summary = string.Empty,
            AgeRating = AgeRating.Unknown,
            Source = ScrobbleProvider.Librariann,
            Items = new List<Series>()
        };
    }

    public AppUserCollectionBuilder WithSource(ScrobbleProvider provider)
    {
        _collection.Source = provider;
        return this;
    }


    public AppUserCollectionBuilder WithSummary(string summary)
    {
        _collection.Summary = summary;
        return this;
    }

    public AppUserCollectionBuilder WithIsPromoted(bool promoted)
    {
        _collection.Promoted = promoted;
        return this;
    }

    public AppUserCollectionBuilder WithItem(Series series)
    {
        _collection.Items ??= new List<Series>();
        _collection.Items.Add(series);
        return this;
    }

    public AppUserCollectionBuilder WithItems(IEnumerable<Series> series)
    {
        _collection.Items ??= new List<Series>();
        foreach (var s in series)
        {
            _collection.Items.Add(s);
        }

        return this;
    }

    public AppUserCollectionBuilder WithCoverImage(string cover)
    {
        _collection.CoverImage = cover;
        return this;
    }

    public AppUserCollectionBuilder WithSourceUrl(string url)
    {
        _collection.SourceUrl = url;
        return this;
    }
}
