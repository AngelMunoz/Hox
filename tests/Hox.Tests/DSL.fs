module DSL

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

open Xunit

open FSharp.Control
open IcedTasks


open Hox
open Hox.Core
open Hox.Rendering

module Elements =
  [<Fact>]
  let ``addToNode can be used to add a child node``() =
    let node =
      Element {
        tag = "div"
        children = LinkedList()
        attributes = LinkedList()
      }

    let child = Text "Hello, World!"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Element { tag = "div"; children = children } ->
      let (Text value) = Assert.Single children
      Assert.Equal("Hello, World!", value)
    | other ->
      Assert.Fail
        $"Expected node to be an Element with tag 'div' and a single child, but got %A{other}"

  [<Fact>]
  let ``addToNode can be used to add a child node to a node with existing children``
    ()
    =
    let node =
      Element {
        tag = "div"
        children = LinkedList [ Text "Hello, World!" ]
        attributes = LinkedList()
      }

    let child = Text "Hello, World!"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Element { tag = "div"; children = items } ->
      match items |> List.ofSeq with
      | [ Text "Hello, World!"; Text "Hello, World!" ] -> ()
      | other ->
        Assert.Fail
          $"Expected node to be an Element with tag 'div' and two children, but got %A{other}"
    | other ->
      Assert.Fail
        $"Expected node to be an Element with tag 'div' and two children, but got %A{other}"

  [<Fact>]
  let ``addToNode can add async nodes``() = taskUnit {
    let node =
      Element {
        tag = "div"
        children = LinkedList()
        attributes = LinkedList()
      }

    let child =
      AsyncNode(
        cancellableValueTask {
          do! Task.Delay(5)
          return Text "Hello, World!"
        }
      )

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | AsyncNode content ->

      let! content' = content CancellationToken.None

      match content' with
      | Element { tag = "div"; children = items } ->
        let (Text value) = Assert.Single items
        Assert.Equal("Hello, World!", value)
      | other ->
        Assert.Fail
          $"Expected node to be an Element with tag 'div' and a single text node, but got %A{other}"
    | other ->
      Assert.Fail $"Expected node to be an async node, but got %A{other}"
  }

  [<Fact>]
  let ``addtoNode can add fragment nodes to element``() =
    let node =
      Element {
        tag = "div"
        children = LinkedList()
        attributes = LinkedList()
      }

    let child =
      Fragment(LinkedList [ Text "Hello, World!"; Text "Hello, World!" ])

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Element { tag = "div"; children = items } ->
      match items |> List.ofSeq with
      | [ Text "Hello, World!"; Text "Hello, World!" ] -> ()
      | other ->
        Assert.Fail
          $"Expected node to be an Element with tag 'div' and two children, but got %A{other}"
    | other ->
      Assert.Fail
        $"Expected node to be an Element with tag 'div' and two children, but got %A{other}"

  [<Fact>]
  let ``addToNode can add taskseqs to elements``() = taskUnit {
    let node =
      Element {
        tag = "div"
        children = LinkedList()
        attributes = LinkedList()
      }

    let children =
      AsyncSeqNode(
        taskSeq {
          do! Task.Delay(5)
          yield Text "Hello, World!"
          yield Text "Hello, World!"
        }
      )

    let node' = NodeOps.addToNode(node, children)

    match node' with
    | Element { tag = "div"; children = items } ->
      let (AsyncSeqNode content) = Assert.Single items
      let! content' = TaskSeq.toListAsync content

      match content' with
      | [ Text "Hello, World!"; Text "Hello, World!" ] -> ()
      | other -> Assert.Fail $"Expected two text nodes, but got %A{other}"
    | other ->
      Assert.Fail
        $"Expected node to be an Element with tag 'div' and a single child, but got %A{other}"

  }

