// A per-entity collection object held by a class; BOR0701 inspects structs only.
using System.Collections.Generic;
namespace Borough.Core.ReviewProbes;
internal sealed class ReviewProbe_ManagedClassState
{
    private readonly List<int>[] _perEntity = new List<int>[1024];
    internal void Add(int entity, int value) => (_perEntity[entity] ??= new List<int>()).Add(value);
}
