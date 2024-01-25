module ChunkRender

open System
open System.Threading.Tasks

open Xunit
open FSharp.Control
open IcedTasks

open Hox.Rendering
open Hox.Core

[<Fact>]
let ``Render can render an element``() = taskUnit {

  let actual = ResizeArray()

  for chunk in
    Render.start(
      Element {
        tag = "div"
        attributes = []
        children = []
      }
    ) do
    actual.Add(chunk)

  let expected = [ "<div"; ">"; "</div>" ]

  Seq.zip expected actual |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))

}

[<Fact>]
let ``Render can render an element with attributes``() = taskUnit {

  let actual = ResizeArray()

  for chunk in
    Render.start(
      Element {
        tag = "div"
        attributes = [ Attribute { name = "class"; value = "foo" } ]
        children = []
      }
    ) do
    actual.Add(chunk)

  let expected = [ "<div"; " class=\""; "foo"; "\""; ">"; "</div>" ]

  actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))

}

[<Fact>]
let ``Render can render an element with children``() = taskUnit {

  let actual = ResizeArray()

  for chunk in
    Render.start(
      Element {
        tag = "div"
        attributes = []
        children = [
          Element {
            tag = "span"
            attributes = []
            children = []
          }
        ]
      }
    ) do
    actual.Add(chunk)

  let expected = [ "<div"; ">"; "<span"; ">"; "</span>"; "</div>" ]

  actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))

}

[<Fact>]
let ``Render can render an element with children and attributes``() = taskUnit {

  let actual = ResizeArray()

  for chunk in
    Render.start(
      Element {
        tag = "div"
        attributes = [ Attribute { name = "class"; value = "foo" } ]
        children = [
          Element {
            tag = "span"
            attributes = []
            children = []
          }
        ]
      }
    ) do
    actual.Add(chunk)

  let expected = [
    "<div"
    " class=\""
    "foo"
    "\""
    ">"
    "<span"
    ">"
    "</span>"
    "</div>"
  ]

  actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))

}

[<Fact>]
let ``Render can render an element with children and attributes and text``() = taskUnit {

  let actual = ResizeArray()

  for chunk in
    Render.start(
      Element {
        tag = "div"
        attributes = [ Attribute { name = "class"; value = "foo" } ]
        children = [
          Element {
            tag = "span"
            attributes = []
            children = [ Text "Hello" ]
          }
        ]
      }
    ) do
    actual.Add(chunk)

  let expected = [
    "<div"
    " class=\""
    "foo"
    "\""
    ">"
    "<span"
    ">"
    "Hello"
    "</span>"
    "</div>"
  ]

  actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))

}

[<Fact>]
let ``Render can render an element with children and attributes and text and a fragment``
  ()
  =
  taskUnit {

    let actual = ResizeArray()

    for chunk in
      Render.start(
        Element {
          tag = "div"
          attributes = [ Attribute { name = "class"; value = "foo" } ]
          children = [
            Element {
              tag = "span"
              attributes = []
              children = [ Text "Hello" ]
            }
            Fragment [
              Element {
                tag = "span"
                attributes = []
                children = [ Text "World" ]
              }
            ]
          ]
        }
      ) do
      actual.Add(chunk)

    let expected = [
      "<div"
      " class=\""
      "foo"
      "\""
      ">"
      "<span"
      ">"
      "Hello"
      "</span>"
      "<span"
      ">"
      "World"
      "</span>"
      "</div>"
    ]

    actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))

  }

[<Fact>]
let ``Render can render an element with async nodes and async attributes``() = taskUnit {
  let actual = ResizeArray()

  for chunk in
    Render.start(
      Element {
        tag = "div"
        attributes = [
          AsyncAttribute(
            cancellableValueTask { return { name = "class"; value = "foo" } }
          )
          AsyncAttribute(
            cancellableValueTask { return { name = "class"; value = "fa" } }
          )
        ]
        children = [
          AsyncNode(
            cancellableValueTask {
              return
                Element {
                  tag = "span"
                  attributes = []
                  children = []
                }
            }
          )
        ]
      }
    ) do
    actual.Add(chunk)

  let expected = [
    "<div"
    " class=\""
    "foo fa"
    "\""
    ">"
    "<span"
    ">"
    "</span>"
    "</div>"
  ]

  actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))
}