module Fragments =

  [<Fact>]
  let ``addToNode can add a child node``() =
    let node = Fragment(LinkedList [ Text "Hello, World!" ])

    let child = Text "Hello, World1!"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Fragment items ->
      match items |> List.ofSeq with
      | [ Text "Hello, World!"; Text "Hello, World1!" ] -> ()
      | other ->
        Assert.Fail
          $"Expected node to be a Fragment with two children, but got %A{other}"
    | other ->
      Assert.Fail
        $"Expected node to be a Fragment with two children, but got %A{other}"

  [<Fact>]
  let ``addToNode can will preserve the order of children when adding another fragment``
    ()
    =
    let node = Fragment(LinkedList [ Text "Hello, World!" ])

    let child =
      Fragment(LinkedList [ Text "Hello, World1!"; Text "Hello, World2!" ])

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Fragment items ->
      match items |> List.ofSeq with
      | [ Text "Hello, World!"; Text "Hello, World1!"; Text "Hello, World2!" ] ->
        ()
      | other ->
        Assert.Fail
          $"Expected node to be a Fragment with three children, but got %A{other}"
    | other ->
      Assert.Fail
        $"Expected node to be a Fragment with three children, but got %A{other}"

  [<Fact>]
  let ``addToNode will preserve the order of children when adding an async node seq``
    ()
    =
    taskUnit {
      let node = Fragment(LinkedList [ Text "Hello, World!" ])

      let child =
        AsyncSeqNode(
          taskSeq {
            do! Task.Delay(5)
            yield Text "Hello, World1!"
            yield Text "Hello, World2!"
          }
        )

      let node' = NodeOps.addToNode(node, child)

      match node' with
      | AsyncSeqNode nodes ->
        let! nodes = TaskSeq.toListAsync nodes

        match nodes with
        | [ Text "Hello, World!"; Text "Hello, World1!"; Text "Hello, World2!" ] ->
          ()
        | other ->
          Assert.Fail
            $"Expected nodes to have three text nodes, but got %A{other}"

      | other ->
        Assert.Fail
          $"Expected node to be a Fragment with three children, but got %A{other}"
    }

module Texts =

  [<Fact>]
  let ``addNode will merge two text nodes``() =
    let node = Text "Hello, World!"

    let child = Text "Hello, World1!"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Text "Hello, World!Hello, World1!" -> ()
    | other ->
      Assert.Fail
        $"Expected node to be a Text node with value 'Hello, World!Hello, World1!', but got %A{other}"

  [<Fact>]
  let ``addNode will merge two raw nodes``() =
    let node = Raw "<div>Hello, World!</div>"

    let child = Raw "<div>Hello, World1!</div>"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Raw "<div>Hello, World!</div><div>Hello, World1!</div>" -> ()
    | other ->
      Assert.Fail
        $"Expected node to be a Raw node with value '<div>Hello, World!</div><div>Hello, World1!</div>', but got %A{other}"

  [<Fact>]
  let ``addNode will not merge a child raw node with a parent text node``() =
    let node = Text "Hello, World!"

    let child = Raw "<div>Hello, World1!</div>"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Text "Hello, World!" -> ()
    | other ->
      Assert.Fail
        $"Expected node to be a Raw node with value 'Hello, World!<div>Hello, World1!</div>', but got %A{other}"

  [<Fact>]
  let ``addNode will merge raw nodes and text nodes together``() =
    let node = Raw "<div>Hello, World!</div>"

    let child = Text "Hello, World1!"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Raw "<div>Hello, World!</div>Hello, World1!" -> ()
    | other ->
      Assert.Fail
        $"Expected node to be a Raw node with value '<div>Hello, World!</div>Hello, World1!', but got %A{other}"

  [<Fact>]
  let ``addToNode will merge two comment nodes together``() =
    let node = Comment "Hello, World!"

    let child = Comment "Hello, World1!"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Comment "Hello, World!Hello, World1!" -> ()
    | other ->
      Assert.Fail
        $"Expected node to be a Comment node with value 'Hello, World!Hello, World1!', but got %A{other}"

  [<Fact>]
  let ``addToNode will merge mixed text and comment nodes together``() =
    let node = Comment "Hello, World!"

    let child = Text "Hello, World1!"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Comment "Hello, World!Hello, World1!" -> ()
    | other ->
      Assert.Fail
        $"Expected node to be a Raw node with value 'Hello, World!Hello, World1!', but got %A{other}"

  [<Fact>]
  let ``h parses the first string and the subsecuent ones become text nodes``
    ()
    =
    let node = h("p", "Hello, World!", "Hello, World1!")

    match node with
    | Element { tag = "p"; children = items } ->
      match items |> List.ofSeq with
      | [ Text "Hello, World!"; Text "Hello, World1!" ] -> ()
      | other ->
        Assert.Fail
          $"Expected node to be an Element with tag 'p' and two children, but got %A{other}"
    | other ->
      Assert.Fail
        $"Expected node to be an Element with tag 'p' and two children, but got %A{other}"

