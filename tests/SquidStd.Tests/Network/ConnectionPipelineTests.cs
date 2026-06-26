using SquidStd.Network.Data;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Network;

public class ConnectionPipelineTests
{
    [Fact]
    public void Defaults_AreNull()
    {
        var pipeline = new ConnectionPipeline();

        Assert.Null(pipeline.Codec);
        Assert.Null(pipeline.Middlewares);
        Assert.Null(pipeline.Framer);
    }

    [Fact]
    public void Positional_SetsCodec()
    {
        var codec = new CountingXorCodec(1);

        var pipeline = new ConnectionPipeline(codec);

        Assert.Same(codec, pipeline.Codec);
    }
}
