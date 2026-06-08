namespace pg_protoexport;

/// <summary>
/// State of the generation process, with properties to track the current state of the generation
/// </summary>
/// <param name="standalone"></param>
/// <param name="multiple"></param>
public class GenerationState(bool standalone, bool multiple = false)
{
    /// <summary>
    /// Last message processed by the generator
    /// </summary>
    public PostgresMessageBase? LastMessage { get; set; } = null;

    /// <summary>
    /// When processing a DataRow message, this property will contain the number of rows previously processed.
    /// This is used to add a 'skippedwords' bytefield. Maximum rows before a skipped rows is triggered can be set in <see cref="GenerationOptions.MaxDataRows"/>
    /// </summary>
    public int ConsecutiveDataRows { get; set; } = 0;

    /// <summary>
    /// When set to true, the generator will generate a standalone document class
    /// </summary>
    public bool Standalone { get; } = standalone;

    /// <summary>
    /// When set to true, the generator will generate multiple documents, one per packet
    /// </summary>
    public bool Multiple { get; } = multiple;

    /// <summary>
    /// Tracks the number of rows in the current latex document (for page breaks)
    /// </summary>
    public float LatexRowCount { get; internal set; }

    /// <summary>
    /// Active render options for this export. Set by <see cref="PcapToLatexService"/> at the start of
    /// each export call and read by every template via its params object.
    /// </summary>
    public LatexRenderOptions Render { get; internal set; } = LatexRenderOptions.Default;

    /// <summary>
    /// Tracks the number of messages processed for all packets
    /// </summary>
    public int StatsMessagesProcessed { get; internal set; } = 0;

    /// <summary>
    /// Tracks the number of invalid messages processed for all packets
    /// </summary>
    public int StatsMessagesInvalid { get; internal set; } = 0;

    /// <summary>
    /// Tracks the number of packets processed
    /// </summary>
    public int StatsPacketsProcessed { get; internal set; } = 0;
}
