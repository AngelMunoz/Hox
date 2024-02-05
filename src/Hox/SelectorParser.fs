[<RequireQualifiedAccess>]
module Hox.Parsers

open System.Collections.Immutable

open FParsec

open Hox.Core
open System.Collections.Generic

[<Struct>]
type private SelectorValue =
  | Id of id: string
  | Class of classes: string
  | Attribute of attributes: HAttribute

let private tagName: Parser<string, unit> =
  let options =
    IdentifierOptions(
      isAsciiIdStart = isAsciiLetter,
      isAsciiIdContinue = fun ch -> isAsciiLetter ch || isDigit ch || ch = '-'
    )

  identifier(options)

let private pId: Parser<SelectorValue, unit> =
  let value = satisfy(fun ch -> ch <> '#' && ch <> '.' && ch <> '[')

  pchar '#' >>. manyChars value .>> unicodeSpaces >>= (fun id -> preturn(Id id))

let private pClass: Parser<SelectorValue, unit> =
  let avoid = noneOf [ ' '; '\t'; '\n'; '\r'; '['; '.'; '#' ]

  pchar '.' >>. manyChars(letter <|> digit <|> pchar '-' <|> avoid)
  .>> unicodeSpaces
  >>= fun cls -> preturn(Class cls)

let private pAttribute: Parser<SelectorValue, unit> =
  let name =
    letter .>>. manyChars(letter <|> digit <|> pchar '-')
    >>= fun (initial, rest) -> preturn $"{initial}{rest}"

  let eq = pchar '='
  let value = manySatisfy(fun ch -> ch <> ']')

  pchar '[' >>. name .>> opt eq .>>. opt value
  .>> unicodeSpaces
  .>> pchar ']'
  .>> unicodeSpaces
  >>= fun (name, value) ->
    preturn(
      Attribute {
        name = name
        value = defaultArg value System.String.Empty
      }
    )

let private pSelector: Parser<Element, unit> =
  unicodeSpaces >>. tagName .>> unicodeSpaces
  .>>. many(attempt pClass <|> attempt pAttribute <|> attempt pId)
  .>> unicodeSpaces
  >>= fun (tag, values) ->
    let dcBuilder = ImmutableDictionary.CreateBuilder<string, AttributeNode>()

    for attributes in values do
      match attributes with
      | Attribute { name = "id"; value = value }
      | Id value ->
        dcBuilder.Add(
          "id",
          AttributeNode.Attribute { name = "id"; value = value.Trim() }
        )
      | Attribute { name = "class"; value = value }
      | Class value ->
        match dcBuilder.TryGetValue("class") with
        | true, AttributeNode.Attribute { name = "class"; value = classes } ->
          dcBuilder.Remove("class") |> ignore

          dcBuilder.Add(
            "class",
            AttributeNode.Attribute {
              name = "class"
              value = $"%s{classes} %s{value}"
            }
          )
        | false, _ ->
          dcBuilder.Add(
            "class",
            AttributeNode.Attribute { name = "class"; value = value }
          )
        | _, _ -> ()
      | Attribute attribute ->
        match dcBuilder.TryGetValue(attribute.name) with
        | true, AttributeNode.Attribute { name = key; value = value } ->
          dcBuilder.Remove(key) |> ignore

          dcBuilder.Add(
            key,
            AttributeNode.Attribute {
              name = key
              value = $"%s{value} %s{attribute.value}"
            }
          )
        | false, _ ->
          dcBuilder.Add(attribute.name, AttributeNode.Attribute attribute)
        | _, _ -> ()

    let built = dcBuilder.ToImmutableArray()
    let items = LinkedList()

    for pair in built do
      items.AddLast(pair.Value) |> ignore

    preturn {
      tag = tag
      attributes = items
      children = LinkedList()
    }

let selector(selector: string) =
  match run pSelector selector with
  | Success(result, _, _) -> result
  | Failure(origin, err, _) -> failwith $"Failed to parse '{origin}': {err}"
