module RenderAsyncString

open System
open System.Collections.Generic
open System.Threading
open Xunit

open IcedTasks

open Hox
open Hox.Core
open Hox.Rendering
open System.Threading.Tasks
open FSharp.Control

[<Fact>]
let ``It should render an element``() = taskUnit {
  let node =
    Element {
      tag = "div"
      attributes = LinkedList()
      children = LinkedList()
    }

  let expected = "<div></div>"

  let! actual = Render.asString node
  Assert.Equal(expected, actual)
}

[<Fact>]
let ``It should render an element with Async``() =
  async {
    let node =
      Element {
        tag = "div"
        attributes = LinkedList()
        children = LinkedList()
      }

    let expected = "<div></div>"

    let! actual = Render.asStringAsync node
    Assert.Equal(expected, actual)
  }
  |> Async.StartAsTask
  :> Task

[<Fact>]
let ``It should render a text node``() = taskUnit {
  let node = Text "Hello, world!"

  let expected = "Hello, world!"

  let! actual = Render.asString node
  Assert.Equal(expected, actual)
}

[<Fact>]
let ``It should HTML encode a string renderd as text``() = taskUnit {
  let node = Text "<div>Hello, world!</div>"

  let expected = "&lt;div&gt;Hello, world!&lt;/div&gt;"

  let! actual = Render.asString node
  Assert.Equal(expected, actual)
}

[<Fact>]
let ``It should render a raw node``() = taskUnit {
  let node = Raw "<div>Hello, world!</div>"

  let expected = "<div>Hello, world!</div>"

  let! actual = Render.asString node
  Assert.Equal(expected, actual)
}

[<Fact>]
let ``It should render a comment node``() = taskUnit {
  let node = Comment "Hello, world!"

  let expected = "<!--Hello, world!-->"

  let! actual = Render.asString node
  Assert.Equal(expected, actual)
}

[<Fact>]
let ``It should render a fragment node``() = taskUnit {
  let node = Fragment(LinkedList [ Text "Hello, "; Text "world!" ])

  let expected = "Hello, world!"

  let! actual = Render.asString node
  Assert.Equal(expected, actual)
}

[<Fact>]
let ``It should render an Async Node``() = taskUnit {
  let node = AsyncNode(cancellableValueTask { return Text "Hello, world!" })

  let expected = "Hello, world!"

  let! actual = Render.asString node
  Assert.Equal(expected, actual)
}

[<Fact>]
let ``It should render an Async Seq Node``() = taskUnit {
  let node =
    AsyncSeqNode(
      taskSeq {
        Text "Hello, "
        Text "world!"
      }
    )

  let expected = "Hello, world!"

  let! actual = Render.asString node
  Assert.Equal(expected, actual)
}

[<Fact>]
let ``It should render an element with attributes``() = taskUnit {
  let node =
    Element {
      tag = "div"
      attributes =
        LinkedList [
          Attribute { name = "class"; value = "foo" }
          Attribute { name = "id"; value = "bar" }
        ]
      children = LinkedList()
    }

  let expected = "<div id=\"bar\" class=\"foo\"></div>"

  let! actual = Render.asString node
  Assert.Equal(expected, actual)
}

[<Fact>]
let ``It should render an element with an async attribute``() = taskUnit {
  let node =
    Element {
      tag = "div"
      attributes =
        LinkedList [
          AsyncAttribute(
            cancellableValueTask { return { name = "class"; value = "foo" } }
          )
          AsyncAttribute(
            cancellableValueTask { return { name = "id"; value = "bar" } }
          )
        ]
      children = LinkedList()
    }

  let expected = "<div id=\"bar\" class=\"foo\"></div>"

  let! actual = Render.asString node
  Assert.Equal(expected, actual)
}

[<Fact>]
let ``It should render a mix of sync/async nodes``() = taskUnit {
  let node =
    Fragment(
      LinkedList [
        AsyncNode(cancellableValueTask { return Text "Hello, " })
        Text "world!"
      ]
    )

  let expected = "Hello, world!"

  let! actual = Render.asString node
  Assert.Equal(expected, actual)
}

