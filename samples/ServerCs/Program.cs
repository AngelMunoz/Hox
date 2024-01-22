using Hox.Core;

using static Hox.NodeBuilder;
using static Hox.Rendering;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging();
var app = builder.Build();

app.MapGet("/", () =>
{
  var node =
      Layout.Default(
        h("h1", text("Hello World!")),
        h("p", text("This is a paragraph."))
      );

  return AppResults.HoxView(node);
});

app.MapGet("/string", () =>
{
    var node =
      Layout.Default(
        h("h1", text("Hello World!")),
        h("p", text("This is a paragraph."))
      );
    return AppResults.HoxString(node);
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
        h("title", text("Hox")),
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

static class AppResults
{
  public static IResult HoxView(this Node node) => new HoxStreamedResult(node);

  public static IResult HoxString(this Node node) => new HoxStringResult(node);


  class HoxStreamedResult(Node node) : IResult
  {

    public async Task ExecuteAsync(HttpContext httpContext)
    {
      httpContext.Response.ContentType = "text/html; charset=utf-8";
      await httpContext.Response.StartAsync(httpContext.RequestAborted);
      await Render.ToStream(node, httpContext.Response.Body, cancellationToken: httpContext.RequestAborted);
      await httpContext.Response.CompleteAsync();
    }
  }

  class HoxStringResult(Node node) : IResult
  {

    public async Task ExecuteAsync(HttpContext httpContext)
    {
      httpContext.Response.ContentType = "text/html; charset=utf-8";
      await httpContext.Response.StartAsync(httpContext.RequestAborted);
      var result = await Render.AsString(node, httpContext.RequestAborted);
      await httpContext.Response.WriteAsync(result, httpContext.RequestAborted);
      await httpContext.Response.CompleteAsync();
    }
  }
}
