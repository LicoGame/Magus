using ComparableGenerator;
using Generator.Equals;
using MemoryPack;

namespace Magus.Test.Internal
{
    [MemoryPackable, Equatable, Comparable]
    public partial class Another
    {
        [CompareBy]
        public int Id;
    }
}