[<Fact>]
let ``It should render a mix of sync/async nodes with attributes``() = taskUnit {
  let node =
    Fragment(
      LinkedList [
        AsyncNode(cancellableValueTask { return Text "Hello, " })
        Element {
          tag = "div"
          attributes =
            LinkedList [
              Attribute { name = "class"; value = "foo" }
              Attribute { name = "id"; value = "bar" }
            ]
          children = LinkedList()
        }
        Text "world!"
      ]
    )

  let expected = "Hello, <div id=\"bar\" class=\"foo\"></div>world!"

  let! actual = Render.asString node
  Assert.Equal(expected, actual)
}

[<Fact>]
let ``It should render a mix of sync/async nodes with async attributes``() = taskUnit {
  let node =
    Fragment(
      LinkedList [
        AsyncNode(cancellableValueTask { return Text "Hello, " })
        Element {
          tag = "div"
          attributes =
            LinkedList [
              AsyncAttribute(
                cancellableValueTask {
                  return { name = "class"; value = "foo" }
                }
              )
              AsyncAttribute(
                cancellableValueTask { return { name = "id"; value = "bar" } }
              )
            ]
          children = LinkedList()
        }
        Text "world!"
      ]
    )

  let expected = "Hello, <div id=\"bar\" class=\"foo\"></div>world!"

  let! actual = Render.asString node
  Assert.Equal(expected, actual)
}

[<Fact>]
let ``It should render a mix of sync/async nodes with async attributes and async children``
  ()
  =
  taskUnit {
    let node =
      Fragment(
        LinkedList [
          AsyncNode(cancellableValueTask { return Text "Hello, " })
          Element {
            tag = "div"
            attributes =
              LinkedList [
                AsyncAttribute(
                  cancellableValueTask {
                    return { name = "class"; value = "foo" }
                  }
                )
                AsyncAttribute(
                  cancellableValueTask { return { name = "id"; value = "bar" } }
                )
              ]
            children =
              LinkedList [
                AsyncNode(cancellableValueTask { return Text "world!" })
              ]
          }
        ]
      )

    let expected = "Hello, <div id=\"bar\" class=\"foo\">world!</div>"

    let! actual = Render.asString node
    Assert.Equal(expected, actual)
  }

[<Fact>]
let ``It should render a mix of sync/async nodes with async attributes and async children and async fragment``
  ()
  =
  taskUnit {
    let node =
      Fragment(
        LinkedList [
          AsyncNode(cancellableValueTask { return Text "Hello, " })
          Element {
            tag = "div"
            attributes =
              LinkedList [
                AsyncAttribute(
                  cancellableValueTask {
                    return { name = "class"; value = "foo" }
                  }
                )
                AsyncAttribute(
                  cancellableValueTask { return { name = "id"; value = "bar" } }
                )
              ]
            children =
              LinkedList [
                AsyncNode(cancellableValueTask { return Text "world!" })
              ]
          }
          AsyncNode(cancellableValueTask { return Text "!" })
        ]
      )

    let expected = "Hello, <div id=\"bar\" class=\"foo\">world!</div>!"

    let! actual = Render.asString node
    Assert.Equal(expected, actual)
  }

[<Fact>]
let ``It should render a mix of sync/async nodes with async attributes and async children and async fragment and async seq``
  ()
  =
  taskUnit {
    let node =
      Fragment(
        LinkedList [
          AsyncNode(cancellableValueTask { return Text "Hello, " })
          Element {
            tag = "div"
            attributes =
              LinkedList [
                AsyncAttribute(
                  cancellableValueTask {
                    return { name = "class"; value = "foo" }
                  }
                )
                AsyncAttribute(
                  cancellableValueTask { return { name = "id"; value = "bar" } }
                )
              ]
            children =
              LinkedList [
                AsyncNode(cancellableValueTask { return Text "world!" })
              ]
          }
          AsyncNode(cancellableValueTask { return Text "!" })
          AsyncSeqNode(
            taskSeq {
              Text " "
              Text "How are you?"
            }
          )
        ]
      )

    let expected =
      "Hello, <div id=\"bar\" class=\"foo\">world!</div>! How are you?"

    let! actual = Render.asString node
    Assert.Equal(expected, actual)
  }

