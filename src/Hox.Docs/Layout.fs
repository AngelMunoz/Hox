namespace Hox.Docs

open System

open Hox
open Hox.Core

open Hox.Docs.Toc
open type Hox.ScopableElements

module Views =

  let tableOfContents toc =
    h(
      "ul",
      toc
      |> List.skip 2
      |> List.map(fun (entry: EntryMetadata) ->
        let fileUrl = entry.file.Replace(".md", ".html")
        h("li", h($"a[href=/{fileUrl}]", entry.title)))
    )

type Layout =

  static member inline Default
    (
      toc: EntryMetadata list,
      metadata: EntryMetadata,
      htmlBaseHref: string,
      [<ParamArray>] content: Node array
    ) =
    h(
      "html[lang=en]",
      h(
        "head",
        h("title", metadata.title),
        h "meta[name=viewport][content=width=device-width,initial-scale=1]",
        h "meta[property=og:site_name][content=Hox Documentation]",
        h $"meta[name=og:description][content={metadata.summary}]",
        h $"meta[property=og:title][content={metadata.title}]",
        h $"meta[property=og:type][content=website]",
        h "link[rel=stylesheet][href=/assets/styles.css]",
        h $"base[href={htmlBaseHref}]"
      ),
      h(
        "body",
        h(
          "nav",
          h(
            "ul",
            h("li", h("a[href=/]", "Home")),
            h("li", h("a[href=/about.html]", "About Hox")),
            h("li", h("a[href=/guides/general-usage.html]", "Documentation")),
            h("li", h("a[href=/reference/nodes.html]", "Reference"))
          )
        ),
        h("aside", Views.tableOfContents toc),
        main(
          h("link[rel=stylesheet][href=/assets/main.css]", h "slot"),
          content
        ),
        footer(
          h("link[rel=stylesheet][href=/assets/footer.css]", h "slot"),
          h(
            "p",
            h("a[href=https://github.com/AngelMunoz/Hox]", "Hox"),
            text
              " is an F# library for rendering HTML documents asynchronously."
          ),
          h("p", "Licensed under the MIT License."),
          h(
            "p",
            text "Documentation generated on ",
            h("time", DateTime.UtcNow.ToString("yyyy-MM-dd")),
            text "."
          )
        ),
        h(
          "script",
          // Declarative shadow DOM polyfill
          raw
            """if (!HTMLTemplateElement.prototype.hasOwnProperty('shadowRootMode')) {
  (function attachShadowRoots(root) {
    root.querySelectorAll("template[shadowrootmode]").forEach(template => {
      const mode = template.getAttribute("shadowrootmode");
      const shadowRoot = template.parentNode.attachShadow({ mode });
      shadowRoot.appendChild(template.content);
      template.remove();
      attachShadowRoots(shadowRoot);
    });
  })(document);
}"""
        )
      )
    )