module AsyncNodes =

  [<Fact>]
  let ``addToNode can add sync nodes to async nodes``() = taskUnit {
    let node =
      AsyncNode(
        cancellableValueTask {
          do! Task.Delay(5)

          return
            Element {
              tag = "div"
              children = LinkedList()
              attributes = LinkedList()
            }
        }
      )

    let child = Text "Hello, World!"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | AsyncNode content ->
      let! content = content CancellationToken.None

      match content with
      | Element { tag = "div"; children = items } ->
        let (Text value) = Assert.Single items
        Assert.Equal("Hello, World!", value)
      | other ->
        Assert.Fail
          $"Expected node to be an Element with tag 'div' and a single child, but got %A{other}"
    | other ->
      Assert.Fail $"Expected node to be an async node, but got %A{other}"
  }

  [<Fact>]
  let ``addToNode can add async nodes to async nodes``() = taskUnit {
    let node =
      AsyncNode(
        cancellableValueTask {
          do! Task.Delay(5)

          return
            Element {
              tag = "div"
              children = LinkedList()
              attributes = LinkedList()
            }
        }
      )

    let child =
      AsyncNode(
        cancellableValueTask {
          do! Task.Delay(5)

          return Text "Hello, World!"
        }
      )

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | AsyncNode content ->
      let! content = content CancellationToken.None

      match content with
      | Element { tag = "div"; children = items } ->
        let (Text value) = Assert.Single items
        Assert.Equal("Hello, World!", value)
      | other ->
        Assert.Fail
          $"Expected node to be an Element with tag 'div' and a single child, but got %A{other}"
    | other ->
      Assert.Fail $"Expected node to be an async node, but got %A{other}"
  }

  [<Fact>]
  let ``addToNode can add fragment nodes to async nodes``() = taskUnit {
    let node =
      AsyncNode(
        cancellableValueTask {
          do! Task.Delay(5)

          return
            Element {
              tag = "div"
              children = LinkedList()
              attributes = LinkedList()
            }
        }
      )

    let child =
      Fragment(LinkedList [ Text "Hello, World!"; Text "Hello, World!" ])

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | AsyncNode content ->
      let! content = content CancellationToken.None

      match content with
      | Element { tag = "div"; children = items } ->
        match items |> List.ofSeq with
        | [ Text "Hello, World!"; Text "Hello, World!" ] -> ()
        | other ->
          Assert.Fail
            $"Expected node to be an Element with tag 'div' and two children, but got %A{other}"
      | other ->
        Assert.Fail
          $"Expected node to be an Element with tag 'div' and two children, but got %A{other}"
    | other ->
      Assert.Fail $"Expected node to be an async node, but got %A{other}"
  }

  [<Fact>]
  let ``addToNode can add taskseqs to async nodes``() = taskUnit {
    let node =
      AsyncNode(
        cancellableValueTask {
          do! Task.Delay(5)

          return
            Element {
              tag = "div"
              children = LinkedList()
              attributes = LinkedList()
            }
        }
      )

    let children =
      AsyncSeqNode(
        taskSeq {
          do! Task.Delay(5)
          yield Text "Hello, World!"
          yield Text "Hello, World!"
        }
      )

    let node' = NodeOps.addToNode(node, children)

    match node' with
    | AsyncNode content ->
      let! content = content CancellationToken.None

      match content with
      | Element { tag = "div"; children = item } ->
        let (AsyncSeqNode nodes) = Assert.Single item
        let! nodes = TaskSeq.toListAsync nodes

        match nodes with
        | [ Text "Hello, World!"; Text "Hello, World!" ] -> ()
        | other ->
          Assert.Fail
            $"Expected nodes to have two text nodes, but got %A{other}"
      | other ->
        Assert.Fail
          $"Expected node to be an Element with tag 'div' a single async seq node, but got %A{other}"
    | other ->
      Assert.Fail $"Expected node to be an async node, but got %A{other}"
  }

