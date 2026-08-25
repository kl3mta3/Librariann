using Librariann.Common.Extensions;
using Librariann.Models.DTOs;

namespace Librariann.Services.Extensions;

public static class VolumeExtensions
{

    extension(VolumeDto volumeDto)
    {
        /// <summary>
        /// Is this a loose leaf volume
        /// </summary>
        /// <returns></returns>
        public bool IsLooseLeaf()
        {
            return volumeDto.MinNumber.Is(Scanner.Parser.LooseLeafVolumeNumber);
        }

        /// <summary>
        /// Does this volume hold only specials
        /// </summary>
        /// <returns></returns>
        public bool IsSpecial()
        {
            return volumeDto.MinNumber.Is(Scanner.Parser.SpecialVolumeNumber);
        }
    }

}
