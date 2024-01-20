using Htmelo.Core;
using static Htmelo.NodeBuilder;
using static Htmelo.Rendering;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging();
var app = builder.Build();

app.MapGet("/", async (HttpContext ctx) =>
{
  var node =
      Layout.Default(
        h("h1", text("Hello World!")),
        h("p", text("This is a paragraph."))
      );

  // return await ctx.RenderView(node);
  await ctx.StreamView(node);
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


static class HttpContextExtensions
{
  public static async Task<IResult> RenderView(this HttpContext ctx, Node node)
  {
    var content = await Builder.ValueTask.render(node, ctx.RequestAborted);

    return Results.Text(content, "text/html; charset=utf-8");
  }

  public static async Task StreamView(this HttpContext context, Node node)
  {
    context.Response.ContentType = "text/html; charset=utf-8";
    await foreach (var item in Chunked.render(node, context.RequestAborted))
    {
      await context.Response.BodyWriter.WriteAsync(new ReadOnlyMemory<byte>(System.Text.Encoding.UTF8.GetBytes(item)), context.RequestAborted);
      await context.Response.BodyWriter.FlushAsync(context.RequestAborted);
    }
  }
}