module AsyncSeqNodes =

  [<Fact>]
  let ``addToNode can add sync nodes to async seq nodes``() = taskUnit {
    let node =
      AsyncSeqNode(
        taskSeq {
          do! Task.Delay(5)
          Text "Hello, World!"
          Text "Hello, World1!"
        }
      )

    let child = Text "Hello, World2!"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | AsyncSeqNode nodes ->
      let! nodes = TaskSeq.toListAsync nodes

      match nodes with
      | [ Text "Hello, World!"; Text "Hello, World1!"; Text "Hello, World2!" ] ->
        ()
      | other ->
        Assert.Fail
          $"Expected nodes to have three text nodes, but got %A{other}"
    | other ->
      Assert.Fail $"Expected node to be an async seq node, but got %A{other}"
  }

  [<Fact>]
  let ``addToNode will respect order when merging an async seq node with a fragment``
    ()
    =
    taskUnit {
      let node =
        AsyncSeqNode(
          taskSeq {
            do! Task.Delay(5)
            Text "Hello, World!"
            Text "Hello, World1!"
          }
        )

      let child =
        Fragment(LinkedList [ Text "Hello, World2!"; Text "Hello, World3!" ])

      let node' = NodeOps.addToNode(node, child)

      match node' with
      | AsyncSeqNode nodes ->
        let! nodes = TaskSeq.toListAsync nodes

        match nodes with
        | [ Text "Hello, World!"
            Text "Hello, World1!"
            Text "Hello, World2!"
            Text "Hello, World3!" ] -> ()
        | other ->
          Assert.Fail
            $"Expected nodes to have four text nodes, but got %A{other}"
      | other ->
        Assert.Fail $"Expected node to be an async seq node, but got %A{other}"
    }

  [<Fact>]
  let ``addToNode will respect order when merging an async seq node with another async seq node``
    ()
    =
    taskUnit {
      let node =
        AsyncSeqNode(
          taskSeq {
            do! Task.Delay(5)
            Text "Hello, World!"
            Text "Hello, World1!"
          }
        )

      let child =
        AsyncSeqNode(
          taskSeq {
            do! Task.Delay(5)
            Text "Hello, World2!"
            Text "Hello, World3!"
          }
        )

      let node' = NodeOps.addToNode(node, child)

      match node' with
      | AsyncSeqNode nodes ->
        let! nodes = TaskSeq.toListAsync nodes

        match nodes with
        | [ Text "Hello, World!"
            Text "Hello, World1!"
            Text "Hello, World2!"
            Text "Hello, World3!" ] -> ()
        | other ->
          Assert.Fail
            $"Expected nodes to have four text nodes, but got %A{other}"
      | other ->
        Assert.Fail $"Expected node to be an async seq node, but got %A{other}"
    }

  [<Fact>]
  let ``addToNode will respect order when merging an async seq node with an async node``
    ()
    =
    taskUnit {
      let node =
        AsyncSeqNode(
          taskSeq {
            do! Task.Delay(5)
            Text "Hello, World!"
            Text "Hello, World1!"
          }
        )

      let child =
        AsyncNode(
          cancellableValueTask {
            do! Task.Delay(5)
            return Text "Hello, World2!"
          }
        )

      let node' = NodeOps.addToNode(node, child)

      match node' with
      | AsyncNode content ->

        let! content = content CancellationToken.None

        match content with
        | AsyncSeqNode nodes ->
          let! nodes = TaskSeq.toListAsync nodes

          match nodes with
          | [ Text "Hello, World!"; Text "Hello, World1!"; Text "Hello, World2!" ] ->
            ()
          | other ->
            Assert.Fail
              $"Expected nodes to have three text nodes, but got %A{other}"
        | other ->
          Assert.Fail
            $"Expected node to be an async seq node, but got %A{other}"
      | other ->
        Assert.Fail $"Expected node to be an async node, but got %A{other}"
    }

