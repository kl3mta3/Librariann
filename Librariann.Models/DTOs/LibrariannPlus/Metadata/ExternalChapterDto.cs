using System;
using System.Collections.Generic;
using Librariann.Models.DTOs.SeriesDetail;

namespace Librariann.Models.DTOs.LibrariannPlus.Metadata;
#nullable enable

/// <summary>
/// Information about an individual issue/chapter/book from Librariann+
/// </summary>
public sealed record ExternalChapterDto
{
    public string Title { get; set; }

    public string IssueNumber { get; set; }

    public decimal? CriticRating { get; set; }

    public decimal? UserRating { get; set; }

    public string? Summary { get; set; }

    public IList<string>? Writers { get; set; }

    public IList<string>? Artists { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public string? Publisher { get; set; }

    public string? CoverImageUrl { get; set; }

    public string? IssueUrl { get; set; }

    public int? HardcoverId { get; set; }

    public string? MangaBakaWorkId { get; set; }

    public IList<UserReviewDto> CriticReviews { get; set; }
    public IList<UserReviewDto> UserReviews { get; set; }
}
