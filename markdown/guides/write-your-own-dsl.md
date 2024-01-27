In case that neither Hox or Feliz fit your needs, you can always write your own DSL.

This can be accomplished in any dotnet language using the functions in the `Hox.NodeOps` module.

This module exposes two simple functions

- `addToNode`
- `AddAttribute`

These functions already contain all the functionality required to compose nodes together, regardless of the underlyng type of node.

The following example shows how to create a more traditiona F# DSL with the two list style.

```fsharp
open Hox
open Hox.Core

let createElement name attributes children =
    Element {
        tag = name
        attributes = attributes
        children = children
    }

let createAttribute name value =
    Attribute {
        name = name
        value = value
    }


module Elem =
    let inline html attributes children =
        createElement "html" attributes children
    let inline head attributes children =
        createElement "head" attributes children
    let inline body attributes children =
    // the same can be done with Task<Node>
    let inline async attributes (node: Async<Node>) =
      AsyncNode(cancellableValueTask {
        let! token = CancellableValueTask.getCancellationToken()
        if token.IsCancellationRequested then
          return Fragment []
        else
          let! node = node
        return
          attributes
          |> List.fold
              (fun node attribute -> addAttribute(node, attribute))
              node
      })

module Attr =
  let inline lang value =
    createAttribute "lang" value

  let inline title value =
    createAttribute "title" value

  let inline class' value =
    createAttribute "class" value

  let inline classAsync value =
    AsyncAttribute(cancellableValueTask {
      let! token = CancellableValueTask.getCancellationToken()
      if token.IsCancellationRequested then
        // attributes without a name are ignored
        return createAttribute "" ""
      else
        return createAttribute "class" value
    })


Elem.html [ lang "en" ] [
  Elem.head [] [
    Elem.title [] [
      Text "Hello World"
    ]
  ]
  Elem.body [ class' "container" ] [
    Elem.h1 [] [
      Text "Hello World"
    ]
    Elem.p [] [
      Text "This is a paragraph"
    ]
    Elem.async
      [ classAsync(async {
          do! Async.Sleep(1000)
          return "container"
        })
      ]
      (async {
        do! Async.Sleep(1000)
        return Elem.p [] [
          Text "This is a paragraph"
        ]
      })
  ]
```

For simplicity the operators `<+` to add child nodes and `<+.` to add attributes are available in the `Hox.NodeOps.Operators` module.

## C# & VB.NET

C# users can leverage extension methods to create a more fluent DSL however, due to the differences in the type system with F#, the DSL will have to use the Hox DSL functions, instead of the core types. Also, keep in mind that the DSL's are often easier made in F# so you might want to consider using F# for your DSL, however if you identify an opportunity to improve the APIs for C# users, please feel free to raise an issue.

Let's imagine we want a strongly typed DSL with a more C# feel or flavor.

```csharp
// our goal is to have something like this

var content =
  new Form()
    .Class("container")
    .Children(
      new Input()
        .Type("text")
        .Placeholder("Type yuout name here"),
      h("br"), // we can still use the existing functions
      h("section.row.space-evenly", // in case we're consuming existing libraries
        new Button() // or we're still working in the DSL
          .Type("submit")
          .Value("Submit"),
        new Button()
          .Type("reset")
          .Value("Submit")
      )
    );

var result = await Render.AsString(content);
```

That looks quite C#'ish, but how do we get there?

First we need to create a base class for all our elements Keep in mind that these BaseElement and derived types will be just wrappers to the Hox.Node type, so we might want to use structs instead of classes but I'll leave that for the performance experts to decide.

```csharp
public record BaseElement(Node Node)
{
  // And also let's define an implicit operator to convert our elements to nodes
  // so we can call the Render functions on them like we did above
  // keep in mind this is is not required but it is just more ergonomic and convenient.
  public static implicit operator Node(BaseElement element) => element.Node;

  // this implicit conversion will allows us to combine our existing h function
  // with our new BaseElement type
  public static implicit operator BaseElement(Node node) => new(node);

}
```

Our records need to inherit from the BaseElement class so they can share common attributes and methods like Class, Id, Style, Children, etc.

```csharp
public record Input : BaseElement
{
  public Input() : base(h("input")) { }
}

public record Button : BaseElement
{
  public Button() : base(h("button")) { }
}

public record Div : BaseElement
{
  public Div() : base(h("div")) { }
}
```

Noe we use extension methods with constrains to the BaseElement to add the required functionality please note that we're not modifying the original element but instead we're creating a new one with the updated node This can help us to keep our code safe from unexpected changes, it also signals to consumers that they should not modify the original element.

```csharp
public static class BaseElementExtensions
{
  public static T Class<T>(this T element, string className) where T : BaseElement =>
    element with { Node = NodeOps.addAttribute(element.Node, attribute("class", className)) };

  public static T Id<T>(this T element, string id) where T : BaseElement =>
    // also keep in mind that we're mixing and matchin the existing functions within Hox and Hox.NodeOps
    // so if you feel there's something missing to make your DSL more ergonomic, feel free
    // to raise an issue to make sure it gets visibility.
    element with { Node = NodeOps.addAttribute(element.Node, attribute("id", id)) };

  public static T Style<T>(this T element, string style) where T : BaseElement =>
    element with { Node = NodeOps.addAttribute(element.Node, attribute("style", style)) };

  public static T Children<T>(this T element, params BaseElement[] children) where T : BaseElement =>
    element with
    {
      Node = NodeOps.addToNode(
        element.Node,
        fragment(children.Select(el => el.Node))
      )
    };
}

static class InputExtensions
{
  public static T Type<T>(this T input, string type) where T : Input =>
    input with { Node = NodeOps.addAttribute(input.Node, attribute("type", type)) };

  public static T Value<T>(this T input, string value) where T : Input =>
    input with { Node = NodeOps.addAttribute(input.Node, attribute("value", value)) };

  public static T Placeholder<T>(this T input, string placeholder) where T : Input =>
    input with { Node = NodeOps.addAttribute(input.Node, attribute("placeholder", placeholder)) };

}

static class ButtonExtensions
{
  public static T Type<T>(this T button, string type) where T : Button =>
    button with { Node = NodeOps.addAttribute(button.Node, attribute("type", type)) };

  public static T Value<T>(this T button, string value) where T : Button =>
    button with { Node = NodeOps.addAttribute(button.Node, attribute("value", value)) };

  public static T Placeholder<T>(this T button, string placeholder) where T : Button =>
    button with { Node = NodeOps.addAttribute(button.Node, attribute("placeholder", placeholder)) };

}
```

And that's how we can create a strongly typed, C# friendly DSL.

Our C# DSL could also be improved upon by using strongly typed attributes rather than strings e.g.

```csharp
public enum InputType
{
  Text,
  Number,
  Email,
  Password,
  ...
}

public static T Type<T>(this T input, InputType type) where T : Input =>
  input with { Node = NodeOps.addAttribute(input.Node, attribute("type", type.ToString().ToLowerInvariant())) };
```