open NodeOps.Operators

[<Fact>]
let ``addToNode will add correctly and every kind of Node into an element parent``
  ()
  =
  taskUnit {
    let node =
      Element {
        tag = "div"
        children = LinkedList()
        attributes = LinkedList()
      }

    let node =
      node
      <+ Text "Text Node"
      <+ Raw "<div>Raw Node</div>"
      <+ Comment "Comment Node"
      <+ Fragment(LinkedList [ Text "Fragment Node"; Text "Fragment Node1" ])
      <+ AsyncNode(
        cancellableValueTask {
          do! Task.Delay(5)
          return Text "Async Node"
        }
      )
      <+ AsyncSeqNode(
        taskSeq {
          do! Task.Delay(5)
          yield Text "Async Seq Node"
          yield Text "Async Seq Node1"
        }
      )

    match node with
    | AsyncNode node ->
      let! node = node CancellationToken.None

      match node with
      | Element { tag = "div"; children = items } ->
        match items |> List.ofSeq with
        | [ Text "Text Node"
            Raw "<div>Raw Node</div>"
            Comment "Comment Node"
            Text "Fragment Node"
            Text "Fragment Node1"
            Text "Async Node"
            AsyncSeqNode asyncSeqChild ] ->
          let! asyncSeqChild = TaskSeq.toListAsync asyncSeqChild

          match asyncSeqChild with
          | [ Text "Async Seq Node"; Text "Async Seq Node1" ] -> ()
          | other ->
            Assert.Fail
              $"Expected async seq child to have two text nodes, but got %A{other}"
        | other ->
          Assert.Fail
            $"Expected node to be an Element with tag 'div' and six children, but got %A{other}"

      | other ->
        Assert.Fail
          $"Expected node to be an Element with tag 'div' and six children, but got %A{other}"
    | other ->
      Assert.Fail $"Expected node to be an async node, but got %A{other}"
  }


