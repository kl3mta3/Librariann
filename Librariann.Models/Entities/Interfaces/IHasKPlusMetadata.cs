using System.Collections.Generic;
using Librariann.Models.Entities.MetadataMatching;

namespace Librariann.Models.Entities.Interfaces;

public interface IHasKPlusMetadata
{
    /// <summary>
    /// Tracks which metadata has been set by K+
    /// </summary>
    public IList<MetadataSettingField> KPlusOverrides { get; set; }
}
