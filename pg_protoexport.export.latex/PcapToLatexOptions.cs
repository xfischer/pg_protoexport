namespace pg_protoexport;



public sealed class PcapToLatexOptions
{
    /// <summary>
    /// Maximum number of consecutive DataRow messages to render before inserting a "skipped rows" indicator.
    /// </summary>
    public int MaxDataRows { get; set; } = 1;

    /// <summary>
    /// Default value applied to <see cref="LatexRenderOptions.Exact"/> when a caller does not supply
    /// explicit render options. CLI flags override this.
    /// </summary>
    public bool DefaultExact { get; set; } = false;

    /// <summary>
    /// Default value applied to <see cref="LatexRenderOptions.RowWidthBytes"/> when a caller does not
    /// supply explicit render options. CLI flags override this.
    /// </summary>
    public int DefaultRowWidthBytes { get; set; } = 32;

    internal Func<PostgresMessageBase, ITextTransformer?>? CustomTemplateProvider;

    internal Func<string?, GenerationState, ITextTransformer?>? CustomHeaderProvider;
        
    /// <summary>
    /// Allows to add a custom template provider for a given <see cref="PostgresMessageBase"/>.
    /// </summary>
    /// <param name="customTemplateProvider">Delegate called to provide additionnal template for a given <see cref="PostgresMessageBase"/>.
    /// Implementers should return an <see cref="ITextTransformer"/> instance or <c>null</c> when message should be transformed using default transformer</param>
    /// <remarks>Any template returned by this function will take precedence over the default template.</remarks>
    public void AddTemplateProvider(Func<PostgresMessageBase, ITextTransformer?>? customTemplateProvider)
    {
        CustomTemplateProvider = customTemplateProvider;
    }

    /// <summary>
    /// Allows to add a custom header provider for a given message and <see cref="GenerationState"/>.
    /// </summary>
    /// <param name="customHeader"></param>
    public void AddCustomHeader(Func<string?, GenerationState, ITextTransformer?> customHeader)
    {
        CustomHeaderProvider = customHeader;
    }
}