[<Fact>]
let ``Render can respect id, class, attribute order``() = taskUnit {
  let actual = ResizeArray()

  for chunk in
    Render.start(
      Element {
        tag = "div"
        attributes = [
          Attribute { name = "id"; value = "foo" }
          Attribute { name = "class"; value = "bar" }
          Attribute { name = "class"; value = "baz" }
          Attribute { name = "class"; value = "qux" }
          Attribute { name = "class"; value = "quux" }
          Attribute { name = "data-foo"; value = "bar" }
          Attribute { name = "data-bar"; value = "baz" }
        ]
        children = []
      }
    ) do
    actual.Add(chunk)

  let expected = [
    "<div"
    " id=\"foo\""
    " class=\""
    "bar baz qux quux"
    "\""
    " data-foo=\"bar\""
    " data-bar=\"baz\""
    ">"
    "</div>"
  ]

  actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))

}

[<Fact>]
let ``Render can render an element with children and attributes and text and a fragment and async nodes and async attributes``
  ()
  =
  taskUnit {

    let actual = ResizeArray()

    for chunk in
      Render.start(
        Element {
          tag = "div"
          attributes = [
            Attribute { name = "id"; value = "foo" }
            Attribute { name = "class"; value = "bar" }
            Attribute { name = "class"; value = "baz" }
            Attribute { name = "class"; value = "qux" }
            Attribute { name = "class"; value = "quux" }
            Attribute { name = "data-foo"; value = "bar" }
            Attribute { name = "data-bar"; value = "baz" }
            AsyncAttribute(
              cancellableValueTask { return { name = "class"; value = "foo" } }
            )
            AsyncAttribute(
              cancellableValueTask { return { name = "class"; value = "fa" } }
            )
          ]
          children = [
            Element {
              tag = "span"
              attributes = []
              children = [ Text "Hello" ]
            }
            Fragment [
              Element {
                tag = "span"
                attributes = []
                children = [ Text "World" ]
              }
            ]
            AsyncNode(
              cancellableValueTask {
                return
                  Element {
                    tag = "span"
                    attributes = []
                    children = [ Text "Hello" ]
                  }
              }
            )
          ]
        }
      ) do
      actual.Add(chunk)

    let expected = [
      "<div"
      " id=\"foo\""
      " class=\""
      "bar baz qux quux foo fa"
      "\""
      " data-foo=\"bar\""
      " data-bar=\"baz\""
      ">"
      "<span"
      ">"
      "Hello"
      "</span>"
      "<span"
      ">"
      "World"
      "</span>"
      "<span"
      ">"
      "Hello"
      "</span>"
      "</div>"
    ]

    actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))

  }

[<Fact>]
let ``Render discards any "id" attribute after the first``() = taskUnit {
  let actual = ResizeArray()

  for chunk in
    Render.start(
      Element {
        tag = "div"
        attributes = [
          Attribute { name = "id"; value = "foo" }
          Attribute { name = "id"; value = "bar" }
        ]
        children = []
      }
    ) do
    actual.Add(chunk)

  let expected = [ "<div"; " id=\"foo\""; ">" ]

  actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))
}

[<Fact>]
let ``Render discards any async "id" after the first``() = taskUnit {
  let actual = ResizeArray()

  for chunk in
    Render.start(
      Element {
        tag = "div"
        attributes = [
          Attribute { name = "id"; value = "foo" }
          AsyncAttribute(
            cancellableValueTask { return { name = "id"; value = "bar" } }
          )
        ]
        children = []
      }
    ) do
    actual.Add(chunk)

  let expected = [ "<div"; " id=\"foo\""; ">" ]

  actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))
}

[<Fact>]
let ``Render discards any "id" after the first async attribute``() = taskUnit {
  let actual = ResizeArray()

  for chunk in
    Render.start(
      Element {
        tag = "div"
        attributes = [
          AsyncAttribute(
            cancellableValueTask { return { name = "id"; value = "foo" } }
          )
          Attribute { name = "id"; value = "bar" }
        ]
        children = []
      }
    ) do
    actual.Add(chunk)

  let expected = [ "<div"; " id=\"foo\""; ">" ]

  actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))
}

[<Fact>]
let ``Render can render text nodes``() = taskUnit {
  let actual = ResizeArray()

  for chunk in Render.start(Text "Hello, world!") do
    actual.Add(chunk)

  let expected = [ "Hello, world!" ]

  actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))
}

[<Fact>]
let ``Render will HTML encode a text node``() = taskUnit {
  let actual = ResizeArray()

  for chunk in Render.start(Text "<script>") do
    actual.Add(chunk)

  let expected = [ "&lt;script&gt;" ]

  actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))
}

[<Fact>]
let ``Render can render a raw node``() = taskUnit {
  let actual = ResizeArray()

  for chunk in Render.start(Raw "<script>") do
    actual.Add(chunk)

  let expected = [ "<script>" ]

  actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))
}

