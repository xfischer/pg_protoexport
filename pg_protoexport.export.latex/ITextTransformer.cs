namespace pg_protoexport;

public interface ITextTransformer
{
    string TransformText();

    float EstimateBytefieldRowCount() => 1f;

    /// <summary>
    /// Render options for the current export call. Set by <see cref="PcapToLatexService"/> after
    /// constructing the transformer. Default is <see cref="LatexRenderOptions.Default"/>.
    /// </summary>
    LatexRenderOptions Render { get; set; }
}