module Attributes =

  [<Fact>]
  let ``addAttribute will add an attribute to an element node``() =
    let node =
      Element {
        tag = "div"
        children = LinkedList()
        attributes = LinkedList()
      }

    let node' =
      NodeOps.addAttribute(
        node,
        Attribute { name = "class"; value = "test-class" }
      )

    match node' with
    | Element { tag = "div"; attributes = items } ->
      let (Attribute attr) = Assert.Single items
      Assert.Equal("class", attr.name)
      Assert.Equal("test-class", attr.value)

    | other ->
      Assert.Fail
        $"Expected node to be an Element with tag 'div' and a single child, but got %A{other}"

  [<Fact>]
  let ``addAttribute will add an async attribute to an attribute``() = taskUnit {
    let node =
      Element {
        tag = "div"
        children = LinkedList()
        attributes = LinkedList()
      }

    let node' =
      NodeOps.addAttribute(
        node,
        AsyncAttribute(
          cancellableValueTask {
            do! Task.Delay(5)
            return { name = "class"; value = "test-class" }
          }
        )
      )

    match node' with
    | Element { tag = "div"; attributes = items } ->
      let (AsyncAttribute attr) = Assert.Single items
      let! attr = attr CancellationToken.None

      match attr with
      | { name = "class"; value = "test-class" } -> ()
      | other ->
        Assert.Fail
          $"Expected attribute to be an Attribute with name 'class' and value 'test-class', but got %A{other}"
    | other ->
      Assert.Fail
        $"Expected node to be an Element with tag 'div' and a single child, but got %A{other}"
  }

  [<Fact>]
  let ``addAttribute will add an attribute to an async node element``() = taskUnit {

    let node =
      AsyncNode(
        cancellableValueTask {
          do! Task.Delay(5)

          return
            Element {
              tag = "div"
              children = LinkedList()
              attributes = LinkedList()
            }
        }
      )

    let node' =
      NodeOps.addAttribute(
        node,
        Attribute { name = "class"; value = "test-class" }
      )

    match node' with
    | AsyncNode node ->
      let! node = node CancellationToken.None

      match node with
      | Element { tag = "div"; attributes = items } ->
        let (Attribute attr) = Assert.Single items
        Assert.Equal("class", attr.name)
        Assert.Equal("test-class", attr.value)
      | other ->
        Assert.Fail
          $"Expected node to be an Element with tag 'div' and a single child, but got %A{other}"
    | other ->
      Assert.Fail $"Expected node to be an async node, but got %A{other}"
  }

  [<Fact>]
  let ``addAttribute will ignore any node that is not an element``() =
    let node = Text "Hello, World!"

    let node' =
      NodeOps.addAttribute(
        node,
        Attribute { name = "class"; value = "test-class" }
      )

    match node' with
    | Text "Hello, World!" -> ()
    | other ->
      Assert.Fail
        $"Expected node to be a Text node with value 'Hello, World!', but got %A{other}"

  [<Fact>]
  let ``addAttribute will ignore any node that is not an element, even if it is an async node``
    ()
    =
    taskUnit {
      let node =
        AsyncNode(
          cancellableValueTask {
            do! Task.Delay(5)

            return Text "Hello, World!"
          }
        )

      let node' =
        NodeOps.addAttribute(
          node,
          Attribute { name = "class"; value = "test-class" }
        )

      match node' with
      | AsyncNode node ->
        let! node = node CancellationToken.None

        match node with
        | Text "Hello, World!" -> ()
        | other ->
          Assert.Fail
            $"Expected node to be a Text node with value 'Hello, World!', but got %A{other}"
      | other ->
        Assert.Fail $"Expected node to be an async node, but got %A{other}"
    }

  [<Fact>]
  let ``addAttribute will respect the order of addition of the attributes for both sync/async``
    ()
    =
    taskUnit {

      let node =
        Element {
          tag = "div"
          children = LinkedList()
          attributes = LinkedList()
        }

      let node =
        node
        <+. Attribute { name = "class"; value = "test-class" }
        <+. Attribute { name = "id"; value = "id-attr" }
        <+. Attribute {
          name = "data-name"
          value = "data-name-attr"
        }
        <+. AsyncAttribute(
          cancellableValueTask {
            do! Task.Delay(5)

            return {
              name = "my-async-attr"
              value = "my async attr"
            }
          }
        )
        <+. AsyncAttribute(
          cancellableValueTask {
            do! Task.Delay(5)

            return {
              name = "my-async-attr1"
              value = "my async attr1"
            }
          }
        )

      match node with
      | Element {
                  tag = "div"
                  children = _
                  attributes = attributes
                } ->
        match attributes |> List.ofSeq with
        | [ Attribute { name = "class"; value = "test-class" }
            Attribute { name = "id"; value = "id-attr" }
            Attribute {
                        name = "data-name"
                        value = "data-name-attr"
                      }
            AsyncAttribute attr
            AsyncAttribute attr1 ] ->

          let! attr = attr CancellationToken.None
          let! attr1 = attr1 CancellationToken.None

          match attr, attr1 with
          | {
              name = "my-async-attr"
              value = "my async attr"
            },
            {
              name = "my-async-attr1"
              value = "my async attr1"
            } -> ()
          | other ->
            Assert.Fail
              $"Expected attributes to be an Attribute with name 'my-async-attr' and value 'my async attr' and an Attribute with name 'my-async-attr1' and value 'my async attr1', but got %A{other}"
        | _ -> Assert.Fail "Expected two async attributes"
      | other ->
        Assert.Fail
          $"Expected node to be an Element with tag 'div' and a single child, but got %A{other}"
    }


