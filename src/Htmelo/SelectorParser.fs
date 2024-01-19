namespace Htmelo

open System.Collections.Immutable

[<Struct>]
type SelectorValue =
  | Id of id: string
  | Class of classes: string
  | Attribute of attributes: Htmelo.Attribute

module Parsers =
  open FParsec

  let tagName: Parser<string, unit> =
    let options =
      IdentifierOptions(
        isAsciiIdStart = isAsciiLetter,
        isAsciiIdContinue = fun ch -> isAsciiLetter ch || isDigit ch || ch = '-'
      )

    identifier(options)

  let pId: Parser<SelectorValue, unit> =
    let value = satisfy(fun ch -> ch <> '#' && ch <> '.' && ch <> '[')
    pchar '#' >>. manyChars value >>= (fun id -> preturn(Id id))

  let pClass: Parser<SelectorValue, unit> =
    pchar '.' >>. manyChars(letter <|> digit <|> pchar '-')
    >>= fun cls -> preturn(Class cls)

  let pAttribute: Parser<SelectorValue, unit> =
    let name = manyChars(letter <|> digit <|> pchar '-')
    let eq = pchar '='

    let value = manyChars(satisfy(fun ch -> ch <> ']'))

    pchar '[' >>. name .>> eq .>>. value .>> unicodeSpaces .>> pchar ']'
    >>= fun (name, value) -> preturn(Attribute { name = name; value = value })

  let pSelector: Parser<Element, unit> =
    tagName .>>. many(attempt pClass <|> attempt pAttribute <|> attempt pId)
    >>= fun (tag, values) ->
      let dcBuilder = ImmutableDictionary.CreateBuilder<string, AttributeNode>()

      for attributes in values do
        match attributes with
        | Attribute { name = "id"; value = value }
        | Id value ->
          dcBuilder.Add(
            "id",
            AttributeNode.Attribute { name = "id"; value = value }
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

      preturn {
        tag = tag
        attributes =
          dcBuilder.ToImmutableList()
          |> Seq.map(fun pair -> pair.Value)
          |> Seq.toList
        children = []
      }

  let selector(selector: string) =
    match run pSelector selector with
    | Success(result, _, _) -> result
    | Failure(origin, err, _) -> failwith $"Failed to parse '{origin}': {err}"

  let selectorResult(selector: string) =
    match run pSelector selector with
    | Success(result, _, _) -> Result.Ok result
    | Failure(origin, err, _) ->
      Result.Error($"Failed to parse '{origin}': {err}")

  let trySelector(selector: string) =
    match run pSelector selector with
    | Success(result, _, _) -> Some result
    | Failure(_, _, _) -> None
