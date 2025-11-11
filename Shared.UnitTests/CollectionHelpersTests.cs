using System.Collections.ObjectModel;

namespace Shared.UnitTests;

public class CollectionHelpersTests
{
    [Fact]
    public void MergesProperly()
    {
        var ob = new ObservableCollection<string>() { "a", "b", "c" };
        var newValues = new List<string>() { "a", "x", "c", "d" };

        CollectionHelpers.UpdateObservableCollection(ob, newValues);

        Assert.Equal(4, ob.Count);
        Assert.Collection(ob,
            item => Assert.Equal("a", item),
            item => Assert.Equal("x", item),
            item => Assert.Equal("c", item),
            item => Assert.Equal("d", item)
        );
    }

    // Test populating an empty array works.
    [Fact]
    public void PopulatesEmptyCollectionProperly()
    {
        var ob = new ObservableCollection<string>();
        var newValues = new List<string>() { "a", "b", "c" };
        CollectionHelpers.UpdateObservableCollection(ob, newValues);
        Assert.Equal(3, ob.Count);
        Assert.Collection(ob,
            item => Assert.Equal("a", item),
            item => Assert.Equal("b", item),
            item => Assert.Equal("c", item)
        );
    }

    // Test emptying an array works.
    [Fact]
    public void EmptiesCollectionProperly()
    {
        var ob = new ObservableCollection<string>() { "a", "b", "c" };
        var newValues = new List<string>();
        CollectionHelpers.UpdateObservableCollection(ob, newValues);
        Assert.Empty(ob);
    }

    // Test that removing items works.
    [Fact]
    public void RemovesProperly()
    {
        var ob = new ObservableCollection<string>() { "a", "b", "c", "d" };
        var newValues = new List<string>() { "a", "c" };
        CollectionHelpers.UpdateObservableCollection(ob, newValues);
        Assert.Equal(2, ob.Count);
        Assert.Collection(ob,
            item => Assert.Equal("a", item),
            item => Assert.Equal("c", item)
        );
    }
}
