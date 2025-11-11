using System.Collections.ObjectModel;

namespace Shared.UnitTests;

public class CollectionHelpersTests
{
    [Fact]
    public void MergesProperly()
    {
        var ob = new ObservableCollection<string>() { "a", "b", "c" };
    }
}
