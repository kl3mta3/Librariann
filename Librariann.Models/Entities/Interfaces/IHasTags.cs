using System.Collections.Generic;

namespace Librariann.Models.Entities.Interfaces;

public interface IHasTags<T> where T : class, ITag
{
    ICollection<T> Tags { get; set; }
}
