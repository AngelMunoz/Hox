module Parser

open System
open Xunit
open Hox
open Hox.Core

[<Fact>]
let ``Can parse a tag name``() =
  let expected = {
    tag = "div"
    attributes = []
    children = []
  }

  let actual = Parsers.selector "div"
  Assert.Equal(expected.tag, actual.tag)

[<Fact>]
let ``Can parse a tag name with an id``() =
  let expected = {
    tag = "div"
    attributes = [ Attribute { name = "id"; value = "my-id" } ]
    children = []
  }

  let actual = Parsers.selector "div#my-id"
  Assert.Equal(expected.tag, actual.tag)

  match actual.attributes with
  | [ Attribute { name = "id"; value = "my-id" } ] -> ()
  | _ -> Assert.Fail("Expected one attribute")

[<Fact>]
let ``Can parse a css class``() =
  let expected = {
    tag = "div"
    attributes = [ Attribute { name = "class"; value = "my-class" } ]
    children = []
  }

  let actual = Parsers.selector "div.my-class"
  Assert.Equal(expected.tag, actual.tag)

  match actual.attributes with
  | [ Attribute { name = "class"; value = "my-class" } ] -> ()
  | _ -> Assert.Fail("Expected a class with the value 'my-class'")

[<Fact>]
let ``Can parse multiple css classes``() =

  let expected = {
    tag = "div"
    attributes = [
      Attribute { name = "class"; value = "my-class" }
      Attribute {
        name = "class"
        value = "another-class"
      }
    ]
    children = []
  }

  let actual = Parsers.selector "div.my-class.another-class"
  Assert.Equal(expected.tag, actual.tag)

  match actual.attributes with
  | [ Attribute {
                  name = "class"
                  value = "my-class another-class"
                } ] -> ()
  | _ -> Assert.Fail("Expected a class with the value 'my-class another-class'")

[<Fact>]
let ``Can parse an attribute``() =
  let expected = {
    tag = "div"
    attributes = [ Attribute { name = "data-foo"; value = "bar" } ]
    children = []
  }

  let actual = Parsers.selector "div[data-foo=bar]"
  Assert.Equal(expected.tag, actual.tag)

  match actual.attributes with
  | [ Attribute { name = "data-foo"; value = "bar" } ] -> ()
  | _ ->
    Assert.Fail(
      "Expected an attribute with the name 'data-foo' and value 'bar'"
    )

[<Fact>]
let ``Can parse multiple attributes``() =
  let expected = {
    tag = "div"
    attributes = [
      Attribute { name = "data-foo"; value = "bar" }
      Attribute { name = "data-baz"; value = "qux" }
    ]
    children = []
  }

  let actual = Parsers.selector "div[data-foo=bar][data-baz=qux]"
  Assert.Equal(expected.tag, actual.tag)
  Assert.Equal(expected.attributes.Length, actual.attributes.Length)

  let attributes =
    actual.attributes
    |> List.map(fun attribute ->
      match attribute with
      | Attribute { name = name; value = value } -> struct (name, value)
      | _ -> failwith "Expected an attribute")

  Assert.Contains(struct ("data-foo", "bar"), attributes)
  Assert.Contains(struct ("data-baz", "qux"), attributes)

[<Fact>]
let ``Can parse a mix of id, class, and attribute selectors``() =

  let expected = {
    tag = "div"
    attributes = [
      Attribute { name = "data-foo"; value = "bar" }
      Attribute { name = "class"; value = "my-class" }
      Attribute { name = "id"; value = "my-id" }
    ]
    children = []
  }

  let actual = Parsers.selector "div#my-id.my-class[data-foo=bar]"
  Assert.Equal(expected.tag, actual.tag)
  Assert.Equal(expected.attributes.Length, actual.attributes.Length)

  let attributes =
    actual.attributes
    |> List.map(fun attribute ->
      match attribute with
      | Attribute { name = name; value = value } -> struct (name, value)
      | _ -> failwith "Expected an attribute")

  Assert.Contains(struct ("id", "my-id"), attributes)
  Assert.Contains(struct ("class", "my-class"), attributes)
  Assert.Contains(struct ("data-foo", "bar"), attributes)