[<Fact>]
let ``It should render a mix of sync/async nodes with async attributes and async children and async fragment and async seq and async node``
  ()
  =
  taskUnit {
    let node =
      Fragment(
        LinkedList [
          AsyncNode(cancellableValueTask { return Text "Hello, " })
          Element {
            tag = "div"
            attributes =
              LinkedList [
                AsyncAttribute(
                  cancellableValueTask {
                    return { name = "class"; value = "foo" }
                  }
                )
                AsyncAttribute(
                  cancellableValueTask { return { name = "id"; value = "bar" } }
                )
              ]
            children =
              LinkedList [
                AsyncNode(cancellableValueTask { return Text "world!" })
              ]
          }
          AsyncNode(cancellableValueTask { return Text "!" })
          AsyncSeqNode(
            taskSeq {
              Text " "
              Text "How are you?"
            }
          )
          AsyncNode(cancellableValueTask { return Text "I'm fine, thanks!" })
        ]
      )

    let expected =
      "Hello, <div id=\"bar\" class=\"foo\">world!</div>! How are you?I&#39;m fine, thanks!"

    let! actual = Render.asString node
    Assert.Equal(expected, actual)
  }


[<Fact>]
let ``Rendering childless nodes should not write the end tag``() = taskUnit {
  let node =
    Element {
      tag = "input"
      attributes = LinkedList()
      children = LinkedList()
    }

  let expected = "<input>"

  let! actual = Render.asString node
  Assert.Equal(expected, actual)
}

[<Fact>]
let ``Childless nodes can be added to elements and fragments``() = taskUnit {
  let node =
    Fragment(
      LinkedList [
        Element {
          tag = "div"
          attributes = LinkedList()
          children =
            LinkedList [
              Element {
                tag = "source"
                attributes =
                  LinkedList [ Attribute { name = "src"; value = "foo" } ]
                children = LinkedList()
              }
            ]
        }
        Element {
          tag = "input"
          attributes = LinkedList()
          children = LinkedList()
        }
      ]
    )

  let expected = "<div><source src=\"foo\"></div><input>"

  let! actual = Render.asString node
  Assert.Equal(expected, actual)
}

[<Fact>]
let ``It should render a nested element``() = taskUnit {
  let node = h("div > span")

  let expected = "<div><span></span></div>"

  let! actual = Render.asString node
  Assert.Equal(expected, actual)
}

[<Fact>]
let ``It should render nested elements with attributes``() = taskUnit {
  let node = h("div > span#foo.bar")

  let expected = "<div><span id=\"foo\" class=\"bar\"></span></div>"

  let! actual = Render.asString node
  Assert.Equal(expected, actual)
}

[<Fact>]
let ``It should render attributes for parent and nested elements``() = taskUnit {
  let node = h("div#foo.bar > span#baz.qux")

  let expected =
    "<div id=\"foo\" class=\"bar\"><span id=\"baz\" class=\"qux\"></span></div>"

  let! actual = Render.asString node
  Assert.Equal(expected, actual)
}

[<Fact>]
let ``It should add the node to the inner most child in a nested element``() = taskUnit {
  let node = h("div > span", "Hello, world!")

  let expected = "<div><span>Hello, world!</span></div>"

  let! actual = Render.asString node
  Assert.Equal(expected, actual)
}

[<Fact>]
let ``It should add the node to the inner most child and respect the attributes``
  ()
  =
  taskUnit {
    let node = h("div#foo.bar > span#baz.qux", "Hello world!")

    let expected =
      "<div id=\"foo\" class=\"bar\"><span id=\"baz\" class=\"qux\">Hello world!</span></div>"

    let! actual = Render.asString node
    Assert.Equal(expected, actual)
  }

[<Fact>]
let ``It should add the parent to the tree even if the node is added to the inner most child``
  ()
  =
  taskUnit {
    let node =
      h(
        "ul.list-items",
        h("li.is-active > a.list-item[href=/]", "Home"),
        h("li > a.list-item[href=/about]", "About"),
        h("li > a.list-item[href=/contact]", "Contact")
      )

    let expected =
      "<ul class=\"list-items\"><li class=\"is-active\"><a class=\"list-item\" href=\"/\">Home</a></li><li><a class=\"list-item\" href=\"/about\">About</a></li><li><a class=\"list-item\" href=\"/contact\">Contact</a></li></ul>"

    let! actual = Render.asString node
    Assert.Equal(expected, actual)

  }
