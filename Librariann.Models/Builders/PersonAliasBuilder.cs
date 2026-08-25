using Librariann.Common.Extensions;
using Librariann.Models.Entities.Person;

namespace Librariann.Models.Builders;

public class PersonAliasBuilder : IEntityBuilder<PersonAlias>
{
    private readonly PersonAlias _alias;
    public PersonAlias Build() => _alias;

    public PersonAliasBuilder(string name)
    {
        _alias = new PersonAlias()
        {
            Alias = name.Trim(),
            NormalizedAlias = name.ToNormalized(),
        };
    }
}
