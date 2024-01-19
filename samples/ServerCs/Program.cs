using Htmelo;
using static Htmelo.DSL;
using static Htmelo.DSL.NodeExtensions;
using static Htmelo.DSL.Htmelo;
using static Htmelo.Rendering;


var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", async (HttpContext ctx) =>
{

    var node = Layout(el("style", raw("h1 { color: red; }")))
    .children(
        el("h1", text("Hello World!")),
        el("p", text("This is a paragraph."))
    );

    await ctx.StreamView(node);
});

app.Run();

static Node Layout(Node? head = null, Node? scripts = null, params Node[] children) =>
    el(
      "html[lang=en].sl-theme-light",
      el(
        "head",
        el("meta[charset=utf-8]"),
        el("meta[name=viewport][content=width=device-width, initial-scale=1.0]"),
        el("title", text("Htmelo")),
        el("link[rel=stylesheet]")
          .href(
            "https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/light.css"
          )
          .media("(prefers-color-scheme:light)"),
        el("link[rel=stylesheet]")
          .href(
            "https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/dark.css"
          )
          .media("(prefers-color-scheme:dark)")
          .custom(
            "onload",
            "document.documentElement.classList.add('sl-theme-dark');"
          ),
        head ?? fragment([])
      ),
      el(
        "body",
        fragment(children),
        el("script[type=module]")
          .src(
            "https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/shoelace-autoloader.js"
          ),
        scripts ?? fragment([])
      )
    );


static class HttpContextExtensions
{
    public static async Task StreamView(this HttpContext context, Node node)
    {
        using var writer = new StreamWriter(context.Response.Body, System.Text.Encoding.UTF8);
        context.Response.ContentType = "text/html; charset=utf-8";
        await foreach (var item in Chunked.render(node, context.RequestAborted))
        {
            await writer.WriteAsync(item);
            await writer.FlushAsync();
        }
    }

}
