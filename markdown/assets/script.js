import hljs from "highlight.js/lib/core";
import text from "highlight.js/lib/languages/plaintext";
import fsharp from "highlight.js/lib/languages/fsharp";
import bash from "highlight.js/lib/languages/bash";
import xml from "highlight.js/lib/languages/xml";

hljs.registerLanguage("", text);
hljs.registerLanguage("fsharp", fsharp);
hljs.registerLanguage("bash", bash);
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
