[<RequireQualifiedAccess>]
module Hox.Parsers

open System.Collections.Immutable

open FParsec

open Hox.Core
open System.Collections.Generic

[<Struct>]
type SelectorValue =
  | Child of cTag: string * cValue: SelectorValue list
  | Id of id: string
  | Class of classes: string
  | Attribute of attributes: HAttribute

let tagName: Parser<string, unit> =
  let options =
    IdentifierOptions(
      isAsciiIdStart = isAsciiLetter,
      isAsciiIdContinue = fun ch -> isAsciiLetter ch || isDigit ch || ch = '-'
    )

  identifier(options)

let pId: Parser<SelectorValue, unit> =
  let value =
    satisfy(fun ch -> ch <> '#' && ch <> '.' && ch <> '[' && ch <> '\n')

  pchar '#' >>. manyChars value >>= (fun id -> preturn(Id id))

let pClass: Parser<SelectorValue, unit> =
  let avoid = noneOf [ ' '; '\t'; '\n'; '\r'; '['; '.'; '#' ]

  pchar '.' >>. manyChars(choice [ letter; digit; pchar '-'; avoid ])
  >>= fun cls -> preturn(Class cls)

let pAttribute: Parser<SelectorValue, unit> =
  let name =
    letter .>>. manyChars(choice [ letter; digit; pchar '-' ])
    >>= fun (initial, rest) -> preturn $"{initial}{rest}"

  let eq = pchar '='
  let value = manySatisfy(fun ch -> ch <> ']')

  pchar '[' >>. name .>> opt eq .>>. opt value .>> unicodeSpaces .>> pchar ']'
  >>= fun (name, value) ->
    preturn(
      Attribute {
        name = name
        value = defaultArg value System.String.Empty
      }
    )

let pElement =
  tagName .>> unicodeSpaces
  .>>. many(
    choice [ attempt pId; attempt pClass; attempt pAttribute ] .>> unicodeSpaces
  )

let pChild =
  pchar '>' >>. unicodeSpaces >>. pElement
  >>= fun (tag, values) -> preturn(Child(tag, values))

let pSelector: Parser<_, unit> =
  let childRefs, pValueRef = createParserForwardedToRef<SelectorValue, unit>()

  pValueRef.Value <- attempt pChild

  unicodeSpaces >>. pElement .>> unicodeSpaces .>>. many childRefs
  .>> unicodeSpaces

let rec getAttributes
  (builder: ImmutableDictionary<string, string>.Builder)
  (attributes: SelectorValue list)
  =
  match attributes with
  | [] ->
    builder.ToImmutable()
    |> Seq.map(fun (KeyValue(k, v)) ->
      AttributeNode.Attribute { name = k; value = v })
    |> LinkedList
  | Id id :: rest ->
    builder.Add("id", id)
    getAttributes builder rest
  | Class cls :: rest ->
    match builder.TryGetValue("class") with
    | true, value ->
      builder.["class"] <- $"{value} {cls}"
      getAttributes builder rest
    | false, _ ->
      builder.Add("class", cls)
      getAttributes builder rest
  | Attribute attribute :: rest ->
    builder.Add(attribute.name, attribute.value)
    getAttributes builder rest
  | _ :: rest -> getAttributes builder rest

let rec getChildren (parent: Element) (children: SelectorValue list) =
  match children with
  | [] -> parent
  | Child(tag, attributes) :: rest ->
    let builder = ImmutableDictionary.CreateBuilder<string, string>()

    let element = {
      tag = tag
      attributes = getAttributes builder attributes
      children = LinkedList()
    }

    let child = getChildren element rest
    parent.children.AddLast(Element child) |> ignore
    parent
  | _ -> failwith "Trees should not start without an element"

let selector(selector: string) =
  match run pSelector selector with
  | Success(result, _, _) ->
    match result with
    | (tag, attributes), children ->
      let builder = ImmutableDictionary.CreateBuilder<string, string>()

      let element = {
        tag = tag
        attributes = getAttributes builder attributes
        children = LinkedList()
      }

      getChildren element children
  | Failure(origin, err, _) -> failwith $"Failed to parse '{origin}': {err}"
