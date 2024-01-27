The Shadow DOM is a feature that isolates its own DOM tree from the main document DOM, often used with Javascript and custom elements to create what is known as Web Components.
Of course this is a simplification but it gives the general idea.

While Shadow DOM and it's styling isolation features is a great thing, it didn't play well with server side rendering as you'd render the custom element tag but the enhancing of the tag would happen on the client side, which would cause a flash of unstyled content (FOUC).

Declarative Shadow DOM allows you to create these DOM boundaries in a declarative way, which means that the browser can render the content in the right order and you don't have to worry about the FOUC.

Clients can still enhance the elements produced with Declarative Shadow DOM, but it is not required. From a backend's perspective, it is just a DOM tree with scoped styling.

You can learn more about Declarative Shadow DOM in this piece from the Chrome for Developers website: https://developer.chrome.com/docs/css-ui/declarative-shadow-dom

An elemnt with Declarative Shadow DOM looks like the following

```html
<tag-name>
  <template shadowrootmode="open">
    <style>
      /* Scoped styles */
    </style>
    <link rel="stylesheet" href="/also-scoped-styles.css" />
    <!-- Content -->
    <p>I'm on the shadow DOM, my styles are scoped</p>
  </template>
</tag-name>
```

Anything inside the `<template>` tag will be scoped and considered part of the Shadow DOM.
Stylesheets and styles inside the `<template>` tag will be scoped to the Shadow DOM.
The browsers know how to reliably render the content inside and avoid loading the stylesheets multiple times.

You can also use slots to include "Light" DOM elements into the Shadow DOM in a concept known as content projection. This is a useful technique when you want to have "shell-like" elements like "cards" or "panels" that can be customized with all kinds of content.

```html
<tag-name>
  <template shadowrootmode="open">
    <style>
      /* Scoped styles */
    </style>
    <!-- Content -->
    <!-- Slots are not required
         but offer a way to include "Light" DOM
         elements into the Shadow DOM -->
    <p>I'm on the shadow DOM, my styles are scoped</p>

    <slot></slot>

    <slot name="footer"></slot>
  </template>

  <p>I'm on the light DOM my styles aren't scoped</p>
  <footer slot="footer">
    <p>I'm on the light DOM but I'm in the footer slot</p>
  </footer>
</tag-name>
```

In the cases above, the `<tag-name>` is not a custom element, as that requires to be registered in the Javascript custom elements registry. It is just a container element.
There are also some built-in elements that support Declarative Shadow DOM, like `<details>` and `<summary>`, so you can use it without having to create new tags if you don't need to.

## Sh and Shcs

Having all of that said, Hox provides some ways to ease the creation of Declarative Shadow DOM elements.

the `sh` function and the `shcs` for C#/VB devs, allow you to create a factory function will use the provided initial template and will append the nodes to the right place.

```fsharp

let myElement =
    sh("my-element",
       fragment(
        h("style", raw "p { color: red; }"),
        h("article", h("p", "I'm on the shadow DOM, my styles are scoped")),
       )
    )
```

That will produce the following html

```html
<my-element>
  <template shadowrootmode="open">
    <style>
      p {
        color: red;
      }
    </style>
    <article>
      <p>I'm on the shadow DOM, my styles are scoped</p>
    </article>
  </template>
</my-element>
```

For cases where you want to use slots, things are sliglty more complicated, as you have to create the template with slots and then assign the new content outside the template tag, whis can be cumbersome, so Hox provides an overload that takes the initial template and then gets you a factory to enable shared content.

```fsharp
let myPanel =
    sh("my-panel",
       fragment(
        h("link[rel='stylesheet'][href='/my-panel.css']"),
        h("article",
          h("header", h("slot[name=panel-header]"))
          h("section", h "slot")
        )
       )
    )
// later on
let firstPanel = myPanel(
    h("h3[slot=panel-header]", "My Panel"),
    h("p", "I'm on the section of the panel")
)
// the factory function enable a "component-like" API
// where styles are scoped to that component
let secondPanel = myPanel(
    h("h4[slot=panel-header]", "Another Panel"),
    h("p", "I'm on the section of the panel")
)
```

That will produce the following html

```html
<my-panel>
  <template shadowrootmode="open">
    <link rel="stylesheet" href="/my-panel.css" />
    <article>
      <header>
        <slot name="panel-header"></slot>
      </header>
      <section>
        <slot></slot>
      </section>
    </article>
  </template>
  <h3 slot="panel-header">My Panel</h3>
  <p>I'm on the section of the panel</p>
</my-panel>
<!-- And on the second function call -->
<my-panel>
  <template shadowrootmode="open">
    <link rel="stylesheet" href="/my-panel.css" />
    <article>
      <header>
        <slot name="panel-header"></slot>
      </header>
      <section>
        <slot></slot>
      </section>
    </article>
  </template>
  <h4 slot="panel-header">Another Panel</h4>
  <p>I'm on the section of the panel</p>
</my-panel>
```

Traditionally F# devs would archive this kind of composition by using functions that take the content as parameters and fill the wholes defined in the templates they produce, however this approach is not friendly to scoping or requires complicated setups to enable scoping, here we're leveraging the browser's built-in support for Declarative Shadow DOM to enable this kind of composition.

For the C#/VB devs, the story is quite similar

```csharp
var myElement =
    shcs("my-element",
       fragment(
        h("style", raw "p { color: red; }"),
        h("article",
          h("nav", h("slot[name=navigation]")),
          h("section", h "slot")
        )
       )
    );

var element = myElement(
  h("h3[slot=navigation]", "My Panel"),
  h("p", "I'm on the section of the panel")
);
```

In this case `shcs` returns a more friendly `Func<Node, Node>` that doesn't involve FuncConvert to F# functions.

## Built-in elements

For the tags that support Declarative Shadow DOM, Hox provides a set of functions that will create the right template for you.

A list of such functions is the following:

- article
- aside
- blockquote
- body
- div
- footer
- h1
- h2
- h3
- h4
- h5
- h6
- header
- main
- nav
- p
- section
- span

To create an scoped `article` element you can do the following

```fsharp
open type ScopableElements


h("body",
  h("article", h("p", "I'm on the light DOM, my styles aren't scoped")),
  article(
    h("style", raw "p { color: red; }"),
    h("p", "I'm on the shadow DOM, my styles are scoped")
  )
)
```

> **_Note_**: If you need to create a built-in element that supports slots, then you have to use the `sh` function factory instead.