[<Fact>]
let ``Can parse id, class, and attributes over multiple lines``() =

  let expected = {
    tag = "div"
    attributes = [
      Attribute { name = "id"; value = "my-id" }
      Attribute { name = "class"; value = "my-class" }
      Attribute { name = "data-foo"; value = "bar" }
    ]
    children = []
  }

  let actual =
    Parsers.selector
      """
    div#my-id
      .my-class
      [data-foo=bar]
  """

  Assert.Equal(expected.tag, actual.tag)

  let attributes =
    actual.attributes
    |> List.map(fun attribute ->
      match attribute with
      | Attribute { name = name; value = value } -> struct (name, value)
      | _ -> failwith "Expected an attribute")

  Assert.Contains(struct ("id", "my-id"), attributes)
  Assert.Contains(struct ("class", "my-class"), attributes)
  Assert.Contains(struct ("data-foo", "bar"), attributes)

[<Fact>]
let ``Attributes can have arbitrary content within '[' ']'``() =
  let expected = {
    tag = "div"
    attributes = [
      Attribute {
        name = "camello"
        value = "~!@#2abcDE"
      }
      Attribute {
        name = "camello-2"
        value =
          """^&*()_+QWERTYUIOP{}|ASDFGHJKL:"ZXCVBNM<>?1234567890-=qwertyuiop\asdfghjkl;'zxcvbnm,./"""
      }
      Attribute {
        name = "camello-3"
        value =
          """1234567890-=QWERTYUIOP{}|ASDFGHJKL:"ZXCVBNM<>?qwertyuiop\asdfghjkl;'zxcvbnm,./~!@#$%^&*()_+"""
      }
      Attribute {
        name = "camello-4"
        value =
          """~!@#$%^&*()_+QWERTYUIOP{}|ASDFGHJKL:"ZXCVBNM<>?1234567890-=qwertyuiop\asdfghjkl;'zxcvbnm,./"""
      }
    ]
    children = []
  }

  let actual =
    Parsers.selector
      """
    div[camello=~!@#2abcDE]
       [camello-2=^&*()_+QWERTYUIOP{}|ASDFGHJKL:"ZXCVBNM<>?1234567890-=qwertyuiop\asdfghjkl;'zxcvbnm,./]
       [camello-3=1234567890-=QWERTYUIOP{}|ASDFGHJKL:"ZXCVBNM<>?qwertyuiop\asdfghjkl;'zxcvbnm,./~!@#$%^&*()_+]
       [camello-4=~!@#$%^&*()_+QWERTYUIOP{}|ASDFGHJKL:"ZXCVBNM<>?1234567890-=qwertyuiop\asdfghjkl;'zxcvbnm,./]
  """

  Assert.Equal(expected.tag, actual.tag)
  Assert.Equal(expected.attributes.Length, actual.attributes.Length)

  let attributes =
    actual.attributes
    |> List.map(fun attribute ->
      match attribute with
      | Attribute { name = name; value = value } -> struct (name, value)
      | _ -> failwith "Expected an attribute")

  Assert.Contains(struct ("camello", "~!@#2abcDE"), attributes)

  Assert.Contains(
    struct ("camello-2",
            """^&*()_+QWERTYUIOP{}|ASDFGHJKL:"ZXCVBNM<>?1234567890-=qwertyuiop\asdfghjkl;'zxcvbnm,./"""),
    attributes
  )

  Assert.Contains(
    struct ("camello-3",
            """1234567890-=QWERTYUIOP{}|ASDFGHJKL:"ZXCVBNM<>?qwertyuiop\asdfghjkl;'zxcvbnm,./~!@#$%^&*()_+"""),
    attributes
  )

  Assert.Contains(
    struct ("camello-4",
            """~!@#$%^&*()_+QWERTYUIOP{}|ASDFGHJKL:"ZXCVBNM<>?1234567890-=qwertyuiop\asdfghjkl;'zxcvbnm,./"""),
    attributes
  )

// TODO: Add this test back in when we can parse \] in attributes
//[<Fact>]
//let ``Attributes can have \] in them``() =
//  let expected = {
//    tag = "div"
//    attributes = [ Attribute { name = "camello"; value = @"\]" } ]
//    children = []
//  }

//  let actual =
//    Parsers.selector
//      """
//    div[camello=\]]
//  """

//  Assert.Equal(expected.tag, actual.tag)
//  Assert.Equal(expected.attributes.Length, actual.attributes.Length)

//  let attributes =
//    actual.attributes
//    |> List.map(fun attribute ->
//      match attribute with
//      | Attribute { name = name; value = value } -> struct (name, value)
//      | _ -> failwith "Expected an attribute")

//  Assert.Contains(struct ("camello", @"]"), attributes)