module DSD =

  [<Fact>]
  let ``sh can produce a function that will create a template``() =
    let template = sh("x-my-tag", h "slot", h "slot[name=second]")

    let templatedResult = template(text "Hello, World!")

    match templatedResult with
    | Element { tag = "x-my-tag"; children = items } ->

    match items |> List.ofSeq with
    | [ Element {
                  tag = "template"
                  attributes = attributes
                  children = items
                }
        Text "Hello, World!" ] ->
      match attributes |> List.ofSeq with
      | [ Attribute {
                      name = "shadowrootmode"
                      value = "open"
                    } ] -> ()
      | other ->
        Assert.Fail
          $"Expected template to have an attribute with name 'shadowrootmode' and value 'open', but got %A{other}"

      let (Element {
                     tag = "slot"
                     attributes = _
                     children = _
                   }) =
        items.First.Value

      let (Element {
                     tag = "slot"
                     attributes = attributes2
                   }) =
        items.First.Next.Value

      let (Attribute { name = name; value = value }) = Assert.Single attributes2

      Assert.Equal("name", name)
      Assert.Equal("second", value)

    | other ->
      Assert.Fail
        $"Expected node to be an Element with tag 'x-my-tag' and a single child, but got %A{other}"

  [<Fact>]
  let ``shcs can produce a function that will create a template``() =
    let template = shcs("x-my-tag", h "slot", h "slot[name=second]")

    let templatedResult = template.Invoke(text "Hello, World!")

    match templatedResult with
    | Element { tag = "x-my-tag"; children = items } ->

    match items |> List.ofSeq with
    | [ Element {
                  tag = "template"
                  attributes = attributes
                  children = items
                }
        Text "Hello, World!" ] ->
      match attributes |> List.ofSeq with
      | [ Attribute {
                      name = "shadowrootmode"
                      value = "open"
                    } ] -> ()
      | other ->
        Assert.Fail
          $"Expected template to have an attribute with name 'shadowrootmode' and value 'open', but got %A{other}"

      let (Element {
                     tag = "slot"
                     attributes = _
                     children = _
                   }) =
        items.First.Value

      let (Element {
                     tag = "slot"
                     attributes = attributes2
                   }) =
        items.First.Next.Value

      let (Attribute { name = name; value = value }) = Assert.Single attributes2

      Assert.Equal("name", name)
      Assert.Equal("second", value)

    | other ->
      Assert.Fail
        $"Expected node to be an Element with tag 'x-my-tag' and a single child, but got %A{other}"


  [<Fact>]
  let ``Scopable Article will have DSD enabled``() =
    let node =
      ScopableElements.article(
        h "link[href=https://some-css-file]",
        text "Hello, World!"
      )

    let (Element article) = node
    let (Element template) = article.children.First.Value

    let (Attribute { name = name; value = value }) =
      Assert.Single template.attributes

    Assert.Equal("shadowrootmode", name)
    Assert.Equal("open", value)

    let (Fragment templateChildren) = template.children.First.Value

    let (Element link) = templateChildren.First.Value

    let (Attribute { name = name; value = value }) =
      Assert.Single link.attributes

    Assert.Equal("href", name)
    Assert.Equal("https://some-css-file", value)

    let (Text value) = templateChildren.First.Next.Value

    Assert.Equal("Hello, World!", value)
