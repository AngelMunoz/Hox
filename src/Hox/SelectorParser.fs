[<RequireQualifiedAccess>]
module Hox.Parsers

open System
open System.Collections.Immutable
open Hox.Core
open XParsec
open XParsec.CharParsers
open XParsec.Parsers

type private SelectorParser<'T> = Parser<'T, char, unit, ReadableString>

[<Struct>]
type SelectorValue =
  | Child of cTag: string * cValue: ImmutableArray<SelectorValue>
  | Id of id: string
  | Class of classes: string
  | Attribute of attributes: HAttribute

let tagName: SelectorParser<string> =
  many1Chars2
    (satisfy isAsciiLetter)
    (satisfy(fun c -> isAsciiLetter c || isDigit c || c = '-'))

let pId: SelectorParser<SelectorValue> =
  let value =
    satisfy(fun ch -> ch <> '#' && ch <> '.' && ch <> '[' && ch <> '\n')

  pchar '#' >>. manyChars value >>= (fun id -> preturn(Id id))

let pClass: SelectorParser<SelectorValue> =
  let avoid = noneOf [ ' '; '\t'; '\n'; '\r'; '['; '.'; '#' ]

  pchar '.' >>. manyChars(choice [ satisfy isLetter; digit; pchar '-'; avoid ])
  >>= fun cls -> preturn(Class cls)

let pAttribute: SelectorParser<SelectorValue> =
  let name =
    satisfy isLetter
    .>>. manyChars(choice [ satisfy isLetter; digit; pchar '-' ])
    >>= fun (struct (initial, rest)) -> preturn($"{initial}{rest}")

  let eq = pchar '='
  let value = manyChars(satisfy(fun ch -> ch <> ']'))

  pchar '[' >>. name .>> opt eq .>>. opt value .>> spaces .>> pchar ']'
  >>= fun (struct (name, value)) ->
    preturn(
      Attribute {
        name = name
        value =
          (match value with
           | ValueSome v -> v
           | ValueNone -> "")
      }
    )

let pElement: SelectorParser<struct (string * ImmutableArray<SelectorValue>)> =
  tagName .>> spaces .>>. many(choice [ pId; pClass; pAttribute ] .>> spaces)

let pChild: SelectorParser<SelectorValue> =
  pchar '>' >>. spaces >>. pElement
  >>= fun (struct (tag, values)) -> preturn(Child(tag, values))

let pSelector
  : SelectorParser<
      struct (struct (string * ImmutableArray<SelectorValue>) *
      ImmutableArray<SelectorValue>)
     > =
  spaces >>. pElement .>> spaces .>>. many pChild .>> spaces

let private collectAttributes
  (values: ImmutableArray<SelectorValue>)
  : Deque<AttributeNode> =
  let mergedClass =
    values
    |> Seq.choose (function
      | Class cls -> Some cls
      | _ -> None)
    |> String.concat " "

  let attributes = Deque(values.Length)
  let mutable idSeen = false
  let mutable classSeen = false

  for value in values do
    match value with
    | Id id when not idSeen ->
      idSeen <- true
      attributes.AddLast(AttributeNode.Attribute { name = "id"; value = id })
    | Id _ -> ()
    | Class _ when classSeen -> ()
    | Class _ ->
      classSeen <- true

      attributes.AddLast(
        AttributeNode.Attribute { name = "class"; value = mergedClass }
      )
    | Attribute attribute ->
      attributes.AddLast(AttributeNode.Attribute attribute)
    | Child _ -> ()

  attributes

let rec private getChildrenFrom
  (parent: Element)
  (children: ImmutableArray<SelectorValue>)
  index
  =
  if index >= children.Length then
    parent
  else
    match children[index] with
    | Child(tag, values) ->
      let element = {
        tag = tag
        attributes = collectAttributes values
        children = Deque(4)
      }

      let child = getChildrenFrom element children (index + 1)
      parent.children.AddLast(Element child)
      parent
    | _ -> failwith "Trees should not start without an element"

let selector(selector: string) =
  match pSelector(Reader.ofString selector ()) with
  | Ok(struct (struct (tag, values), children)) ->
    let element = {
      tag = tag
      attributes = collectAttributes values
      children = Deque(4)
    }

    getChildrenFrom element children 0
  | Error error ->
    let formatted = ErrorFormatting.formatStringError selector error
    failwith $"Failed to parse '{selector}': {formatted}"
