using NetEscapades.EnumGenerators;

namespace SourceGenerators.Test;

[TestClass]
public class EnumGeneratorTests
{
    [DataTestMethod]
    [DataRow(Colour.Red)]
    [DataRow(Colour.Green)]
    [DataRow(Colour.Blue)]
    public void ToStringFastWorks(Colour col)
    {
        Assert.AreEqual(col.ToString(), col.ToStringFast());
    }
}
