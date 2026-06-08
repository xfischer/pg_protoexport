namespace pg_protoexport.Templates;

// Provides the ITextTransformer.Render property on every template partial class in one place.
// Each template's main partial is in <Name>.Params.cs; this file adds the Render auto-property
// so the service can set the active LatexRenderOptions on the transformer instance after
// construction without each .Params.cs having to declare the property itself.

public partial class AuthenticationGeneric { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class BackendKeyData       { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class Bind                 { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class BindComplete         { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class CommandComplete      { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class DataRow              { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class Describe             { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class ErrorResponse        { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class Execute              { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class NoData               { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class NoticeResponse       { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class ParameterDescription { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class ParameterStatus      { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class Parse                { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class ParseComplete        { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class Query                { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class ReadyForQuery        { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class RowDescription       { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class SASLInitialResponse  { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class SASLResponse         { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class SkippedWords         { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class SSLRequest           { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class SSLResponse          { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class GSSENCRequest        { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class GSSENCResponse       { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class CancelRequest        { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class CopyInResponse       { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class CopyOutResponse      { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class CopyBothResponse     { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class CopyData             { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class CopyDone             { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class CopyFail             { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class StartupMessage       { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class Sync                 { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class Terminate            { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class Unknown              { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
public partial class Header               { public LatexRenderOptions Render { get; set; } = LatexRenderOptions.Default; }