[<Fact>]
let ``Render can render a comment node``() = taskUnit {
  let actual = ResizeArray()

  for chunk in Render.start(Comment "Hello, world!") do
    actual.Add(chunk)

  let expected = [ "<!--Hello, world!-->" ]

  actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))
}

[<Fact>]
let ``Render can render a fragment node``() = taskUnit {
  let actual = ResizeArray()

  for chunk in Render.start(Fragment [ Text "Hello, "; Text "world!" ]) do
    actual.Add(chunk)

  let expected = [ "Hello, "; "world!" ]

  actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))
}

[<Fact>]
let ``Render can render an Async Node``() = taskUnit {
  let actual = ResizeArray()

  for chunk in
    Render.start(
      AsyncNode(
        cancellableValueTask {
          do! Task.Delay(5)
          return Text "Hello, world!"
        }
      )
    ) do
    actual.Add(chunk)

  let expected = [ "Hello, world!" ]

  actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))
}

[<Fact>]
let ``Render can render an Async Seq Node``() = taskUnit {
  let actual = ResizeArray()

  for chunk in
    Render.start(
      AsyncSeqNode(
        taskSeq {
          do! Task.Delay(5)
          Text "Hello, "
          do! Task.Delay(5)
          Text "world!"
        }
      )
    ) do
    actual.Add(chunk)

  let expected = [ "Hello, "; "world!" ]

  actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))
}

[<Fact>]
let ``Render can render an Async Seq Node with a fragment``() = taskUnit {
  let actual = ResizeArray()

  for chunk in
    Render.start(
      AsyncSeqNode(
        taskSeq {
          do! Task.Delay(5)
          Text "Hello, "
          do! Task.Delay(5)
          Text "world!"
          do! Task.Delay(5)
          Fragment [ Text "Hello, "; Text "world!" ]
        }
      )
    ) do
    actual.Add(chunk)

  let expected = [ "Hello, "; "world!"; "Hello, "; "world!" ]

  actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))
}

[<Fact>]
let ``Render can render an Async Seq Node with a fragment and an async node``
  ()
  =
  taskUnit {

    let actual = ResizeArray()

    for chunk in
      Render.start(
        AsyncSeqNode(
          taskSeq {
            do! Task.Delay(5)
            Text "Hello, "
            do! Task.Delay(5)
            Text "world!"
            do! Task.Delay(5)
            Fragment [ Text "Hello, "; Text "world!" ]
            do! Task.Delay(5)

            AsyncNode(
              cancellableValueTask {
                do! Task.Delay(5)
                return Text "Hello, world!"
              }
            )
          }
        )
      ) do
      actual.Add(chunk)

    let expected = [ "Hello, "; "world!"; "Hello, "; "world!"; "Hello, world!" ]

    actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))

  }

[<Fact>]
let ``Render can render a mix of sync/async nodes``() = taskUnit {
  let actual = ResizeArray()

  for chunk in
    Render.start(
      Fragment [
        Text "Hello, "
        AsyncNode(
          cancellableValueTask {
            do! Task.Delay(5)
            return Text "world!"
          }
        )
      ]
    ) do
    actual.Add(chunk)

  let expected = [ "Hello, "; "world!" ]

  actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))
}

[<Fact>]
let ``Render can render a mix of sync/async nodes with a fragment``() = taskUnit {
  let actual = ResizeArray()

  for chunk in
    Render.start(
      Fragment [
        Text "Hello, "
        AsyncNode(
          cancellableValueTask {
            do! Task.Delay(5)
            return Text "world!"
          }
        )
        Fragment [ Text "Hello, "; Text "world!" ]
      ]
    ) do
    actual.Add(chunk)

  let expected = [ "Hello, "; "world!"; "Hello, "; "world!" ]

  actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))
}

[<Fact>]
let ``Render can render a mix of sync/async nodes with a mix of sync/async attributes and a mix of sync/async children``
  ()
  =
  taskUnit {
    let actual = ResizeArray()

    for chunk in
      Render.start(
        Fragment [
          AsyncNode(
            cancellableValueTask {
              do! Task.Delay(5)
              return Text "Hello, "
            }
          )
          Element {
            tag = "div"
            attributes = [
              Attribute { name = "class"; value = "foo" }
              AsyncAttribute(
                cancellableValueTask {
                  do! Task.Delay(5)
                  return { name = "id"; value = "bar" }
                }
              )
            ]
            children = [
              AsyncNode(
                cancellableValueTask {
                  do! Task.Delay(5)
                  return Text "world!"
                }
              )
            ]
          }
        ]
      ) do
      actual.Add(chunk)

    let expected = [
      "Hello, "
      "<div"
      " id=\"bar\""
      " class=\""
      "foo"
      "\""
      ">"
      "world!"
      "</div>"
    ]

    actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))
  }
