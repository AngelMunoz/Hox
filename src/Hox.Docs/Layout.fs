namespace Hox.Docs

open System

open Hox
open Hox.Core

open Hox.Docs.Toc
open type Hox.ScopableElements

module Views =

  let tableOfContents toc =
    let (categorized, uncategorized) =
      toc |> List.skip 2 |> List.partition(fun entry -> entry.category <> None)

    let categorized = List.groupBy (fun item -> item.category.Value) categorized

    h(
      "ul",
      fragment [
        for entry in uncategorized do
          let fileUrl = entry.file.Replace(".md", ".html")
          h("li", h($"a[href={fileUrl}]", entry.title))
        for (category, entries) in categorized do
          h(
            "li",
            h($"h4", category),
            h(
              "ul",
              fragment [
                for entry in entries do
                  let fileUrl = entry.file.Replace(".md", ".html")
                  h("li", h($"a[href={fileUrl}]", entry.title))
              ]
            )
          )

      ]
    )

  let importMap =
    h(
      "script[type=importmap]",
      raw
        """{
  "imports": {
    "hox": "/assets/script.js",
    "highlight.js": "https://ga.jspm.io/npm:highlight.js@11.7.0/es/index.js",
    "highlight.js/lib/core": "https://ga.jspm.io/npm:highlight.js@11.7.0/es/core.js",
    "highlight.js/lib/languages/fsharp": "https://ga.jspm.io/npm:highlight.js@11.7.0/es/languages/fsharp.js",
    "highlight.js/lib/languages/csharp": "https://ga.jspm.io/npm:highlight.js@11.7.0/es/languages/csharp.js",
    "highlight.js/lib/languages/vbnet": "https://ga.jspm.io/npm:highlight.js@11.7.0/es/languages/vbnet.js",
    "highlight.js/lib/languages/xml": "https://ga.jspm.io/npm:highlight.js@11.7.0/es/languages/xml.js",
     "highlight.js/lib/languages/plaintext": "https://ga.jspm.io/npm:highlight.js@11.7.0/es/languages/plaintext.js"
  }
}"""
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
        h(
          "style",
          raw
            """:root {
  --hox-font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto,
    Oxygen-Sans, Ubuntu, Cantarell, "Helvetica Neue", sans-serif;
  --hox-font-size: 16px;
  --hox-line-height: 1.5;
  --hox-font-color: #504c4c;
  --hox-background-color: #fffdf6;

  --hox-blockquote-bg-color: #eae6d6;
  --hox-card-background-color: var(--hox-background-color, #fff);
  --hox-card-color: var(--hox-font-color, #0f0f0f);

  --hox-card-border-radius: 0.25rem;
  --hox-card-box-shadow: 0 5px 5px 2px rgba(0, 0, 0, 0.2),
    0 4px 3px 0 rgba(0, 0, 0, 0.15);

  --anchor-color: #c79c4c;
  --anchor-color-hover: #ffdd9d;
  --anchor-color-visited: #946713;
  --anchor-color-active: #c79c4c;
  --anchor-color-focus: #966405;
}
@media (prefers-color-scheme: dark) {
  :root {
    --hox-font-color: #e8e6e3;
    --hox-background-color: #312614;
    --hox-blockquote-bg-color: #2c1d04;
    --hox-card-background-color: var(--hox-background-color, #0f0f0f);
    --hox-card-color: var(--hox-font-color, #e8e6e3);

    --anchor-color: #f0bf63;
    --anchor-color-hover: #ffdd9d;
    --anchor-color-visited: #be8315;
    --anchor-color-active: #e9bd6b;
    --anchor-color-focus: #df9f29;
    --hox-card-box-shadow: 0 5px 5px 4px rgba(199, 156, 76, 0.5),
      0 4px 4px 0 rgba(199, 156, 76, 0.4);
  }
}"""
        ),
        h "link[rel=stylesheet][href=/assets/styles.css]",
        h "link[rel=stylesheet][href=/assets/links.css]",
        (if metadata.file = "index.md" then
           h "link[rel=stylesheet][href=/assets/index.css]"
         else
           fragment []),
        h $"base[href={htmlBaseHref}]",
        h(
          "script[async=][src=https://ga.jspm.io/npm:es-module-shims@1.6.2/dist/es-module-shims.js][crossorigin=anonymouys]"
        ),
        Views.importMap
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
        (if metadata.file = "index.md" then
           fragment []
         else
           h("aside", Views.tableOfContents toc)),
        main(
          h "link[rel=stylesheet][href=/assets/main.css]",
          h "link[rel=stylesheet][href=/assets/links.css]",
          h "link[rel=stylesheet][href=/assets/index.css]",
          h "link[rel=stylesheet][href=/assets/highlight.css]",
          fragment content
        ),
        footer(
          fragment(
            h "link[rel=stylesheet][href=/assets/footer.css]",
            h "link[rel=stylesheet][href=/assets/links.css]"
          ),
          h(
            "p",
            h("a[href=https://github.com/AngelMunoz/Hox]", "Hox"),
            text
              " is an F# library for rendering HTML documents asynchronously."
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
        ),
        h(
          "script[type=module]",
          raw
            "import { highlightAll } from 'hox';

    document.addEventListener('DOMContentLoaded', () => {
        highlightAll();
    });"
        )
      )
    )
