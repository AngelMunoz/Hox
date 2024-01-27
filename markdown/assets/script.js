import hljs from "highlight.js/lib/core";
import text from "highlight.js/lib/languages/plaintext";
import fsharp from "highlight.js/lib/languages/fsharp";
import csharp from "highlight.js/lib/languages/csharp";
import vbnet from "highlight.js/lib/languages/vbnet";
import xml from "highlight.js/lib/languages/xml";

hljs.registerLanguage("", text);
hljs.registerLanguage("fsharp", fsharp);
hljs.registerLanguage("csharp", csharp);
hljs.registerLanguage("vb", vbnet);
hljs.registerLanguage("html", xml);

export function highlightAll() {
  const highligntElement = (element) => {
    if (!element) return;
    hljs.highlightElement(element);
  };
  document.querySelectorAll("pre code")?.forEach?.(highligntElement);
  document
    .querySelector("main")
    ?.shadowRoot?.querySelectorAll("pre code")
    ?.forEach?.(highligntElement);
}
