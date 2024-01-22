using Hox.Core;

using System.Diagnostics;

using static Hox.NodeBuilder;
using static Hox.Rendering;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging();
var app = builder.Build();

app.MapGet("/", (HttpContext ctx) =>
{
    var node =
        Layout.Default(
          h("h1", text("Hello World!")),
          h("p", text("This is a paragraph."))
        );

    // return await ctx.RenderView(node);
    return node.ToStreamedResult();
});

app.Run();

static class Layout
{
    public record DefaultLayoutArgs(Node? Head, Node? Scripts);

    public static Node Default(DefaultLayoutArgs args, params Node[] children) =>
      h(
        "html[lang=en].sl-theme-light",
        h(
          "head",
          h("meta[charset=utf-8]"),
          h(@"meta[name=viewport]
                 [content=width=device-width, initial-scale=1.0]
            "),
          h("title", text("Htmelo")),
          h(@"link[rel=stylesheet]
                 [href=https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/light.css]
                 [media=(prefers-color-scheme:light)]"),
          h(@"link[rel=stylesheet]
                 [href=https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/dark.css]
                 [media=(prefers-color-scheme:dark)]
                 [onload=document.documentElement.classList.add('sl-theme-dark');]"),
          args.Head ?? fragment([])
        ),
        h(
          "body",
          fragment(children),
          h(@"script[type=module]
                   [src=https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/shoelace-autoloader.js]"),
          args.Scripts ?? fragment([])
        )
      );

    public static Node Default(params Node[] children) =>
      Default(new DefaultLayoutArgs(null, null), children);
}

public static class ResultsExtensions
{
    public static IResult ToStreamedResult(this Node node) => new HtmeloStreamedResult(node);
    public static async Task<IResult> ToTextResult(this Node node, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await Builder.ValueTask.render(node, cancellationToken);
            return Results.Text(result, "text/html; charset=utf-8");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return Results.Problem("Server Error", statusCode: 500);
        }
    }
}

public class HtmeloStreamedResult(Node node) : IResult
{

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = "text/html; charset=utf-8";
        await httpContext.Response.StartAsync(httpContext.RequestAborted);
        await foreach (var item in Chunked.render(node, httpContext.RequestAborted))
        {
            await httpContext.Response.BodyWriter.WriteAsync(new ReadOnlyMemory<byte>(System.Text.Encoding.UTF8.GetBytes(item)), httpContext.RequestAborted);
            await httpContext.Response.BodyWriter.FlushAsync(httpContext.RequestAborted);
        }
        await httpContext.Response.CompleteAsync();
    }
}
